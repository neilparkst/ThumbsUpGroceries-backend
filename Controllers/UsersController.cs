using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ThumbsUpGroceries_backend.Data.Models;
using ThumbsUpGroceries_backend.Data.Repository;
using ThumbsUpGroceries_backend.Service;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public UsersController(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult> GetMyInfo()
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var user = await _userRepository.GetUserInfoByUserId(userId);
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

        [Authorize]
        [HttpPut("me")]
        [HttpPatch("me")]
        public async Task<ActionResult> UpdateMyInfo([FromBody] UserInfoUpdateRequest request)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var user = await _userRepository.UpdateUserInfo(userId, request);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                var claims = new List<Claim>{
                        new Claim("userId", user.UserId.ToString()),
                        new Claim("email", user.Email),
                        new Claim("userName", user.UserName ?? ""),
                        new Claim("phoneNumber", user.PhoneNumber ?? ""),
                        new Claim("firstName", user.FirstName ?? ""),
                        new Claim("lastName", user.LastName ?? ""),
                        new Claim("address", user.Address ?? ""),
                        new Claim("role", user.Role)
                    };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var accessToken = new JwtSecurityToken(
                    issuer: _configuration["JwtSettings:Issuer"],
                    audience: _configuration["JwtSettings:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpiryMinutes"])),
                    signingCredentials: creds
                );

                return Ok(new SigninResponse
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(accessToken)
                });
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [Authorize]
        [HttpPut("me/password")]
        public async Task<ActionResult> UpdateMyPassword([FromBody] UserPasswordUpdateRequest request)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var user = await _userRepository.GetUserInfoByUserId(userId);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                {
                    return BadRequest("Old password is incorrect");
                }

                user = await _userRepository.UpdateUserPassword(userId, request);

                var claims = new List<Claim>{
                        new Claim("userId", user.UserId.ToString()),
                        new Claim("email", user.Email),
                        new Claim("userName", user.UserName ?? ""),
                        new Claim("phoneNumber", user.PhoneNumber ?? ""),
                        new Claim("firstName", user.FirstName ?? ""),
                        new Claim("lastName", user.LastName ?? ""),
                        new Claim("address", user.Address ?? ""),
                        new Claim("role", user.Role)
                    };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var accessToken = new JwtSecurityToken(
                    issuer: _configuration["JwtSettings:Issuer"],
                    audience: _configuration["JwtSettings:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpiryMinutes"])),
                    signingCredentials: creds
                );

                return Ok(new SigninResponse
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(accessToken)
                });
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
        [HttpDelete("me")]
        public async Task<ActionResult> DeleteMyAccount()
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var user = await _userRepository.DeleteUser(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                return Ok(new { UserId = user.UserId });
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }
    }
}
