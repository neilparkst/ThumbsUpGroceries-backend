using Dapper;
using Microsoft.Data.SqlClient;
using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data.Repository
{
    public class MembershipRepository : IMembershipRepository
    {
        private readonly string _connectionString;

        public MembershipRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionStrings:DefaultConnection"];
        }

        public async Task<string> GetStripePriceIdByPlanId(int planId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var priceId = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT StripePriceId FROM MembershipPlan WHERE PlanId = @PlanId",
                        new { PlanId = planId }
                    );

                    if (string.IsNullOrEmpty(priceId))
                    {
                        throw new InvalidDataException("plan not found");
                    }

                    return priceId;
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(InvalidDataException))
                    {
                        throw e;
                    }
                    throw new Exception("An error occurred while fetching the price ID");
                }
            }
        }

        public async Task<string?> GetCurrentUserMembershipName(Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var membershipPlanId = await connection.QueryFirstOrDefaultAsync<int?>(
                        "SELECT PlanId FROM UserMembership WHERE UserId = @UserId AND Status = @Status",
                        new { UserId = userId, Status = MembershipStatus.active.ToString() }
                    );
                    if (membershipPlanId == null)
                    {
                        return null;
                    }
                    var membership = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM MembershipPlan WHERE PlanId = @PlanId",
                        new { PlanId = membershipPlanId }
                    );

                    return membership;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching the current user membership");
                }
            }
        }
    }
}
