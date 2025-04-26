using System.ComponentModel.DataAnnotations;

namespace ThumbsUpGroceries_backend.Data.Models
{
    public class SignupRequest
    {
        [Required(ErrorMessage = "email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255)]
        public string Email { get; set; }

        [Required(ErrorMessage = "password is required")]
        public string Password { get; set; }

        [Required(ErrorMessage = "username is required")]
        [StringLength(100)]
        public string UserName { get; set; }

        [StringLength(100)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }
    }
}
