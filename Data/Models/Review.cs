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

    public class ReviewAddRequest
    {
        public string Comment { get; set; }
        public float Rating { get; set; }
    }
}
