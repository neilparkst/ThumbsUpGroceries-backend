namespace ThumbsUpGroceries_backend.Data.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public Guid UserId { get; set; }
        public int SubTotalAmount { get; set; }
        public TrolleyMethod ServiceMethod { get; set; }
        public int BagFee { get; set; }
        public int ServiceFee { get; set; }
        public int TotalAmount { get; set; }
        public string ChosenAddress { get; set; }
        public DateTime ChosenDate { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public string TransactionId { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class OrderMany
    {
        public int OrderId { get; set; }
        public TrolleyMethod ServiceMethod { get; set; }
        public int TotalAmount { get; set; }
        public string ChosenAddress { get; set; }
        public DateTime ChosenDate { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Price { get; set; }
        public PriceUnitType PriceUnitType { get; set; } = PriceUnitType.ea;
        public string? Image { get; set; }
        public int Quantity { get; set; }
        public int TotalPrice { get; set; }
    }

    public class OrderContent
    {
        public int OrderId { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public int SubTotalAmount { get; set; }
        public TrolleyMethod ServiceMethod { get; set; }
        public int BagFee { get; set; }
        public int ServiceFee { get; set; }
        public int TotalAmount { get; set; }
        public string ChosenAddress { get; set; }
        public DateTime ChosenDate { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class CancelOrderResponse
    {
        public int OrderId { get; set; }
        public OrderStatus OrderStatus { get; set; }
    }
}
