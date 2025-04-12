using System.ComponentModel.DataAnnotations;

namespace ThumbsUpGroceries_backend.Data.Models
{
    public class Trolley
    {
        public int TrolleyId { get; set; }
        public Guid UserId { get; set; }
        public int ItemCount { get; set; }
        public string Method { get; set; } = string.Empty;
    }

    public class TrolleyItem
    {
        public int TrolleyItemId { get; set; }
        public int TrolleyId { get; set; }
        public int ProductId { get; set; }
        public PriceUnitType PriceUnitType { get; set; } = PriceUnitType.ea;
        public float Quantity { get; set; }
    }

    public class TrolleyCountResponse
    {
        public int TrolleyId { get; set; }
        public int ItemCount { get; set; }
    }

    public class TrolleyItemRequest
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        public PriceUnitType PriceUnitType { get; set; }
        [Required]
        public float Quantity { get; set; }
    }

    public class TrolleyItemResponse
    {
        public int TrolleyItemId { get; set; }
        public int ProductId { get; set; }
        public PriceUnitType PriceUnitType { get; set; } = PriceUnitType.ea;
        public float Quantity { get; set; }
    }

    public class TrolleyItemDeleteResponse
    {
        public int TrolleyItemId { get; set; }
        public int ProductId { get; set; }
    }

    public class TrolleyItemMany
    {
        public int TrolleyItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public float ProductPrice { get; set; }
        public PriceUnitType ProductPriceUnitType { get; set; } = PriceUnitType.ea;
        public string Image { get; set; } = string.Empty;
        public float Quantity { get; set; }
        public float TotalPrice { get; set; }
    }

    public class TrolleyContentResponse
    {
        public int TrolleyId { get; set; }
        public int ItemCount { get; set; }
        public List<TrolleyItemMany> Items { get; set; } = new List<TrolleyItemMany>();
        public float SubTotalPrice { get; set; }
        public string Method { get; set; } = string.Empty;
        public float ServiceFee { get; set; }
        public float BagFee { get; set; }
        public float TotalPrice { get; set; }
    }
}