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

        public async Task<List<OrderMany>> GetOrders(Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var orders = await connection.QueryAsync<Order>(
                        "SELECT * FROM ProductOrder WHERE UserId = @UserId ORDER BY OrderDate DESC",
                        new { UserId = userId }
                    );

                    var orderManyList = new List<OrderMany>();
                    foreach (var order in orders)
                    {
                        // get time slot info
                        var timeSlot = await connection.QueryFirstOrDefaultAsync<TrolleyTimeSlot>(
                            "SELECT * FROM TrolleyTimeSlot WHERE SlotId = @TimeSlotId",
                            new { TimeSlotId = order.ChosenTimeSlot }
                        );

                        orderManyList.Add( new OrderMany()
                        {
                            OrderId = order.OrderId,
                            ServiceMethod = order.ServiceMethod,
                            TotalAmount = order.TotalAmount,
                            ChosenAddress = order.ChosenAddress,
                            ChosenStartDate = timeSlot?.StartDate ?? order.ChosenDate,
                            ChosenEndDate = timeSlot?.EndDate ?? order.ChosenDate,
                            OrderStatus = order.OrderStatus,
                            OrderDate = order.OrderDate
                        });
                    }

                    return orderManyList;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching orders");
                }
            }
        }

        public async Task<OrderContent> GetOrder(int orderId, Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    // get order info
                    var order = await connection.QueryFirstOrDefaultAsync<Order>(
                        "SELECT * FROM ProductOrder WHERE OrderId = @OrderId AND UserId = @UserId",
                        new { OrderId = orderId, UserId = userId }
                    );

                    if (order == null)
                    {
                        throw new InvalidDataException("Order not found");
                    }

                    // get time slot info
                    var timeSlot = await connection.QueryFirstOrDefaultAsync<TrolleyTimeSlot>(
                        "SELECT * FROM TrolleyTimeSlot WHERE SlotId = @TimeSlotId",
                        new { TimeSlotId = order.ChosenTimeSlot }
                    );

                    // get order items info
                    var orderItems = await connection.QueryAsync<OrderItem>(
                        "SELECT * FROM ProductOrderItem WHERE OrderId = @OrderId",
                        new { OrderId = order.OrderId }
                    );

                    // get product first image
                    foreach (var item in orderItems)
                    {
                        var product = await connection.QueryFirstOrDefaultAsync<Product>(
                            "SELECT * FROM Product WHERE ProductId = @ProductId",
                            new { ProductId = item.ProductId }
                        );

                        if (product != null)
                        {
                            item.Image = product.Images?.Split(",")[0];
                        }
                    }

                    return new OrderContent
                    {
                        OrderId = order.OrderId,
                        SubTotalAmount = order.SubTotalAmount,
                        ServiceMethod = order.ServiceMethod,
                        BagFee = order.BagFee,
                        ServiceFee = order.ServiceFee,
                        TotalAmount = order.TotalAmount,
                        ChosenAddress = order.ChosenAddress,
                        ChosenStartDate = timeSlot?.StartDate ?? order.ChosenDate,
                        ChosenEndDate = timeSlot?.EndDate ?? order.ChosenDate,
                        OrderStatus = order.OrderStatus,
                        OrderDate = order.OrderDate,
                        OrderItems = orderItems.ToList()
                    };
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(InvalidDataException))
                    {
                        throw e;
                    }
                    throw new Exception("An error occurred while fetching the order");
                }
            }
        }

        public async Task<Order> CancelOrder(int orderId, Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var order = await connection.QueryFirstOrDefaultAsync<Order>(
                        "SELECT * FROM ProductOrder WHERE OrderId = @OrderId AND UserId = @UserId",
                        new { OrderId = orderId, UserId = userId }
                    );

                    if (order == null)
                    {
                        throw new InvalidDataException("Order not found");
                    }
                    else if(order.OrderStatus != OrderStatus.registered)
                    {
                        throw new InvalidDataException("Order cannot be canceled");
                    }

                    var result = await connection.QueryFirstAsync<Order>(
                        "UPDATE ProductOrder " +
                        "SET OrderStatus = @OrderStatus " +
                        "OUTPUT INSERTED.* " +
                        "WHERE OrderId = @OrderId",
                        new { OrderStatus = OrderStatus.canceling.ToString(), OrderId = order.OrderId }
                    );

                    return result;
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(InvalidDataException))
                    {
                        throw e;
                    }
                    throw new Exception("An error occurred while canceling the order");
                }
            }
        }
    }
}
