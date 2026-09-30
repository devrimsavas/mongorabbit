using System.ComponentModel.DataAnnotations;
namespace MongoRabbitA.DTOs.OrderDTOs
{
    public class CreateOrderDTO
    {
        [Required]
        public string ProductId {get;set;}=string.Empty;
        [Range(1,int.MaxValue,ErrorMessage ="Amount must be at least 1")]
        public int Amount {get;set;}
    }
}