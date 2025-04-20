using Dapper;

using Microsoft.Data.SqlClient;
using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;
        private readonly string _configuration;

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionStrings:DefaultConnection"];
        }

        public async Task<SignupResponse> Signup(SignupRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();


                    var emailExists = await connection.QueryFirstOrDefaultAsync<bool>(
                        "SELECT CASE WHEN EXISTS (SELECT 1 FROM AppUser WHERE Email = @Email) THEN 1 ELSE 0 END",
                        new { Email = request.Email }
                    );
                    if (emailExists)
                    {
                        throw new InvalidDataException("Email already exists");
                    }

                    var result = await connection.QueryFirstOrDefaultAsync<SignupResponse>(
                        "INSERT INTO AppUser (Email, PasswordHash, UserName, PhoneNumber, FirstName, LastName, Address, Role) " +
                        "OUTPUT INSERTED.UserId, INSERTED.Email " +
                        "VALUES (@Email, @PasswordHash, @UserName, @PhoneNumber, @FirstName, @LastName, @Address, @Role)",
                        new
                        {
                            Email = request.Email,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                            UserName = request.UserName,
                            PhoneNumber = request.PhoneNumber,
                            FirstName = request.FirstName,
                            LastName = request.LastName,
                            Address = request.Address,
                            Role = "Customer"
                        }
                    );

                    return result;

                }
                catch (InvalidDataException e)
                {
                    throw e;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while signing up");
                }
            }
        }

        public async Task<User> GetUserInfoByEmail(string email)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var user = await connection.QueryFirstOrDefaultAsync<User>(
                        "SELECT * FROM AppUser WHERE Email = @Email",
                        new { Email = email }
                    );

                    return user;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while signing in");
                }
            }
        }

        public async Task<User> GetUserInfoByUserId(Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var user = await connection.QueryFirstOrDefaultAsync<User>(
                        "SELECT * FROM AppUser WHERE UserId = @UserId",
                        new { UserId = userId }
                    );

                    return user;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching user info");
                }
            }
        }

        public async Task<string?> GetStripeCustomerIdByUserId(Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var stripeCustomerId = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT StripeCustomerId FROM AppUser WHERE UserId = @UserId",
                        new { UserId = userId }
                    );

                    return stripeCustomerId;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching Stripe customer ID");
                }
            }
        }

        public async Task<User> UpdateUserInfo(Guid userId, UserInfoUpdateRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var user = await connection.QueryFirstOrDefaultAsync<User>(
                        "UPDATE AppUser SET " +
                        "UserName = ISNULL(@UserName, UserName), " +
                        "PhoneNumber = ISNULL(@PhoneNumber, PhoneNumber), " +
                        "FirstName = ISNULL(@FirstName, FirstName), " +
                        "LastName = ISNULL(@LastName, LastName), " +
                        "Address = ISNULL(@Address, Address) " +
                        "OUTPUT INSERTED.* " +
                        "WHERE UserId = @UserId",
                        new
                        {
                            UserId = userId,
                            UserName = request.UserName,
                            PhoneNumber = request.PhoneNumber,
                            FirstName = request.FirstName,
                            LastName = request.LastName,
                            Address = request.Address
                        }
                    );

                    return user;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while updating user info");
                }
            }
        }

        public async Task<User> UpdateUserPassword(Guid userId, UserPasswordUpdateRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // update password
                    var user = await connection.QueryFirstOrDefaultAsync<User>(
                        "UPDATE AppUser SET " +
                        "PasswordHash = @PasswordHash " +
                        "OUTPUT INSERTED.* " +
                        "WHERE UserId = @UserId",
                        new
                        {
                            UserId = userId,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword)
                        }
                    );

                    return user;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while updating user password");
                }
            }
        }

        public async Task<User> DeleteUser(Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var user = await connection.QueryFirstOrDefaultAsync<User>(
                        "DELETE FROM AppUser " +
                        "OUTPUT DELETED.* " +
                        "WHERE UserId = @UserId",
                        new { UserId = userId }
                    );

                    return user;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while deleting user info");
                }
            }
        }
    }
}
