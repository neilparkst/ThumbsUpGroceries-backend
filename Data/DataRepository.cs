using Dapper;

using Microsoft.Data.SqlClient;
using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data
{
    public class DataRepository : IDataRepository
    {
        private readonly string _connectionString;
        private readonly string _configuration;

        public DataRepository(IConfiguration configuration)
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
                catch(InvalidDataException e)
                {
                    throw e;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while signing up");
                }
            }
        }

        public async Task<DatabaseModel.AppUser> GetUserInfoByEmail(string email)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var user = await connection.QueryFirstOrDefaultAsync<DatabaseModel.AppUser>(
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

        public async Task<List<Category>> GetCategories()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var categories = await connection.QueryAsync<Category>(
                        "SELECT * FROM Category"
                    );

                    return categories.ToList();
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching categories");
                }
            }
        }
    }

}
