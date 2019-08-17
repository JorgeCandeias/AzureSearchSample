using Microsoft.Azure.Search;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Index = Microsoft.Azure.Search.Models.Index;

namespace AzureSearch
{
    public class MyService : IHostedService
    {
        private readonly ILogger<MyService> logger;
        private readonly MyServiceOptions options;

        public MyService(ILogger<MyService> logger, IOptions<MyServiceOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await CreateIndexAsync().ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task CreateIndexAsync()
        {
            var index = new Index()
            {
                Name = "hotels",
                Fields = FieldBuilder.BuildForType<Hotel>()
            };

            using (var client = CreateSearchServiceClient())
            {
                await client.Indexes.CreateOrUpdateAsync(index).ConfigureAwait(false);
            }

            logger.LogInformation("Created Index {@Index}", index.Name);
        }

        private SearchServiceClient CreateSearchServiceClient() =>
            new SearchServiceClient(options.SearchServiceName, new SearchCredentials(options.AdminApiKey));
    }
}
