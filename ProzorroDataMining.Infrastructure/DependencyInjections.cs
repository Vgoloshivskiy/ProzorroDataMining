using Microsoft.Extensions.DependencyInjection;
using ProzorroDataMining.Application.ApplicationContracts;
using ProzorroDataMining.Application.RepositoryContracts;
using ProzorroDataMining.Infrastructure.DbContext;
using ProzorroDataMining.Infrastructure.Repositories;
using ProzorroDataMining.Infrastructure.HttpClients;
using System;
using Polly;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace ProzorroDataMining.Infrastructure;
public static class DependencyInjections
{
    /// <summary>
    /// Extension method to add infrastructure services to the dependency injection container.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <summary>
    /// Registers infrastructure services, repositories, HttpClient typed clients and related policies.
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Infrastructure registrations
        services.AddTransient<DapperDbContext>();

        // Repositories
        services.AddTransient<ITenderRepository, TenderRepository>();
        services.AddTransient<ProzorroDataMining.Application.RepositoryContracts.ITenderAnalyticsRepository, AnalyticsRepository>();

        // Register a shared in-process rate limiter
        services.AddSingleton<ProzorroDataMining.Infrastructure.Utilities.SimpleTokenBucketRateLimiter>(sp =>
            new ProzorroDataMining.Infrastructure.Utilities.SimpleTokenBucketRateLimiter(100, 100, TimeSpan.FromSeconds(1)));

        // Configure HttpClientFactory-based typed clients with Polly retry and rate-limit delegating handler
        // Retry policy: handle transient errors and 429 responses, use exponential backoff with jitter and honor Retry-After when present
        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode == 429 || (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(5, retryAttempt =>
            {
                var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, 100));
                return TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt - 1)) + jitter;
            });

        services.AddHttpClient<ITenderApiRepository, TenderApiRepository>(client =>
        {
            client.BaseAddress = new Uri("https://public-api.prozorro.gov.ua/api/2.5/");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler(sp => new RateLimitHandler(sp.GetRequiredService<ProzorroDataMining.Infrastructure.Utilities.SimpleTokenBucketRateLimiter>()))
        .AddPolicyHandler(retryPolicy);

        return services;
    }
}
