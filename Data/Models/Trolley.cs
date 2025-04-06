using System.ComponentModel.DataAnnotations;

namespace ThumbsUpGroceries_backend.Data.Models
{
    public class Trolley
    {
        public int TrolleyId { get; set; }
        public Guid UserId { get; set; }
        public int ItemCount { get; set; }
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
        public string PriceUnitType { get; set; }
        [Required]
        public float Quantity { get; set; }
    }

    public class TrolleyItemResponse
    {
        public int TrolleyId { get; set; }
        public int ProductId { get; set; }
        public string PriceUnitType { get; set; } = string.Empty;
        public float Quantity { get; set; }
    }
}