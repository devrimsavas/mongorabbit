# APPLICATION OVERVIEW

# Rabbit Integration

- 1 Rabbit Download from docker

```bash
docker run -d --name rabbitmq-server -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

- run rabbit
  http://localhost:15672

- 2 Add Package to Project

```bash
dotnet add package RabbitMQ.Client
```

after package is installed, we need to do 2 things

- 1- appsettings.json:RabbitMQ Connection string
  `(HostName:localhost)`

```bash
appsettings.json
"RabbitMQ":"localhost"

```

- 2- Message Model : What to send to message queue
  ` (OrderId,ProductId,Amount)`
  here, we create a new model for message to be sent to queue

```bash
namespace MongoRabbitA.Models
{
    public class OrderCreatedEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Amount { get; set; }
    }
}
```
