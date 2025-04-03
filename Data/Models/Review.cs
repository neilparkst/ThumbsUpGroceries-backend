using System.ComponentModel.DataAnnotations;

namespace ThumbsUpGroceries_backend.Data.Models
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public Guid UserId { get; set; }
        public string Comment { get; set; }
        public float Rating { get; set; }
    }

    public class ReviewManyResponse
    {
        public int ReviewId { get; set; }
        public string UserName { get; set; }
        public string Comment { get; set; }
        public float Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ReviewAddRequest
    {
        [Required]
        public string Comment { get; set; }
        [Required]
        public float Rating { get; set; }
    }

}
