using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
namespace MongoRabbitA.Models
{
    public  enum OrderStatus
    {
        Pending,
        Processing,
        Completed,
        Cancelled
    }
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get;set;}
        
        public string CorrelationId{get;set;}=string.Empty;
        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProductId {get;set;}=null!;
        [Required]
        [Range(1,int.MaxValue,ErrorMessage ="Amount must be at least 1")]
        public int Amount {get;set;}
        [BsonRepresentation(BsonType.String)]
        public OrderStatus OrderStatus {get;set;}=OrderStatus.Pending;
        public DateTime CreatedAt {get;set;}=DateTime.UtcNow;
    }
}