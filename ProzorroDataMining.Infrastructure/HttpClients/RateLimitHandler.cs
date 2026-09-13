using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ProzorroDataMining.Infrastructure.Utilities;

namespace ProzorroDataMining.Infrastructure.HttpClients
{
    public class RateLimitHandler : DelegatingHandler
    {
        private readonly SimpleTokenBucketRateLimiter _limiter;

        public RateLimitHandler(SimpleTokenBucketRateLimiter limiter)
        {
            _limiter = limiter;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await _limiter.AcquireAsync(1, cancellationToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
