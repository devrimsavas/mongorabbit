# APPLICATION OVERVIEW
![Test](https://github.com/devrimsavas/mongorabbit/blob/main/.github/workflows/test.yml/badge.svg)

## RabbitMQ Integration (Async Queue Pattern)

### 1. Run RabbitMQ via Docker

**First time only:** create and start the RabbitMQ container with the management plugin enabled:

```bash
docker run -d --name rabbitmq-server -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

**Afterwards:** the container already exists, so just start it:

```bash
docker start rabbitmq-server
```

> If you get a `container name "/rabbitmq-server" is already in use` error, the container was created before. Use `docker start rabbitmq-server` instead of `docker run`.

- **AMQP Port:** `5672` (Application communication)
- **Management UI:** `http://localhost:15672` (Credentials: `guest` / `guest`)

---

### 2. Install Client Package

Install the official client (v7+ uses fully asynchronous methods):

```bash
dotnet add package RabbitMQ.Client
```

---

### 3. Configuration (`appsettings.json`)

Define the host configuration:

```json
"RabbitMQ": {
  "HostName": "localhost"
}
```

---

### 4. Message Model / Contract (`OrderCreatedEvent.cs`)

Define the minimal payload transferred through the queue:

```csharp
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

---

### 5. Publisher Service (`RabbitMQPublisher.cs`)

Handles connection lifecycle and publishing messages using `RabbitMQ.Client` v7. The flow consists of these specific steps:

1. **ConnectionFactory:** Gets configuration (connection string) via `IConfiguration`.
2. **CreateConnectionAsync:** Connects to the RabbitMQ host.
3. **CreateChannelAsync:** Opens a virtual channel for communication.
4. **QueueDeclareAsync:** Checks if the queue (`order-created-queue`) exists; creates it if it doesn't.
5. **Serialization:** Converts the C# object to a JSON string and encodes it into a UTF-8 `byte[]` array (the body).
6. **BasicPublishAsync:** Delivers the payload body to the queue.

---

### 6. Dependency Injection & Controller Trigger

Register the publisher and trigger it inside the order creation flow:

- **Service Registration (`Program.cs`):**

  ```csharp
  builder.Services.AddSingleton<RabbitMQPublisher>();
  ```

- **Controller Flow (`OrderController.cs`):**
  1. Accept incoming `POST /api/Order` request.
  2. Save order to MongoDB with initial status `Pending` (`0`).
  3. Map created order details to `OrderCreatedEvent`.
  4. Publish the event to the queue.
  5. Return `HTTP 201 Created` immediately (the request goes to the queue, it doesn't wait for heavy processing on the server).
