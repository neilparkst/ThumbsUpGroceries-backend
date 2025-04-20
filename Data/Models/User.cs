using System.ComponentModel.DataAnnotations;

namespace ThumbsUpGroceries_backend.Data.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string Role { get; set; }
        public string StripeCustomerId { get; set; }
    }

    public class UserInfoResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UserInfoUpdateRequest
    {
        [StringLength(100)]
        public string? UserName { get; set; }
        [StringLength(100)]
        public string? PhoneNumber { get; set; }
        [StringLength(100)]
        public string? FirstName { get; set; }
        [StringLength(100)]
        public string? LastName { get; set; }
        [StringLength(255)]
        public string? Address { get; set; }
    }

    public class UserPasswordUpdateRequest
    {
        [Required]
        public string OldPassword { get; set; }
        [Required]
        public string NewPassword { get; set; }
    }
}
