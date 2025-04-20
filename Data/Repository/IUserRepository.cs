using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data.Repository
{
    public interface IUserRepository
    {
        Task<SignupResponse> Signup(SignupRequest request);

        Task<User> GetUserInfoByEmail(string email);

        Task<User> GetUserInfoByUserId(Guid userId);

        Task<string?> GetStripeCustomerIdByUserId(Guid userId);

        Task<User> UpdateUserInfo(Guid userId, UserInfoUpdateRequest request);

        Task<User> UpdateUserPassword(Guid userId, UserPasswordUpdateRequest request);

        Task<User> DeleteUser(Guid userId);
    }
}
