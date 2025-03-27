using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ThumbsUpGroceries_backend.Data;
using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IDataRepository _dataRepository;
        private readonly IMemoryCache _cache;

        public ProductsController(IDataRepository dataRepository, IMemoryCache cache)
        {
            _dataRepository = dataRepository;
            _cache = cache;
        }

        [HttpGet(":productId")]
        public async Task<ActionResult> GetProduct(int productId)
        {
            try
            {
                var product = await _dataRepository.GetProduct(productId);
                if (product == null)
                {
                    return NotFound();
                }

                var productCategories = await _dataRepository.GetCategoriesByProduct(productId);

                ProductResponse productResponse = new ProductResponse
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Price = product.Price,
                    PriceUnitType = product.PriceUnitType,
                    Description = product.Description,
                    Images = product.Images?.Split(",").ToList(),
                    Quantity = product.Quantity,
                    Categories = productCategories,
                    Rating = product.Rating,
                    ReviewCount = product.ReviewCount
                };

                return Ok(productResponse);
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("")]
        public async Task<ActionResult> AddProduct([FromForm] ProductAddRequest reqeust)
        {
            try
            {
                var productId = await _dataRepository.AddProduct(reqeust);
                return Ok(new { productId });
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        private const string CacheKeyCategoryTree = "CategoryTree";
        private class CategoryDto
        {
            public int CategoryId { get; set; }
            public string Name { get; set; }
            public List<CategoryDto> Children { get; set; }
        }

        [HttpGet("categories")]
        public async Task<ActionResult> GetCategoryTree()
        {
            try
            {
                if (_cache.TryGetValue(CacheKeyCategoryTree, out List<CategoryDto> cachedTree))
                {
                    return Ok(cachedTree);
                }
                var categories = await _dataRepository.GetAllCategories();
                var idToCategoryDto = categories.ToDictionary(c => c.CategoryId, c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Children = new List<CategoryDto>()
                });

                var categoryTree = new List<CategoryDto>();
                foreach(var category in categories)
                {
                    if (category.ParentCategoryId == null)
                    {
                        categoryTree.Add(idToCategoryDto[category.CategoryId]);
                    }
                    else
                    {
                        idToCategoryDto[category.ParentCategoryId.Value].Children.Add(idToCategoryDto[category.CategoryId]);
                    }
                }

                _cache.Set(CacheKeyCategoryTree, categoryTree, TimeSpan.FromMinutes(5));
                return Ok(categoryTree);
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }
    }
}
