using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThumbsUpGroceries_backend.Data;
using ThumbsUpGroceries_backend.Data.Models;
using ThumbsUpGroceries_backend.Service;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TrolleyController : ControllerBase
    {
        private readonly IDataRepository _dataRepository;

        public TrolleyController(IDataRepository dataRepository)
        {
            _dataRepository = dataRepository;
        }

        [Authorize]
        [HttpGet("count")]
        public async Task<IActionResult> GetTrolleyCount()
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var trolleyCountResponse = await _dataRepository.GetTrolleyCount(userId);
                return Ok(trolleyCountResponse);
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [Authorize]
        [HttpPost("")]
        public async Task<IActionResult> AddToTrolley([FromBody] TrolleyItemRequest request)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var trolleyItemResponse = await _dataRepository.AddTrolleyItem(userId, request.ProductId, request.PriceUnitType, request.Quantity);
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

                var trolleyItemResponse = await _dataRepository.UpdateTrolleyItem(userId, trolleyItemId, request.Quantity);
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
    }
}
