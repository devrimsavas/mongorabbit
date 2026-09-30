using Microsoft.AspNetCore.Mvc;
using MongoRabbitA.DTOs.ProductDTOs;
using MongoRabbitA.Models;
using MongoRabbitA.Services;

namespace MongoRabbitA.Controllers.ProductControllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class ProductController:ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public ProductController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }
        //Get All Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] int pageSize = 10, [FromQuery] int pageNumber = 1)
        {
            var products = await _mongoDbService.GetAllProductsAsync(pageSize, pageNumber);
            return Ok(products); 
        }
        //add a product
        [HttpPost] 
        public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductDTO dto)
        {
            var createdProduct=await _mongoDbService.CreateProductAsync(dto);
            // Returns 201 Created with the location header and the created product
            return CreatedAtAction(nameof(GetProducts), new {id=createdProduct.Id},createdProduct);

        }
        [HttpPost("bulk")]
        public async Task<ActionResult<IEnumerable<Product>>> InsertManyProduct([FromBody] IEnumerable<CreateProductDTO> dtos)
        {
            var createdProducts=await _mongoDbService.InsertManyProductAsync(dtos);
            return Ok(createdProducts);
        }




        // GET /api/Product/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(string id)
        {
            try
            {
                var product = await _mongoDbService.GetProductById(id);
                return Ok(product);
            }
            catch (Exception)
            {
                return NotFound(new { message = $"Product with ID '{id}' was not found." });
            }
        }

        // GET /api/Product/by-name/{name}
        [HttpGet("by-name/{name}")]
        public async Task<ActionResult<Product>> GetProductByName(string name)
        {
            var product = await _mongoDbService.GetProduct(name);
            if (product is null)
            {
                return NotFound(new { message = $"Product named '{name}' was not found." });
            }
            return Ok(product);
        }

        // GET /api/Product/in-stock
        [HttpGet("in-stock")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsInStock()
        {
            var inStockProducts = await _mongoDbService.GetProductInStockAsync();
            return Ok(inStockProducts);
        }

        // GET /api/Product/brand/{brand}
        [HttpGet("brand/{brand}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByBrand(string brand)
        {
            try
            {
                var products = await _mongoDbService.GetProductSameBrandAsync(brand);
                return Ok(products);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/Product/count
        [HttpGet("count")]
        public async Task<ActionResult<long>> GetProductCount()
        {
            var count = await _mongoDbService.CountDocumentAsync();
            return Ok(count);
        }

        // PUT /api/Product/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(string id, [FromBody] UpdateProductDTO updateDto)
        {
            var updatedProduct = await _mongoDbService.UpdateProductAsync(id, updateDto);
            if (updatedProduct is null)
            {
                return NotFound(new { message = $"Product with ID '{id}' was not found." });
            }
            return Ok(updatedProduct);
        }

        // PATCH /api/Product/{id}/stock
        [HttpPatch("{id}/stock")]
        public async Task<ActionResult<Product>> UpdateStock(string id, [FromBody] UpdateStockDTO stockDto)
        {
            var updatedProduct = await _mongoDbService.UpdateStock(id, stockDto.Quantity);
            if (updatedProduct is null)
            {
                return BadRequest(new { message = "Update failed. Product not found or insufficient stock for decrement." });
            }
            return Ok(updatedProduct);
        }

        // DELETE /api/Product/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(string id)
        {
            try
            {
                var deletedProductName = await _mongoDbService.DeleteProductByIdAsync(id);
                if (deletedProductName is null)
                {
                    return NotFound(new { message = $"Product with ID '{id}' was not found." });
                }
                return Ok(new { message = $"Product '{deletedProductName}' successfully deleted." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        
    }

    
}