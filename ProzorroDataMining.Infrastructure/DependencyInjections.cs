using Microsoft.Extensions.DependencyInjection;
using ProzorroDataMining.Application.ApplicationContracts;
using ProzorroDataMining.Application.RepositoryContracts;
using ProzorroDataMining.Infrastructure.DbContext;
using ProzorroDataMining.Infrastructure.HttpClients;
using ProzorroDataMining.Infrastructure.Repositories;
using System;
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
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Infrastructure registrations
        services.AddTransient<DapperDbContext>();

        // Repositories
        services.AddTransient<ITenderRepository, TenderRepository>();
        services.AddTransient<IItemRepository, Repositories.ItemRepository>();

        // Register clients manually (avoid requiring AddHttpClient extension)
        services.AddSingleton<IExternalDataClient>(sp =>
        {
            var http = new System.Net.Http.HttpClient()
            {
                BaseAddress = new Uri("https://public-api.prozorro.gov.ua/api/2.5/"),
                Timeout = TimeSpan.FromSeconds(30)
                
            };

            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ExternalDataClient>>();
            return new ExternalDataClient(http, logger);
        });

        services.AddSingleton<ITenderApiRepository>(sp =>
        {
            var http = new System.Net.Http.HttpClient()
            {
                BaseAddress = new Uri("https://public-api.prozorro.gov.ua/api/2.5/"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            return new TenderApiRepository(http);
        });

        return services;
    }
}
