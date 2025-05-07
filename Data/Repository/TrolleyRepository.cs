using Dapper;

using Microsoft.Data.SqlClient;
using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data.Repository
{
    public class TrolleyRepository : ITrolleyRepository
    {
        private readonly string _connectionString;
        private readonly IMembershipRepository _membershipRepository;

        public TrolleyRepository(IConfiguration configuration, IMembershipRepository membershipRepository)
        {
            _connectionString = configuration["ConnectionStrings:DefaultConnection"];
            _membershipRepository = membershipRepository;
        }

        public async Task<Trolley> GetTrolley(Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var trolley = await connection.QueryFirstOrDefaultAsync<Trolley>(
                        "SELECT * FROM Trolley WHERE UserId = @UserId",
                        new { UserId = userId }
                    );

                    if (trolley == null)
                    {
                        trolley = await connection.QueryFirstAsync<Trolley>(
                            "INSERT INTO Trolley (UserId, ItemCount) " +
                            "OUTPUT INSERTED.* " +
                            "VALUES (@UserId, 0)",
                            new { UserId = userId }
                        );
                    }

                    return trolley;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching trolley count");
                }
            }
        }

        public async Task<Trolley> GetTrolleyByTrolleyId(int trolleyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var trolley = await connection.QueryFirstOrDefaultAsync<Trolley>(
                        "SELECT * FROM Trolley WHERE TrolleyId = @TrolleyId",
                        new { TrolleyId = trolleyId }
                    );

                    if (trolley == null)
                    {
                        throw new InvalidDataException("Trolley does not exist");
                    }

                    return trolley;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching trolley count");
                }
            }
        }

        public async Task<List<TrolleyItemMany>> GetTrolleyItems(int trolleyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var trolleyItems = await connection.QueryAsync<TrolleyItemMany>(
                        @"SELECT ti.*, p.Name AS ProductName, p.Price AS ProductPrice, p.PriceUnitType AS ProductPriceUnitType, p.Images AS Image
                        FROM TrolleyItem ti
                        INNER JOIN Product p ON ti.ProductId = p.ProductId
                        WHERE TrolleyId = @TrolleyId",
                        new { TrolleyId = trolleyId }
                    );

                    trolleyItems = trolleyItems.Select(trolleyItem =>
                    {
                        trolleyItem.Image = trolleyItem.Image.Split(',')[0] ?? string.Empty;
                        trolleyItem.TotalPrice = trolleyItem.ProductPrice * trolleyItem.Quantity;
                        return trolleyItem;
                    });

                    return trolleyItems.ToList();
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching trolley items");
                }
            }
        }

        public async Task<TrolleyItem> AddTrolleyItem(Guid userId, int productId, PriceUnitType priceUnitType, float quantity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    if (quantity <= 0)
                    {
                        throw new InvalidDataException("Quantity must be greater than 0");
                    }

                    await connection.OpenAsync();

                    // check if product exists
                    var isProductExists = await connection.ExecuteScalarAsync<bool>(
                        "SELECT 1 WHERE EXISTS(SELECT 1 FROM Product WHERE ProductId = @ProductId)",
                        new { ProductId = productId }
                    );
                    if (!isProductExists)
                    {
                        throw new InvalidDataException("Product does not exist");
                    }

                    var trolleyId = await connection.QueryFirstOrDefaultAsync<int?>(
                        "SELECT TrolleyId FROM Trolley WHERE UserId = @UserId",
                        new { UserId = userId }
                    );

                    if (trolleyId == null)
                    {
                        trolleyId = await connection.QueryFirstAsync<int>(
                            "INSERT INTO Trolley (UserId, ItemCount) " +
                            "OUTPUT INSERTED.TrolleyId " +
                            "VALUES (@UserId, 0)",
                            new { UserId = userId }
                        );
                    }

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // Insert or Update Trolley Item
                            var trolleyItemResponse = await connection.QueryFirstAsync<TrolleyItem>(
                                @"
                                    MERGE INTO TrolleyItem AS target
                                    USING (VALUES (@TrolleyId, @ProductId, @PriceUnitType, @Quantity)) AS source (TrolleyId, ProductId, PriceUnitType, Quantity)
                                        ON target.TrolleyId = source.TrolleyId
                                           AND target.ProductId = source.ProductId
                                           AND target.PriceUnitType = source.PriceUnitType
                                    WHEN MATCHED THEN
                                        UPDATE SET Quantity = target.Quantity + source.Quantity
                                    WHEN NOT MATCHED THEN
                                        INSERT (TrolleyId, ProductId, PriceUnitType, Quantity)
                                        VALUES (source.TrolleyId, source.ProductId, source.PriceUnitType, source.Quantity)
                                    OUTPUT INSERTED.*;
                                ",
                                new
                                {
                                    TrolleyId = trolleyId,
                                    ProductId = productId,
                                    PriceUnitType = priceUnitType.ToString(),
                                    Quantity = quantity
                                },
                                transaction: transaction
                            );

                            // Update Trolley Item Count
                            await connection.ExecuteAsync(
                                "UPDATE Trolley " +
                                "SET ItemCount = (SELECT COUNT(*) FROM TrolleyItem WHERE TrolleyId = @TrolleyId) " +
                                "WHERE TrolleyId = @TrolleyId",
                                new { TrolleyId = trolleyId },
                                transaction: transaction
                            );

                            await transaction.CommitAsync();

                            return trolleyItemResponse;
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
                    if (e.GetType() == typeof(InvalidDataException))
                    {
                        throw e;
                    }
                    throw new Exception("An error occurred while adding to trolley");
                }
            }
        }

        public async Task<TrolleyItem> UpdateTrolleyItem(Guid userId, int trolleyItemId, float quantity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    if (quantity <= 0)
                    {
                        throw new InvalidDataException("Quantity must be greater than 0");
                    }

                    await connection.OpenAsync();

                    // check if the user is authorised
                    var isUserAuthorized = await connection.ExecuteScalarAsync<bool>(
                        "SELECT 1 WHERE EXISTS(SELECT 1 FROM TrolleyItem WHERE TrolleyItemId = @TrolleyItemId AND TrolleyId IN (SELECT TrolleyId FROM Trolley WHERE UserId = @UserId))",
                        new { TrolleyItemId = trolleyItemId, UserId = userId }
                    );
                    if (!isUserAuthorized)
                    {
                        throw new InvalidDataException("User is not authorized to update this trolley item");
                    }

                    // check if trolley item exists
                    var isTrolleyItemExists = await connection.ExecuteScalarAsync<bool>(
                        "SELECT 1 WHERE EXISTS(SELECT 1 FROM TrolleyItem WHERE TrolleyItemId = @TrolleyItemId)",
                        new { TrolleyItemId = trolleyItemId }
                    );
                    if (!isTrolleyItemExists)
                    {
                        throw new InvalidDataException("Trolley item does not exist");
                    }

                    // Update Trolley Item
                    var trolleyItemResponse = await connection.QueryFirstAsync<TrolleyItem>(
                        "UPDATE TrolleyItem " +
                        "SET Quantity = @Quantity " +
                        "OUTPUT INSERTED.* " +
                        "WHERE TrolleyItemId = @TrolleyItemId",
                        new { TrolleyItemId = trolleyItemId, Quantity = quantity }
                    );

                    return trolleyItemResponse;
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(InvalidDataException))
                    {
                        throw e;
                    }
                    throw new Exception("An error occurred while updating trolley item");
                }
            }
        }

        public async Task<TrolleyItem> RemoveTrolleyItem(Guid userId, int trolleyItemId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // check if the user is authorised
                    var isUserAuthorized = await connection.ExecuteScalarAsync<bool>(
                        "SELECT 1 WHERE EXISTS(SELECT 1 FROM TrolleyItem WHERE TrolleyItemId = @TrolleyItemId AND TrolleyId IN (SELECT TrolleyId FROM Trolley WHERE UserId = @UserId))",
                        new { TrolleyItemId = trolleyItemId, UserId = userId }
                    );
                    if (!isUserAuthorized)
                    {
                        throw new InvalidDataException("User is not authorized to delete this trolley item");
                    }

                    // check if trolley item exists
                    var isTrolleyItemExists = await connection.ExecuteScalarAsync<bool>(
                        "SELECT 1 WHERE EXISTS(SELECT 1 FROM TrolleyItem WHERE TrolleyItemId = @TrolleyItemId)",
                        new { TrolleyItemId = trolleyItemId }
                    );
                    if (!isTrolleyItemExists)
                    {
                        throw new InvalidDataException("Trolley item does not exist");
                    }

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // Delete Trolley Item
                            var trolleyItemResponse = await connection.QueryFirstAsync<TrolleyItem>(
                                "DELETE FROM TrolleyItem " +
                                "OUTPUT DELETED.* " +
                                "WHERE TrolleyItemId = @TrolleyItemId",
                                new { TrolleyItemId = trolleyItemId },
                                transaction: transaction
                            );

                            // Update Trolley Item Count
                            await connection.ExecuteAsync(
                                "UPDATE Trolley " +
                                "SET ItemCount = (SELECT COUNT(*) FROM TrolleyItem WHERE TrolleyId = @TrolleyId) " +
                                "WHERE TrolleyId = @TrolleyId",
                                new { TrolleyId = trolleyItemResponse.TrolleyId },
                                transaction: transaction
                            );

                            await transaction.CommitAsync();

                            return trolleyItemResponse;
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
                    if (e.GetType() == typeof(InvalidDataException))
                    {
                        throw e;
                    }
                    throw new Exception("An error occurred while deleting trolley item");
                }
            }
        }

        public async Task<List<TrolleyItem>> RemoveTrolleyItems(Guid userId, List<int> trolleyItemIds)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // check if the user is authorised for all items
                    var count = await connection.ExecuteScalarAsync<int>(@"
                            SELECT COUNT(*) FROM TrolleyItem
                            WHERE
                                TrolleyItemId IN @TrolleyItemIds
                                AND
                                TrolleyId IN (SELECT TrolleyId FROM Trolley WHERE UserId = @UserId)
                        ",
                        new { UserId = userId, TrolleyItemIds = trolleyItemIds }
                    );
                    var isUserAuthorized = (count == trolleyItemIds.Count);
                    if (!isUserAuthorized)
                    {
                        throw new InvalidDataException("User is not authorized to delete these trolley items");
                    }

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // Delete Trolley Items
                            var trolleyItemsResponse = await connection.QueryAsync<TrolleyItem>(
                                "DELETE FROM TrolleyItem " +
                                "OUTPUT DELETED.* " +
                                "WHERE TrolleyItemId IN @TrolleyItemIds",
                                new { TrolleyItemIds = trolleyItemIds },
                                transaction: transaction
                            );

                            // Update Trolley Item Count
                            await connection.ExecuteAsync(
                                "UPDATE Trolley " +
                                "SET ItemCount = (SELECT COUNT(*) FROM TrolleyItem WHERE TrolleyId = @TrolleyId) " +
                                "WHERE TrolleyId = @TrolleyId",
                                new { TrolleyId = trolleyItemsResponse.FirstOrDefault().TrolleyId },
                                transaction: transaction
                            );

                            await transaction.CommitAsync();

                            return trolleyItemsResponse.ToList();
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
                    if (e.GetType() == typeof(InvalidDataException))
                    {
                        throw e;
                    }
                    throw new Exception("An error occurred while deleting trolley item");
                }
            }
        }

        public async Task<bool> ValidateTrolley(Guid userId, TrolleyValidationRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // check if the user is authorised
                    var isUserAuthorized = await connection.ExecuteScalarAsync<bool>(
                        "SELECT 1 WHERE EXISTS(SELECT 1 FROM Trolley WHERE TrolleyId = @TrolleyId AND UserId = @UserId)",
                        new { UserId = userId, TrolleyId = request.TrolleyId }
                    );
                    if (!isUserAuthorized)
                    {
                        throw new InvalidDataException("User is not authorized to validate this trolley");
                    }

                    // Validate Trolley
                    var trolleyItems = request.Items;
                    var productIds = trolleyItems.Select(t => t.ProductId).ToList();
                    var products = await connection.QueryAsync<Product>(
                        "SELECT * FROM Product WHERE ProductId IN @ProductIds",
                        new { ProductIds = productIds }
                    );
                    if(products.Count() != trolleyItems.Count)
                    {
                        throw new InvalidDataException("Some products do not exist in the trolley");
                    }
                    foreach (var trolleyItem in trolleyItems)
                    {
                        // check if quantity is valid
                        if (trolleyItem.Quantity <= 0)
                        {
                            throw new InvalidDataException($"Quantity for product with ID {trolleyItem.ProductId} must be greater than 0");
                        }

                        // compare with current product information
                        var product = products.FirstOrDefault(p => p.ProductId == trolleyItem.ProductId);
                        if (product == null)
                        {
                            throw new InvalidDataException($"Product with ID {trolleyItem.ProductId} does not exist");
                        }
                        if (product.PriceUnitType != trolleyItem.PriceUnitType)
                        {
                            throw new InvalidDataException($"Product with ID {trolleyItem.ProductId} has a different price unit type");
                        }
                        if((product.Price != trolleyItem.ProductPrice) || (trolleyItem.ProductPrice * trolleyItem.Quantity != trolleyItem.TotalPrice))
                        {
                            return false;
                        }
                    }
                    if(request.SubTotalPrice != trolleyItems.Sum(t => t.TotalPrice))
                    {
                        return false;
                    }

                    // get membership data to check serviceFee and bagFee
                    var membership = await _membershipRepository.GetCurrentUserMembershipName(userId);
                    float serviceFee = (request.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                    float bagFee = TrolleyConstants.BAG_FEE;
                    if (string.IsNullOrEmpty(membership))
                    {
                        serviceFee = (request.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                        bagFee = TrolleyConstants.BAG_FEE;
                    }
                    else if(membership == "Saver")
                    {
                        if(request.SubTotalPrice >= 80)
                        {
                            serviceFee = 0;
                            bagFee = TrolleyConstants.BAG_FEE;
                        }
                        else
                        {
                            serviceFee = (request.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                            bagFee = TrolleyConstants.BAG_FEE;
                        }
                    }
                    else if(membership == "Super Saver")
                    {
                        if(request.SubTotalPrice >= 80)
                        {
                            serviceFee = 0;
                            bagFee = 0;
                        }
                        else if(request.SubTotalPrice >= 60)
                        {
                            serviceFee = 0;
                            bagFee = TrolleyConstants.BAG_FEE;
                        }
                        else
                        {
                            serviceFee = (request.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                            bagFee = TrolleyConstants.BAG_FEE;
                        }
                    }
                    if(request.ServiceFee != serviceFee || request.BagFee != bagFee)
                    {
                        return false;
                    }

                    if (request.TotalPrice != request.SubTotalPrice + request.ServiceFee + request.BagFee)
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(InvalidDataException))
                    {
                        throw e;
                    }
                    throw new Exception("An error occurred while validating trolley");
                }
            }
        }
    }
}
