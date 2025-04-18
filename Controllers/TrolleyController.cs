using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThumbsUpGroceries_backend.Data;
using ThumbsUpGroceries_backend.Data.Models;
using ThumbsUpGroceries_backend.Data.Repository;
using ThumbsUpGroceries_backend.Service;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TrolleyController : ControllerBase
    {
        private readonly ITrolleyRepository _trolleyRepository;

        public TrolleyController(ITrolleyRepository trolleyRepository)
        {
            _trolleyRepository = trolleyRepository;
        }

        [Authorize]
        [HttpGet("count")]
        public async Task<IActionResult> GetTrolleyCount()
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var trolley = await _trolleyRepository.GetTrolley(userId);
                var trolleyCountResponse = new TrolleyCountResponse
                {
                    TrolleyId = trolley.TrolleyId,
                    ItemCount = trolley.ItemCount
                };
                return Ok(trolleyCountResponse);
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [Authorize]
        [HttpGet("")]
        public async Task<ActionResult<TrolleyContentResponse>> GetTrolleyContent()
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var trolley = await _trolleyRepository.GetTrolley(userId);
                var trolleyItems = await _trolleyRepository.GetTrolleyItems(trolley.TrolleyId);

                var subTotalPrice = trolleyItems.Sum(item => item.TotalPrice);
                // TODO: change ServiceFee, BagFee, TotalPrice based on the membership type
                var serviceFee = 0;
                var bagFee = 0;
                var totalPrice = subTotalPrice + serviceFee + bagFee;

                var trolleyContentResponse = new TrolleyContentResponse
                {
                    TrolleyId = trolley.TrolleyId,
                    ItemCount = trolley.ItemCount,
                    Items = trolleyItems,
                    SubTotalPrice = subTotalPrice,
                    Method = trolley.Method,
                    ServiceFee = serviceFee,
                    BagFee = bagFee,
                    TotalPrice = totalPrice
                };
                return Ok(trolleyContentResponse);
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }
        
        [Authorize]
        [HttpPost("")]
        public async Task<IActionResult> AddTrolleyItem([FromBody] TrolleyItemRequest request)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var trolleyItem = await _trolleyRepository.AddTrolleyItem(userId, request.ProductId, request.PriceUnitType, request.Quantity);
                var trolleyItemResponse = new TrolleyItemResponse
                {
                    TrolleyItemId = trolleyItem.TrolleyItemId,
                    ProductId = trolleyItem.ProductId,
                    PriceUnitType = trolleyItem.PriceUnitType,
                    Quantity = trolleyItem.Quantity
                };
                return Ok(trolleyItemResponse);
            }
            catch (InvalidDataException e)
            {
                return BadRequest(new { message = e.Message });
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [Authorize]
        [HttpPut("{trolleyItemId}")]
        public async Task<IActionResult> UpdateTrolleyItem(int trolleyItemId, [FromBody] TrolleyItemRequest request)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var trolleyItem = await _trolleyRepository.UpdateTrolleyItem(userId, trolleyItemId, request.Quantity);
                var trolleyItemResponse = new TrolleyItemResponse
                {
                    TrolleyItemId = trolleyItem.TrolleyItemId,
                    ProductId = trolleyItem.ProductId,
                    PriceUnitType = trolleyItem.PriceUnitType,
                    Quantity = trolleyItem.Quantity
                };
                return Ok(trolleyItemResponse);
            }
            catch (InvalidDataException e)
            {
                return BadRequest(new { message = e.Message });
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [Authorize]
        [HttpDelete("{trolleyItemId}")]
        public async Task<IActionResult> RemoveTrolleyItem(int trolleyItemId)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var trolleyItem = await _trolleyRepository.RemoveTrolleyItem(userId, trolleyItemId);

                var trolleyItemDeleteResponse = new TrolleyItemDeleteResponse
                {
                    TrolleyItemId = trolleyItem.TrolleyItemId,
                    ProductId = trolleyItem.ProductId
                };
                return Ok(trolleyItemDeleteResponse);
            }
            catch (InvalidDataException e)
            {
                return BadRequest(new { message = e.Message });
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [Authorize]
        [HttpPost("validation")]
        public async Task<IActionResult> ValidateTrolley([FromBody] TrolleyValidationRequest request)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var isTrolleyValid = await _trolleyRepository.ValidateTrolley(userId, request);
                var trolleyValidationResponse = new TrolleyValidationResponse
                {
                    IsValid = isTrolleyValid
                };
                return Ok(trolleyValidationResponse);
            }
            catch (InvalidDataException e)
            {
                return BadRequest(new { message = e.Message });
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }
    }
}
