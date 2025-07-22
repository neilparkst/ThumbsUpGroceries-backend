using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data.Repository
{
    public interface IOrderRepository
    {
        Task<List<OrderMany>> GetOrders(Guid userId);

        Task<OrderContent> GetOrder(int orderId, Guid userId);

        Task<Order> CancelOrder(int orderId, Guid userId);
    }
}
