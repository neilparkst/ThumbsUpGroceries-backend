using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThumbsUpGroceries_backend.Data;
using ThumbsUpGroceries_backend.Data.Models;
using ThumbsUpGroceries_backend.Service;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IDataRepository _dataRepository;

        public UsersController(IDataRepository dataRepository)
        {
            _dataRepository = dataRepository;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult> GetMyInfo()
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var user = await _dataRepository.GetUserInfoByUserId(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                var userInfoResponse = new UserInfoResponse
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    UserName = user.UserName,
                    PhoneNumber = user.PhoneNumber,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Address = user.Address,
                    Role = user.Role
                };

                return Ok(userInfoResponse);
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }
    }
}
