# Order Store – ASP.NET Core, MongoDB & RabbitMQ

![Build](https://github.com/devrimsavas/mongorabbit/actions/workflows/test.yml/badge.svg)

An order-processing backend that uses RabbitMQ to handle orders asynchronously. The API stores an order in MongoDB, publishes an event to a queue and returns immediately; a background consumer then reserves stock and updates the order status.

## How it works

```
POST /api/Order
   │
   ├─► MongoDB: order saved as Pending
   ├─► RabbitMQ: OrderCreateEvent published to "order-created-queue"
   └─► 201 Created returned to the client

OrderProcessingConsumer (BackgroundService)
   ├─ stock available    → stock decremented, order → Completed
   ├─ stock insufficient → order → Cancelled
   └─ processing error   → message nacked → dead-letter queue
```

Highlights:

- **Atomic stock reservation** – the stock check and decrement happen in a single `FindOneAndUpdate` with a `QuantityInStock >= amount` filter, so concurrent orders cannot oversell a product.
- **Dead-letter handling** – failed messages are routed through a DLX (`order-dlx`) to `order-dead-letter-queue` instead of being lost.
- **Manual acknowledgements** with `prefetchCount = 1`.
- **Correlation ID** generated per request and carried through the message properties and logs.
- **RabbitMQ.Client v7** with the fully async API.

## Tech stack

- **Backend:** ASP.NET Core (.NET 9), MongoDB.Driver, RabbitMQ.Client, Swagger
- **Frontend:** Angular + Tailwind CSS
- **CI:** GitHub Actions (build)

## Running locally

**1. Start MongoDB and RabbitMQ**

```bash
docker run -d --name mongodb -p 27017:27017 mongo
docker run -d --name rabbitmq-server -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

RabbitMQ management UI: http://localhost:15672 (`guest` / `guest`)

**2. Create `MongoRabbitA/appsettings.json`**

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "productdb",
    "OrderDatabase": "orderdb"
  },
  "RabbitMQ": {
    "HostName": "localhost"
  },
  "AllowedHosts": "*"
}
```

**3. Run the API**

```bash
cd MongoRabbitA
dotnet run
```

Swagger UI: http://localhost:5035/swagger. The product collection is seeded from `Data/Seed/products.json` on first start.

**4. Run the frontend**

```bash
cd front-end
npm install
npm start
```

## API overview

| Method | Endpoint | Description |
| --- | --- | --- |
| GET | `/api/Product` | List products (paged) |
| GET | `/api/Product/{id}` | Get product by id |
| GET | `/api/Product/in-stock` | Products with stock > 0 |
| GET | `/api/Product/brand/{brand}` | Products by brand |
| POST | `/api/Product` | Create product |
| POST | `/api/Product/bulk` | Create multiple products |
| PUT | `/api/Product/{id}` | Update product |
| PATCH | `/api/Product/{id}/stock` | Adjust stock |
| DELETE | `/api/Product/{id}` | Delete product |
| GET | `/api/Order` | List orders |
| GET | `/api/Order/correlation/{correlationId}` | Get order by correlation id |
| POST | `/api/Order` | Create order (processed asynchronously) |

## Roadmap

- Docker Compose for the full stack
- Persistent messages and a long-lived publisher connection
- Retry with backoff and idempotent message handling
- Outbox pattern for reliable publishing
- Checkout and live order status in the Angular frontend
- Integration tests in CI
