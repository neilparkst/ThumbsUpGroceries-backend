using System.ComponentModel.DataAnnotations;

namespace ThumbsUpGroceries_backend.Data.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public float Price { get; set; }
        public PriceUnitType PriceUnitType { get; set; }
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
        public PriceUnitType PriceUnitType { get; set; } = PriceUnitType.ea;
        public string? Description { get; set; }
        public List<string>? Images { get; set; }
        public float Quantity { get; set; }
        public List<int> Categories { get; set; } = [];
        public float? Rating { get; set; }
        public int? ReviewCount { get; set; }
    }

    public class ProductManyResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public float Price { get; set; }
        public PriceUnitType PriceUnitType { get; set; } = PriceUnitType.ea;
        public string Image { get; set; } = string.Empty;
        public float Rating { get; set; }
        public int ReviewCount { get; set; }
    }

        public class ProductAddRequest
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public float? Price { get; set; }
        [Required]
        public PriceUnitType PriceUnitType { get; set; }
        public string? Description { get; set; }
        public List<IFormFile>? Images { get; set; }
        [Required]
        public float? Quantity { get; set; }
        [Required]
        public List<int> Categories { get; set; }
    }

    public class ProductUpdateRequest
    {
        public string? Name { get; set; }
        public float? Price { get; set; }
        public PriceUnitType? PriceUnitType { get; set; }
        public string? Description { get; set; }
        public List<IFormFile>? Images { get; set; }
        public float? Quantity { get; set; }
        public float? AddedQuantity { get; set; }
        public List<int>? Categories { get; set; }
    }
}
