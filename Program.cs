using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AzureSearch
{
    class Program
    {
        static Task Main()
        {
            return new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddUserSecrets<Program>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<MyServiceOptions>(options =>
                    {
                        options.SearchServiceName = context.Configuration["SearchServiceName"];
                        options.AdminApiKey = context.Configuration["AdminApiKey"];
                    });

                    services.AddHostedService<MyService>();
                })
                .RunConsoleAsync();
        }
    }
}
