namespace MongoRabbitA.Models
{
    public class OrderCreateEvent
    {
        public string OrderId {get;set;}=string.Empty;
        public string ProductId {get;set;}=string.Empty;
        public int Amount {get;set;}
    }
}