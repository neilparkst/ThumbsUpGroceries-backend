using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data.Repository
{
    public interface ITrolleyRepository
    {
        Task<Trolley> GetTrolley(Guid userId);

        Task<Trolley> GetTrolleyByTrolleyId(int trolleyId);

        Task<List<TrolleyItemMany>> GetTrolleyItems(int trolleyId);

        Task<TrolleyItem> AddTrolleyItem(Guid userId, int productId, PriceUnitType priceUnitType, int quantity);

        Task<TrolleyItem> UpdateTrolleyItem(Guid userId, int trolleyItemId, int quantity);

        Task<TrolleyItem> RemoveTrolleyItem(Guid userId, int trolleyItemId);

        Task<List<TrolleyItem>> RemoveTrolleyItems(Guid userId, List<int> trolleyItemIds);

        Task<Trolley> UpdateTrolleyMethod(Guid userId, int trolleyId, TrolleyMethod method);

        Task<List<TrolleyTimeSlot>> GetTrolleyTimeSlots(DateTime date, TrolleyMethod trolleyMethod);

        Task<bool> CreateTrolleyTimeSlots(DateTime startDate, DateTime endDate);

        Task<bool> OccupyTimeSlot(Guid userId, int timeSlotId);

        Task<bool> ValidateTrolley(Guid userId, TrolleyValidationRequest request);
    }
}
