using System.ComponentModel.Design.Serialization;
using System.Text;
using System.Text.Json;
using MongoRabbitA.Models;
using RabbitMQ.Client;

namespace MongoRabbitA.Services
{
    public class RabbitMQPublisher
    {
        //injection 
        private readonly ConnectionFactory _factory;
        public const string QueueName="order-created-queue";
        public RabbitMQPublisher(IConfiguration config)
        {
            //read from appsettings

            var hostName=config["RabbitMQ:HostName"] ?? "localhost";
            _factory=new ConnectionFactory {HostName=hostName};
        }
        public async Task PublishOrderCreatedAsync(OrderCreateEvent orderEvent)
        {
            //rabbit 7:create connection and channel async 
            using var connection=await _factory.CreateConnectionAsync();
            using var channel=await connection.CreateChannelAsync();
            //declare queue if it does not exist
            await channel.QueueDeclareAsync(
                queue:QueueName,
                durable:true,
                exclusive:false,
                autoDelete:false,
                arguments:null
            );
            //serialize event model to JSON bytes
            var jsonString=JsonSerializer.Serialize(orderEvent);
            var body=Encoding.UTF8.GetBytes(jsonString);
            //publish message 
            await channel.BasicPublishAsync(
                exchange:string.Empty,
                routingKey:QueueName,
                body:body
            );
        }

    }
}