using Dapper;

using Microsoft.Data.SqlClient;
using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;
        private readonly string _configuration;

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionStrings:DefaultConnection"];
        }

        public async Task<List<Category>> GetAllCategories()
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

        public async Task<Product?> GetProduct(int productId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var product = await connection.QueryFirstOrDefaultAsync<Product>(
                        "SELECT * FROM Product WHERE ProductId = @ProductId",
                        new { ProductId = productId }
                    );

                    return product;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching product");
                }
            }
        }

        public async Task<List<int>> GetCategoriesByProduct(int productId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var categories = await connection.QueryAsync<int>(
                        "SELECT CategoryId FROM ProductCategoryXRef WHERE ProductId = @ProductId",
                        new { ProductId = productId }
                    );

                    return categories.ToList();
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching product categories");
                }
            }
        }

        public async Task<int> AddProduct(ProductAddRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    // create folder for images
                    var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    // save images to folder
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
                            PriceUnitType = request.PriceUnitType.ToString(),
                            Description = request.Description ?? (object)DBNull.Value,
                            Images = imagePaths.Count > 0 ? string.Join(",", imagePaths) : (object)DBNull.Value,
                            Quantity = request.Quantity
                        }
                    );

                    // add categories
                    if (request.Categories != null && request.Categories.Count > 0)
                    {
                        foreach (var categoryId in request.Categories)
                        {
                            await connection.ExecuteAsync(
                                "INSERT INTO ProductCategoryXRef (ProductId, CategoryId) VALUES (@ProductId, @CategoryId)",
                                new { ProductId = productId, CategoryId = categoryId }
                            );
                        }
                    }

                    return productId;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while adding product");
                }
            }
        }

        public async Task<int> UpdateProduct(int productId, ProductUpdateRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // check if product exists
                    var isProductExists = await connection.ExecuteScalarAsync<bool>(
                        "SELECT 1 WHERE EXISTS(SELECT 1 FROM Product WHERE ProductId = @ProductId)",
                        new { ProductId = productId }
                    );
                    if (!isProductExists)
                    {
                        return -1;
                    }

                    // get existing images
                    var existingImages = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Images FROM Product WHERE ProductId = @ProductId",
                        new { ProductId = productId }
                     );

                    List<string> currentImages = existingImages?.Split(',').ToList() ?? new();

                    // create folder for images
                    var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    // save images to folder
                    List<string> newImagePaths = new();

                    if (request.Images != null && request.Images.Count > 0)
                    {
                        foreach (var file in request.Images)
                        {
                            // if image already exists, add it to newImagePaths and do nothing
                            if (currentImages.Contains(file.FileName))
                            {
                                newImagePaths.Add(file.FileName);
                                continue;
                            }

                            if (file.Length > 0)
                            {
                                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                                var filePath = Path.Combine(uploadFolder, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                newImagePaths.Add($"/images/products/{fileName}");
                            }
                        }
                    }

                    // delete images that are not in the request
                    var imagesToDelete = currentImages.Except(newImagePaths).ToList();
                    foreach (var imagePath in imagesToDelete)
                    {
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }

                    if (request.AddedQuantity != null)
                    {
                        await connection.ExecuteAsync(
                            "UPDATE Product SET Quantity = Quantity + @AddedQuantity WHERE ProductId = @ProductId",
                            new { AddedQuantity = request.AddedQuantity, ProductId = productId }
                        );
                    }

                    if (request.Name != null || request.Price != null || request.PriceUnitType != null || request.Description != null || newImagePaths.Count > 0 || request.Quantity != null)
                    {
                        await connection.ExecuteAsync(
                            "UPDATE Product SET " +
                            "Name = ISNULL(@Name, Name), " +
                            "Price = ISNULL(@Price, Price), " +
                            "PriceUnitType = ISNULL(@PriceUnitType, PriceUnitType), " +
                            "Description = ISNULL(@Description, Description), " +
                            "Images = @Images, " +
                            "Quantity = ISNULL(@Quantity, Quantity) " +
                            "WHERE ProductId = @ProductId",
                            new
                            {
                                Name = request.Name,
                                Price = request.Price,
                                PriceUnitType = request.PriceUnitType.ToString(),
                                Description = request.Description,
                                Images = newImagePaths.Count > 0 ? string.Join(",", newImagePaths) : (object)DBNull.Value,
                                Quantity = request.Quantity,
                                ProductId = productId
                            }
                        );
                    }

                    // update categories
                    await connection.ExecuteAsync(
                        "DELETE FROM ProductCategoryXRef WHERE ProductId = @ProductId",
                        new { ProductId = productId }
                    );

                    if (request.Categories != null && request.Categories.Count > 0)
                    {
                        foreach (var categoryId in request.Categories)
                        {
                            await connection.ExecuteAsync(
                                "INSERT INTO ProductCategoryXRef (ProductId, CategoryId) VALUES (@ProductId, @CategoryId)",
                                new { ProductId = productId, CategoryId = categoryId }
                            );
                        }
                    }

                    return productId;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while updating product");
                }
            }
        }

        public async Task<int> RemoveProduct(int productId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // check if product exists
                    var isProductExists = await connection.ExecuteScalarAsync<bool>(
                        "SELECT 1 WHERE EXISTS(SELECT 1 FROM Product WHERE ProductId = @ProductId)",
                        new { ProductId = productId }
                    );
                    if (!isProductExists)
                    {
                        return -1;
                    }

                    // delete images
                    var existingImages = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Images FROM Product WHERE ProductId = @ProductId",
                        new { ProductId = productId }
                    );

                    List<string> currentImages = existingImages?.Split(',').ToList() ?? new();

                    var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    foreach (var imagePath in currentImages)
                    {
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }

                    // delete product
                    await connection.ExecuteAsync(
                        "DELETE FROM Product WHERE ProductId = @ProductId",
                        new { ProductId = productId }
                    );

                    return productId;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while deleting product");
                }
            }
        }

        public async Task<List<Product>> GetProducts(int page, int pageSize)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var products = await connection.QueryAsync<Product>(
                        "SELECT * FROM Product ORDER BY ProductId OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                        new { Offset = (page - 1) * pageSize, PageSize = pageSize }
                    );

                    return products.ToList();
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching products");
                }
            }
        }

        public async Task<List<Product>> GetProductsByCategory(int categoryId, string sort, int page, int pageSize)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    string sortMethod = sort switch
                    {
                        "priceLow" => "Price ASC",
                        "priceHigh" => "Price DESC",
                        _ => "ProductId"
                    };

                    string sql = "SELECT * FROM Product WHERE ProductId IN (SELECT ProductId FROM ProductCategoryXRef WHERE CategoryId = @CategoryId) ";
                    if (!string.IsNullOrEmpty(sortMethod))
                    {
                        sql += $"ORDER BY {sortMethod} ";
                    }
                    sql += "OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    var products = await connection.QueryAsync<Product>(sql,
                        new { CategoryId = categoryId, Offset = (page - 1) * pageSize, PageSize = pageSize }
                    );

                    return products.ToList();
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching products");
                }
            }
        }

        public async Task<List<Product>> GetProductsBySearch(string search, string sort, int page, int pageSize)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    string sql = @"
                        SELECT DISTINCT
                            p.*,
                            CASE 
                                WHEN p.Name LIKE @Search THEN 1
                                WHEN c.Name LIKE @Search THEN 2
                                ELSE 3 -- Shouldn't happen with the WHERE clause, but for completeness
                            END AS SearchPriority
                        FROM 
                            Product p
                        INNER JOIN 
                            ProductCategoryXRef pcx ON p.ProductID = pcx.ProductID
                        INNER JOIN 
                            Category c ON pcx.CategoryID = c.CategoryID
                        WHERE 
                            (
                                p.Name LIKE @Search
            
                                OR

                                c.Name LIKE @Search
                            )
                            AND p.Quantity > 0
                        ORDER BY 
                            SearchPriority
                    ";

                    string sortMethod = sort switch
                    {
                        "priceLow" => "p.Price ASC",
                        "priceHigh" => "p.Price DESC",
                        _ => "p.ProductId"
                    };
                    if (!string.IsNullOrEmpty(sortMethod))
                    {
                        sql += $", {sortMethod}";
                    }
                    sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    var products = await connection.QueryAsync<Product>(sql,
                        new { Search = $"%{search}%", Offset = (page - 1) * pageSize, PageSize = pageSize }
                    );

                    return products.ToList();
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching products");
                }
            }
        }

        public async Task<List<Product>> GetProductsBySearchAndCategory(int categoryId, string search, string sort, int page, int pageSize)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    string sortMethod = sort switch
                    {
                        "priceLow" => "Price ASC",
                        "priceHigh" => "Price DESC",
                        _ => "ProductId"
                    };

                    string sql = "SELECT * FROM Product WHERE ProductId IN (SELECT ProductId FROM ProductCategoryXRef WHERE CategoryId = @CategoryId) AND Name LIKE @Search ";
                    if (!string.IsNullOrEmpty(sortMethod))
                    {
                        sql += $"ORDER BY {sortMethod} ";
                    }
                    sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    var products = await connection.QueryAsync<Product>(sql,
                        new { Search = $"%{search}%", CategoryID = categoryId, Offset = (page - 1) * pageSize, PageSize = pageSize }
                    );

                    return products.ToList();
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching products");
                }
            }
        }

        public async Task<List<ReviewManyResponse>> GetReviews(int productId, int page, int pageSize)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var reviews = await connection.QueryAsync<ReviewManyResponse>(
                        @"SELECT
                            r.*, u.UserId, u.UserName 
                        FROM
                            Review r
                        INNER JOIN 
                            AppUser u ON r.UserId = u.UserId
                        WHERE ProductId = @ProductId
                        ORDER BY CreatedAt DESC
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                        new { ProductId = productId, Offset = (page - 1) * pageSize, PageSize = pageSize }
                    );

                    return reviews.ToList();
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching reviews");
                }
            }
        }

        public async Task<int> AddReview(int productId, Guid userId, ReviewAddRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    // check if product exists
                    var isProductExists = await connection.ExecuteScalarAsync<bool>(
                        "SELECT 1 WHERE EXISTS(SELECT 1 FROM Product WHERE ProductId = @ProductId)",
                        new { ProductId = productId }
                    );
                    if (!isProductExists)
                    {
                        return -1;
                    }

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // Insert Review
                            var reviewId = await connection.QueryFirstOrDefaultAsync<int>(
                                "INSERT INTO Review (ProductId, UserId, Comment, Rating) " +
                                "OUTPUT INSERTED.ReviewId " +
                                "VALUES (@ProductId, @UserId, @Comment, @Rating)",
                                new
                                {
                                    ProductId = productId,
                                    UserId = userId,
                                    Comment = request.Comment,
                                    Rating = request.Rating
                                },
                                transaction: transaction
                            );

                            // Update Product
                            await connection.ExecuteAsync(
                                "UPDATE Product SET " +
                                "Rating = (SELECT AVG(Rating) FROM Review WHERE ProductId = @ProductId), " +
                                "ReviewCount = (SELECT COUNT(*) FROM Review WHERE ProductId = @ProductId) " +
                                "WHERE ProductId = @ProductId",
                                new { ProductId = productId },
                                transaction: transaction
                            );

                            // Commit the transaction
                            await transaction.CommitAsync();

                            return reviewId;
                        }
                        catch (Exception)
                        {
                            // Rollback the transaction if any error occurs
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while adding review");
                }
            }
        }

        public async Task<bool> IsUserAuthorizedForReview(int productId, int reviewId, Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var isAuthorized = await connection.ExecuteScalarAsync<bool>(
                        "SELECT 1 WHERE EXISTS(SELECT 1 FROM Review WHERE ReviewId = @ReviewId AND ProductId = @ProductId AND UserId = @UserId)",
                        new { ReviewId = reviewId, ProductId = productId, UserId = userId }
                    );

                    return isAuthorized;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while checking authorization for review");
                }
            }
        }

        public async Task<int> UpdateReview(int productId, int reviewId, ReviewAddRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // Update Review
                            await connection.ExecuteAsync(
                                "UPDATE Review SET " +
                                "Comment = @Comment, " +
                                "Rating = @Rating " +
                                "WHERE ReviewId = @ReviewId",
                                new
                                {
                                    Comment = request.Comment,
                                    Rating = request.Rating,
                                    ReviewId = reviewId
                                },
                                transaction: transaction
                            );

                            // Update Product
                            await connection.ExecuteAsync(
                                "UPDATE Product SET " +
                                "Rating = (SELECT AVG(Rating) FROM Review WHERE ProductId = @ProductId), " +
                                "ReviewCount = (SELECT COUNT(*) FROM Review WHERE ProductId = @ProductId) " +
                                "WHERE ProductId = @ProductId",
                                new { ProductId = productId },
                                transaction: transaction
                            );

                            // Commit the transaction
                            await transaction.CommitAsync();

                            return reviewId;
                        }
                        catch (Exception)
                        {
                            // Rollback the transaction if any error occurs
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while updating review");
                }
            }
        }

        public async Task<int> RemoveReview(int productId, int reviewId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // Delete Review
                            await connection.ExecuteAsync(
                                "DELETE FROM Review WHERE ReviewId = @ReviewId",
                                new { ReviewId = reviewId },
                                transaction: transaction
                            );

                            // Update Product
                            await connection.ExecuteAsync(
                                "UPDATE Product SET " +
                                "Rating = (SELECT AVG(Rating) FROM Review WHERE ProductId = @ProductId), " +
                                "ReviewCount = (SELECT COUNT(*) FROM Review WHERE ProductId = @ProductId) " +
                                "WHERE ProductId = @ProductId",
                                new { ProductId = productId },
                                transaction: transaction
                            );

                            // Commit the transaction
                            await transaction.CommitAsync();

                            return reviewId;
                        }
                        catch (Exception)
                        {
                            // Rollback the transaction if any error occurs
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while deleting review");
                }
            }
        }
    }
}
