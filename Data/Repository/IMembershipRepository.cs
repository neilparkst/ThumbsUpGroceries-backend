using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data.Repository
{
    public interface IMembershipRepository
    {
        public Task<string> GetStripePriceIdByPlanId(int planId);

        public Task<string?> GetCurrentUserMembershipName(Guid userId);

        public Task<UserMembershipContent?> GetCurrentUserMembershipContent(Guid userId);

        public Task<List<MembershipMany>> GetMembershipOptions();
    }
}
