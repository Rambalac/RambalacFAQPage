using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using DataStorage.Azure;

namespace RambalacHome.Function
{
    public class Program
    {
        public static async Task Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) => ConfigureServices(context, services))
                .Build();

            await host.RunAsync();
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;
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