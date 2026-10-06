using System.Text;
using System.Text.Json;
using MongoRabbitA.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MongoRabbitA.Services
{
    public class OrderProcessingConsumer : BackgroundService
    {
        private readonly ConnectionFactory _factory;
        private readonly MongoDbService _productService;
        private readonly MongoOrderService _orderService;
        private readonly ILogger<OrderProcessingConsumer> _logger;

        public OrderProcessingConsumer(
            IConfiguration config,
            MongoDbService productService,
            MongoOrderService orderService,
            ILogger<OrderProcessingConsumer> logger)
        {
            var hostName = config["RabbitMQ:HostName"] ?? "localhost";
            _factory = new ConnectionFactory { HostName = hostName };
            _productService = productService;
            _orderService = orderService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // RabbitMQ v7 Async Connection & Channel
            var connection = await _factory.CreateConnectionAsync(stoppingToken);
            var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            //rabbit queue declaration 
            // Quarantina Boxes 
            ///DLX 
            var dlxExchange="order-dlx";
            await channel.ExchangeDeclareAsync(
                exchange:dlxExchange,
                type:ExchangeType.Direct,
                durable:true,
                autoDelete:false,
                cancellationToken:stoppingToken
            );
            //DLQ
            var dlqQueue="order-dead-letter-queue";
            await channel.QueueDeclareAsync(
                queue:dlqQueue,
                durable:true,
                exclusive:false,
                autoDelete:false,
                arguments:null,
                cancellationToken:stoppingToken
            );
            var dlqRoutingKey="order-dl-key";
            await channel.QueueBindAsync(
                queue:dlqQueue,
                exchange:dlxExchange,
                routingKey:dlqRoutingKey,
                cancellationToken:stoppingToken
            );
            //adding to main queue : post dlx if there is error
            var mainQueueArgs=new Dictionary<string,object?>
            {
                {"x-dead-letter-exchange",dlxExchange},
                {"x-dead-letter-routing-key",dlqRoutingKey}
            };
            //main queue declaration with rules above
            var mainQueue=RabbitMQPublisher.QueueName;



            await channel.QueueDeclareAsync(
                queue: mainQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: mainQueueArgs,
                cancellationToken: stoppingToken
            );

            // Fetch one message at a time
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    //test line for error 
                    //throw new Exception("Simulated fatal database error");

                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var orderEvent = JsonSerializer.Deserialize<OrderCreateEvent>(message);

                    if (orderEvent != null)
                    {
                        _logger.LogInformation("Processing order {OrderId} for product {ProductId}", orderEvent.OrderId, orderEvent.ProductId);
                        
                        var updatedProduct = await _productService.UpdateStock(orderEvent.ProductId, -orderEvent.Amount);

                        if (updatedProduct != null)
                        {
                            //stock sufficient decrease stock 
                            await _orderService.UpdateOrderStatusAsync(orderEvent.OrderId,OrderStatus.Completed);
                            _logger.LogInformation(" Order {OrderId} Completed. Remaining stock: {Stock}", orderEvent.OrderId,updatedProduct.QuantityInStock);
                        }
                        else
                        {
                            //Stock insufficient.Cancel Order
                            await _orderService.UpdateOrderStatusAsync(orderEvent.OrderId,OrderStatus.Cancelled);
                            _logger.LogWarning("Insufficient stock or product not found for ProductId: {ProductId}", orderEvent.ProductId);
                        }
                    }

                    // Acknowledge the message so RabbitMQ removes it
                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message {DeliveryTag}", ea.DeliveryTag);
                    //requeue false: this message is wrong. delete from queue
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            // 1. Start listening to the queue (MUST be outside ReceivedAsync)
            await channel.BasicConsumeAsync(
                queue: mainQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );

            // 2. Keep the background service alive
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}