namespace ThumbsUpGroceries_backend.Data.Repository
{
    public interface IMembershipRepository
    {
        public Task<string> GetStripePriceIdByPlanId(int planId);
    }
}
