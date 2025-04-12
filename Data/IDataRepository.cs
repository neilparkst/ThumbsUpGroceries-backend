using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Data
{
    public interface IDataRepository
    {
        Task<SignupResponse> Signup(SignupRequest request);

        Task<User> GetUserInfoByEmail(string email);

        Task<User> GetUserInfoByUserId(Guid userId);

        Task<User> UpdateUserInfo(Guid userId, UserInfoUpdateRequest request);

        Task<User> UpdateUserPassword(Guid userId, UserPasswordUpdateRequest request);

        Task<User> DeleteUser(Guid userId);

        Task<List<Category>> GetAllCategories();

        Task<Product?> GetProduct(int productId);

        Task<List<int>> GetCategoriesByProduct(int productId);

        Task<int> AddProduct(ProductAddRequest reqeust);

        Task<int> UpdateProduct(int productId, ProductUpdateRequest request);

        Task<int> RemoveProduct(int productId);

        Task<List<Product>> GetProducts(int page, int pageSize);

        Task<List<Product>> GetProductsByCategory(int categoryId, string sort, int page, int pageSize);

        Task<List<Product>> GetProductsBySearch(string search, string sort, int page, int pageSize);

        Task<List<Product>> GetProductsBySearchAndCategory(int categoryId, string search, string sort, int page, int pageSize);

        Task<List<ReviewManyResponse>> GetReviews(int productId, int page, int pageSize);

        Task<int> AddReview(int productId, Guid userId, ReviewAddRequest request);
        
        Task<bool> IsUserAuthorizedForReview(int productId, int reviewId, Guid userId);

        Task<int> UpdateReview(int productId, int reviewId, ReviewAddRequest request);

        Task<int> RemoveReview(int productId, int reviewId);

        Task<Trolley> GetTrolley(Guid userId);

        Task<List<TrolleyItemMany>> GetTrolleyItems(int trolleyId);

        Task<TrolleyItem> AddTrolleyItem(Guid userId, int productId, string priceUnitType, float quantity);

        Task<TrolleyItem> UpdateTrolleyItem(Guid userId, int trolleyItemId, float quantity);

        Task<TrolleyItem> RemoveTrolleyItem(Guid userId, int trolleyItemId);
    }
}
