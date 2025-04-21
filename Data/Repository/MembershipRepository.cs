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

        public async Task<UserMembershipContent?> GetCurrentUserMembershipContent(Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var membership = await connection.QueryFirstOrDefaultAsync<UserMembershipContent>(
                        "SELECT um.MembershipId, m.PlanId, m.Name AS PlanName, m.Price AS PlanPrice, um.StartDate, um.RenewalDate, um.Status " +
                        "FROM UserMembership um JOIN MembershipPlan m ON um.PlanId = m.PlanId " +
                        "WHERE um.UserId = @UserId AND um.Status = @Status",
                        new { UserId = userId, Status = MembershipStatus.active.ToString() }
                    );

                    return membership;
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching the current user membership content");
                }
            }
        }

        public async Task<List<MembershipMany>> GetMembershipOptions()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var memberships = await connection.QueryAsync<MembershipMany>(
                        "SELECT PlanId, Name, Price, DurationMonths, Description FROM MembershipPlan"
                    );

                    return memberships.ToList();
                }
                catch (Exception e)
                {
                    throw new Exception("An error occurred while fetching the membership options");
                }
            }
        }
    }
}
