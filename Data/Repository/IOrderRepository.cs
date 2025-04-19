using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data.Repository
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetOrders(Guid userId);

        Task<OrderContent> GetOrder(int orderId, Guid userId);
    }
}
