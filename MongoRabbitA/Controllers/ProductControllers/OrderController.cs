using System.Diagnostics;
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
        private readonly ILogger<OrderController> _logger;

        public OrderController(MongoOrderService orderService,RabbitMQPublisher rabbitPublisher,ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _rabbitPublisher=rabbitPublisher;
            _logger=logger;

        }

        // GET /api/Order
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync(); // Service'teki metodun
            return Ok(orders);
        }

        //GET order by Correlation Id
        [HttpGet("correlation/{correlationId}")]
        public async Task<ActionResult<Order>> GetOrderByCorrelationId(string correlationId)
        {
            var order=await _orderService.GetOrderByCorrelationIdAsync(correlationId);
            if (order==null)
            {
                return NotFound($"Order with CorrelationId {correlationId} not found") ;
            }
            return Ok(order);
        }

        // POST /api/Order
        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderDTO dto)
        {
            var correlationId=Guid.NewGuid().ToString();
            //request came log with Id 
            _logger.LogInformation("[{CorrelationId}] Order creation initiated for ProductId: {ProductId}", correlationId, dto.ProductId);
            var createdOrder = await _orderService.CreateOrderAsync(dto,correlationId);

            //publish event to RabbitMQ
            var orderEvent=new OrderCreateEvent
            {
                OrderId=createdOrder.Id!,
                ProductId=createdOrder.ProductId,
                Amount=createdOrder.Amount
            };
            await _rabbitPublisher.PublishOrderCreatedAsync(orderEvent,correlationId);
            

            // Returns 201 Created
            return CreatedAtAction(nameof(GetAllOrders), new { id = createdOrder.Id }, createdOrder);
        }

    }
}