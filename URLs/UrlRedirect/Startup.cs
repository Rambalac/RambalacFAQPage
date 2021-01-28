using DataStorage.Azure;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(RambalacHome.Function.Startup))]
namespace RambalacHome.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var services = builder.Services;
            var configuration = builder.GetContext().Configuration;
            var settings = configuration.Get<FunctionSettings>();
            services.AddSingleton(settings);
            services.AddSingleton<ITableStorage>(new AzureTableStorage(settings.Storage.ConnectionString));

            services.AddMemoryCache(options =>
            {
                options.SizeLimit = settings.MemoryCacheLimit;
            });
        }
    }
}