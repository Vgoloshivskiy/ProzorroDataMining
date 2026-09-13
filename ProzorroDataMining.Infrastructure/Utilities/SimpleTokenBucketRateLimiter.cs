using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProzorroDataMining.Infrastructure.Utilities
{
    // Simple token bucket rate limiter implementation to avoid external package dependencies.
    public class SimpleTokenBucketRateLimiter
    {
        private readonly double _capacity;
        private readonly double _tokensPerPeriod;
        private readonly TimeSpan _period;
        private double _tokens;
        private DateTime _lastRefillUtc;
        private readonly object _lock = new object();

        public SimpleTokenBucketRateLimiter(int capacity, int tokensPerPeriod, TimeSpan period)
        {
            _capacity = capacity;
            _tokensPerPeriod = tokensPerPeriod;
            _period = period;
            _tokens = capacity;
            _lastRefillUtc = DateTime.UtcNow;
        }

        private void RefillIfNeeded()
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastRefillUtc;
            if (elapsed <= TimeSpan.Zero) return;

            // how many periods have passed
            var periods = Math.Floor(elapsed.TotalMilliseconds / _period.TotalMilliseconds);
            if (periods <= 0) return;

            var add = periods * _tokensPerPeriod;
            _tokens = Math.Min(_capacity, _tokens + add);
            _lastRefillUtc = _lastRefillUtc.AddMilliseconds(periods * _period.TotalMilliseconds);
        }

        public async Task AcquireAsync(int tokens = 1, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                lock (_lock)
                {
                    RefillIfNeeded();
                    if (_tokens >= tokens)
                    {
                        _tokens -= tokens;
                        return;
                    }
                }

                // Wait a short time before retrying
                await Task.Delay(10, cancellationToken);
            }
        }
    }
}
