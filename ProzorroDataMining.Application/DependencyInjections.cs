using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ProzorroDataMining.Application.Services;
using ProzorroDataMining.Application.ApplicationContracts;

namespace ProzorroDataMining.Application;

public static class DependencyInjections
{
    /// <summary>
    /// Extension method to add infrastructure services to the dependency injection container.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<SemaphoreSlim>(new SemaphoreSlim(1, 1));
        services.AddTransient<IDataSyncService, DataSyncService>();
        services.AddTransient<ITenderService, TenderService>();
        return services;
    }
}
