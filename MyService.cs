using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
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
            await ImportDataAsync().ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private SearchServiceClient CreateSearchServiceClient() =>
            new SearchServiceClient(options.SearchServiceName, new SearchCredentials(options.AdminApiKey));

        private async Task CreateIndexAsync()
        {
            var index = new Index()
            {
                Name = options.IndexName,
                Fields = FieldBuilder.BuildForType<Hotel>()
            };

            using (var client = CreateSearchServiceClient())
            {
                await client.Indexes.CreateOrUpdateAsync(index).ConfigureAwait(false);
            }

            logger.LogInformation("Created index {@Index}", index.Name);
        }

        private async Task ImportDataAsync()
        {
            var data = await File.ReadAllLinesAsync(options.HotelFileName).ConfigureAwait(false);
            var hotels = data.Skip(1).Select(r =>
            {
                var columns = r.Split("\t");
                return new Hotel
                {
                    HotelId = columns[0],
                    HotelName = columns[1],
                    Description = columns[2],
                    DescriptionFr = columns[3],
                    Category = columns[4],
                    Tags = columns[5].Split(",").ToImmutableArray(),
                    ParkingIncluded = columns[6] == "0" ? false : true,
                    SmokingAllowed = columns[7] == "0" ? false : true,
                    LastRenovationDate = Convert.ToDateTime(columns[8], CultureInfo.InvariantCulture),
                    BaseRate = Convert.ToDouble(columns[9], CultureInfo.InvariantCulture),
                    Rating = Convert.ToInt32(Convert.ToDouble(columns[10], CultureInfo.InvariantCulture))
                };
            }).ToList();

            var actions = hotels.Select(h => IndexAction.Upload(h)).ToList();

            var batch = IndexBatch.New(actions);

            using (var client = CreateSearchServiceClient())
            {
                await client.Indexes.GetClient(options.IndexName).Documents.IndexAsync(batch).ConfigureAwait(false);
            }

            logger.LogInformation("Imported {@Count} hotels", hotels.Count);
        }
    }
}