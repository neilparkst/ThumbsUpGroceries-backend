using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data
{
    public interface IDataRepository
    {
        Task<SignupResponse> Signup(SignupRequest request);

        Task<DatabaseModel.AppUser> GetUserInfoByEmail(string email);

        Task<List<Category>> GetAllCategories();

        Task<Product?> GetProduct(int productId);

        Task<List<int>> GetCategoriesByProduct(int productId);

        Task<int> AddProduct(ProductAddRequest reqeust);
    }
}
