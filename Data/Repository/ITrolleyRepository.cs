using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data.Repository
{
    public interface ITrolleyRepository
    {
        Task<Trolley> GetTrolley(Guid userId);

        Task<List<TrolleyItemMany>> GetTrolleyItems(int trolleyId);

        Task<TrolleyItem> AddTrolleyItem(Guid userId, int productId, PriceUnitType priceUnitType, float quantity);

        Task<TrolleyItem> UpdateTrolleyItem(Guid userId, int trolleyItemId, float quantity);

        Task<TrolleyItem> RemoveTrolleyItem(Guid userId, int trolleyItemId);

        Task<bool> ValidateTrolley(Guid userId, TrolleyValidationRequest request);
    }
}
