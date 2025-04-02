using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ThumbsUpGroceries_backend.Data;
using ThumbsUpGroceries_backend.Data.Models;
using ThumbsUpGroceries_backend.Service;

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

        [HttpGet("{productId}")]
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

        [Authorize(Roles = "Admin")]
        [HttpPut("{productId}")]
        [HttpPatch("{productId}")]
        public async Task<ActionResult> UpdateProduct(int productId, [FromForm] ProductUpdateRequest request)
        {
            try
            {
                var _productId = await _dataRepository.UpdateProduct(productId, request);
                if (_productId == -1)
                {
                    return NotFound();
                }
                return Ok(new { productId = _productId });
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{productId}")]
        public async Task<ActionResult> RemoveProduct(int productId)
        {
            try
            {
                var _productId = await _dataRepository.RemoveProduct(productId);
                if (_productId == -1)
                {
                    return NotFound();
                }
                return Ok(new { productId = _productId });
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [HttpGet("")]
        public async Task<ActionResult> GetProducts(
            [FromQuery] int? categoryId,
            [FromQuery] string? sort,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            try
            {
                List<Product> products;
                if (categoryId != null && !String.IsNullOrEmpty(search))
                {
                    products = await _dataRepository.GetProductsBySearchAndCategory(categoryId.Value, search, sort, page, pageSize);
                }
                else if (categoryId != null)
                {
                    products = await _dataRepository.GetProductsByCategory(categoryId.Value, sort, page, pageSize);
                }
                else if (!String.IsNullOrEmpty(search))
                {
                    products = await _dataRepository.GetProductsBySearch(search, sort, page, pageSize);
                }
                else
                {
                    products = await _dataRepository.GetProducts(page, pageSize);
                }

                var productManyResponse = products.Select(p => new ProductManyResponse
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Price,
                    PriceUnitType = p.PriceUnitType,
                    Image = p.Images?.Split(",")[0] ?? "",
                    Rating = p.Rating ?? 0,
                    ReviewCount = p.ReviewCount ?? 0
                }).ToList();
                return Ok(productManyResponse);
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
                foreach (var category in categories)
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

        [Authorize]
        [HttpPost("{productId}/reviews")]
        public async Task<ActionResult> AddReview(int productId, [FromBody] ReviewAddRequest request)
        {
            try
            {
                if(request.Rating < 1 || request.Rating > 5 || request.Rating % 0.5 != 0)
                {
                    return BadRequest("Rating not valid");
                }

                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var reviewId = await _dataRepository.AddReview(productId, userId, request);
                if(reviewId == -1)
                {
                    return NotFound();
                }

                return Ok(new { reviewId });
            }
            catch (Exception e)
            {
                return StatusCode(500, e.ToString());
            }
        }

        [Authorize]
        [HttpPut("{productId}/reviews/{reviewId}")]
        public async Task<ActionResult> UpdateReview(int productId, int reviewId, [FromBody] ReviewAddRequest request)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var isAuthorized = await _dataRepository.IsUserAuthorizedForReview(productId, reviewId, userId);
                if (!isAuthorized)
                {
                    return Unauthorized();
                }

                if (request.Rating < 1 || request.Rating > 5 || request.Rating % 0.5 != 0)
                {
                    return BadRequest("Rating not valid");
                }


                var _reviewId = await _dataRepository.UpdateReview(productId, reviewId, request);
                if (_reviewId == -1)
                {
                    return NotFound();
                }

                return Ok(new { reviewId = _reviewId });
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }
}
