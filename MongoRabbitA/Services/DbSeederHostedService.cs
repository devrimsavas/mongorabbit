using System.Text.Json;
using MongoRabbitA.DTOs.ProductDTOs;

namespace MongoRabbitA.Services
{
    public class DbSeederHostedService : IHostedService
    {
        private readonly MongoDbService _mongoDbService;
        private readonly IHostEnvironment _env;

        public DbSeederHostedService(MongoDbService mongoDbService, IHostEnvironment env)
        {
            _mongoDbService = mongoDbService;
            _env = env;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Check if collection already has documents
            var count = await _mongoDbService.CountDocumentAsync();
            if (count > 0)
            {
                return;
            }

            // Locate file reliably using environment root
            var filePath = Path.Combine(_env.ContentRootPath, "Data", "Seed", "products.json");
            if (!File.Exists(filePath))
            {
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            await using var stream = File.OpenRead(filePath);
            var products = await JsonSerializer.DeserializeAsync<List<CreateProductDTO>>(stream, options, cancellationToken);

            if (products is not null && products.Count > 0)
            {
                await _mongoDbService.InsertManyProductAsync(products);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}