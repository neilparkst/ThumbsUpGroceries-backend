using System.ComponentModel.DataAnnotations;

namespace ThumbsUpGroceries_backend.Data.Models
{
    public class Membership
    {
        public int PlanId { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int DurationMonths { get; set; }
        public string Description { get; set; }
        public string StripePriceId { get; set; }
    }

    public class MembershipMany
    {
        public int PlanId { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int DurationMonths { get; set; }
        public string Description { get; set; }
    }

    public class UserMembership
    {
        public int MembershipId { get; set; }
        public Guid UserId { get; set; }
        public int PlanId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime RenewalDate { get; set; }
        public string Status { get; set; }
    }

    public class MembershipCheckoutSessionRequest
    {
        [Required]
        public int planId { get; set; }
        [Required]
        public string SuccessUrl { get; set; }
        [Required]
        public string CancelUrl { get; set; }
    }
}