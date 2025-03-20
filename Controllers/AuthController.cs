using Microsoft.AspNetCore.Mvc;
using ThumbsUpGroceries_backend.Data;
using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IDataRepository _dataRepository;

        public AuthController(IDataRepository dataRepository)
        {
            _dataRepository = dataRepository;
        }

        [HttpPost("signup")]
        public async Task<ActionResult> Signup(SignupRequest request)
        {
            try
            {
                var result = await _dataRepository.Signup(request);

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
    }
}
