using Azure.Core;
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

        public async Task<int> AddProduct(ProductAddRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    List<string> imagePaths = new();

                    if (request.Images != null && request.Images.Count > 0)
                    {
                        foreach (var file in request.Images)
                        {
                            if (file.Length > 0)
                            {
                                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                                var filePath = Path.Combine(uploadFolder, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                imagePaths.Add($"/images/products/{fileName}");
                            }
                        }
                    }
                    
                    await connection.OpenAsync();

                    var productId = await connection.QueryFirstOrDefaultAsync<int>(
                        "INSERT INTO Product (Name, Price, PriceUnitType, Description, Images, Quantity) " +
                        "OUTPUT INSERTED.ProductId " +
                        "VALUES (@Name, @Price, @PriceUnitType, @Description, @Images, @Quantity)",
                        new
                        {
                            Name = request.Name,
                            Price = request.Price,
                            PriceUnitType = request.PriceUnitType,
                            Description = request.Description ?? (object)DBNull.Value,
                            Images = imagePaths.Count > 0 ? string.Join(",", imagePaths) : (object)DBNull.Value,
                            Quantity = request.Quantity
                        }
                    );

                    return productId;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while adding product");
                }
            }
        }
    }

}
