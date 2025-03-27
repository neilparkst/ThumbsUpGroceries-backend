using System.ComponentModel.DataAnnotations;

namespace ThumbsUpGroceries_backend.Data.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public float Price { get; set; }
        public string PriceUnitType { get; set; }
        public string? Description { get; set; }
        public string? Images { get; set; }
        public float Quantity { get; set; }
        public List<int> Categories { get; set; }
        public float? Rating { get; set; }
        public int? ReviewCount { get; set; }
    }

    public class ProductResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public float Price { get; set; }
        public string PriceUnitType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string>? Images { get; set; }
        public float Quantity { get; set; }
        public List<int> Categories { get; set; } = [];
        public float? Rating { get; set; }
        public int? ReviewCount { get; set; }
    }

    public class ProductAddRequest
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public float? Price { get; set; }
        [Required]
        public string PriceUnitType { get; set; }
        public string? Description { get; set; }
        public List<IFormFile>? Images { get; set; }
        [Required]
        public float? Quantity { get; set; }
        [Required]
        public List<int> Categories { get; set; }
    }
}
