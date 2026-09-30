using System.ComponentModel.DataAnnotations;
namespace MongoRabbitA.DTOs.ProductDTOs
{
    public class CreateProductDTO
    {
        [Required(ErrorMessage ="Product Name is Required")]
        public string Name {get;set;}=string.Empty;
        [Required(ErrorMessage ="Brand is Required")]
        public string Brand {get;set;}=string.Empty;
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        public int QuantityInStock { get; set; }

    }
}