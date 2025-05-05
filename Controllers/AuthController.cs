using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ThumbsUpGroceries_backend.Data.Models;
using ThumbsUpGroceries_backend.Data.Repository;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthController(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        [HttpPost("signup")]
        public async Task<ActionResult> Signup(SignupRequest request)
        {
            try
            {
                var result = await _userRepository.Signup(request);

                return Ok(result);
            }
            catch(InvalidDataException e)
            {
                return BadRequest(e.Message);
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [HttpPost("signin")]
        public async Task<ActionResult> Signin(SigninRequest request)
        {
            try
            {
                var user = await _userRepository.GetUserInfoByEmail(request.Email);

                if (user == null)
                {
                    return BadRequest("Invalid email or password");
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return BadRequest("Invalid email or password");
                }

                var claims = new List<Claim>{
                        new Claim("userId", user.UserId.ToString()),
                        new Claim("userName", user.UserName ?? ""),
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
    }
}
