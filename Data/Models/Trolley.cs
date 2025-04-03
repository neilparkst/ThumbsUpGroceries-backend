namespace ThumbsUpGroceries_backend.Data.Models
{
    public class Trolley
    {
        public int TrolleyId { get; set; }
        public Guid UserId { get; set; }
        public int ItemCount { get; set; }
    }
}
