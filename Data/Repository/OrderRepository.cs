using Dapper;

using Microsoft.Data.SqlClient;
using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionStrings:DefaultConnection"];
        }

        public async Task<List<Order>> GetOrders(Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var orders = await connection.QueryAsync<Order>(
                        "SELECT * FROM ProductOrder WHERE UserId = @UserId ORDER BY OrderDate DESC",
                        new { UserId = userId }
                    );

                    return orders.ToList();
                }
                catch (SqlException ex)
                {
                    throw new Exception("An error occurred while fetching orders", ex);
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching orders");
                }
            }
        }
    }
}
