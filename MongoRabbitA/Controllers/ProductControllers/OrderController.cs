using Microsoft.AspNetCore.Mvc;
using MongoRabbitA.DTOs.OrderDTOs;
using MongoRabbitA.Models;
using MongoRabbitA.Services;

namespace MongoRabbitA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly MongoOrderService _orderService;
        private readonly RabbitMQPublisher _rabbitPublisher;

        public OrderController(MongoOrderService orderService,RabbitMQPublisher rabbitPublisher)
        {
            _orderService = orderService;
            _rabbitPublisher=rabbitPublisher;
        }

        // GET /api/Order
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync(); // Service'teki metodun
            return Ok(orders);
        }

        // POST /api/Order
        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderDTO dto)
        {
            var createdOrder = await _orderService.CreateOrderAsync(dto);

            //publish event to RabbitMQ
            var orderEvent=new OrderCreateEvent
            {
                OrderId=createdOrder.Id!,
                ProductId=createdOrder.ProductId,
                Amount=createdOrder.Amount
            };
            await _rabbitPublisher.PublishOrderCreatedAsync(orderEvent);

            // Returns 201 Created
            return CreatedAtAction(nameof(GetAllOrders), new { id = createdOrder.Id }, createdOrder);
        }
    }
}