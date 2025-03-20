namespace ThumbsUpGroceries_backend.Data.Models
{
    public class DatabaseModel
    {
        public class AppUser
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
        }
    }
}
