using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data
{
    public interface IDataRepository
    {
        Task<SignupResponse> Signup(SignupRequest request);

        Task<DatabaseModel.AppUser> GetUserInfoByEmail(string email);

        Task<List<Category>> GetCategories();

        Task<int> AddProduct(ProductAddRequest reqeust);
    }
}
