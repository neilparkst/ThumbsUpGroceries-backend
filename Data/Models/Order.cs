namespace ThumbsUpGroceries_backend.Data.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public Guid UserId { get; set; }
        public double SubTotalAmount { get; set; }
        public TrolleyMethod ServiceMethod { get; set; }
        public double BagFee { get; set; }
        public double ServiceFee { get; set; }
        public double TotalAmount { get; set; }
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
        public double TotalAmount { get; set; }
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
        public float ProductPrice { get; set; }
        public PriceUnitType ProductPriceUnitType { get; set; } = PriceUnitType.ea;
        public string? Image { get; set; }
        public float Quantity { get; set; }
        public float TotalPrice { get; set; }
    }

    public class OrderContent
    {
        public int OrderId { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public double SubTotalAmount { get; set; }
        public TrolleyMethod ServiceMethod { get; set; }
        public double BagFee { get; set; }
        public double ServiceFee { get; set; }
        public double TotalAmount { get; set; }
        public string ChosenAddress { get; set; }
        public DateTime ChosenDate { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class CancelOrderReponse
    {
        public int OrderId { get; set; }
        public OrderStatus OrderStatus { get; set; }
    }
}
