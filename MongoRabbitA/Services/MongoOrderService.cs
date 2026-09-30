using MongoDB.Driver;
using MongoRabbitA.Models;
using MongoRabbitA.DTOs.OrderDTOs;

namespace MongoRabbitA.Services
{
    public class MongoOrderService
    {
        //injection 
        private readonly IMongoCollection<Order> _orders;
        public MongoOrderService(IConfiguration config)
        {
            var connectionString=config.GetSection("MongoDB:ConnectionString").Value;
            var databaseName=config.GetSection("MongoDB:OrderDatabase").Value;
            var client=new MongoClient(connectionString);
            var database=client.GetDatabase(databaseName);
            _orders=database.GetCollection<Order>("ordercollection");            
        }

        //count document
        public async Task<long> CountDocumentAsync()
        {
            var filter=Builders<Order>.Filter.Empty;
            var documentCount=await _orders.CountDocumentsAsync(filter);
            return documentCount;
        }
        //find all 
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var filter=Builders<Order>.Filter.Empty;
            var orders=await _orders.Find(filter).ToListAsync();
            return orders;
        }

        // Create order
        public async Task<Order> CreateOrderAsync(CreateOrderDTO dto)
        {
            var order = new Order
            {
                ProductId = dto.ProductId,
                Amount = dto.Amount,
                OrderStatus = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _orders.InsertOneAsync(order);
            return order;
        }

        // Update status for RabbitMQ flow
        public async Task<bool> UpdateOrderStatusAsync(string orderId, OrderStatus status)
        {
            var filter = Builders<Order>.Filter.Eq(o => o.Id, orderId);
            var update = Builders<Order>.Update.Set(o => o.OrderStatus, status);

            var result = await _orders.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
    }


}