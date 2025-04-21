using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;
using ThumbsUpGroceries_backend.Data.Models;
using ThumbsUpGroceries_backend.Service;
using ThumbsUpGroceries_backend.Data.Repository;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class MembershipController : ControllerBase
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly IUserRepository _userRepository;

        public MembershipController(IMembershipRepository membershipRepository, IUserRepository userRepository)
        {
            _membershipRepository = membershipRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetMembershipOptions()
        {
            try
            {
                var membershipOptions = await _membershipRepository.GetMembershipOptions();
                return Ok(membershipOptions);
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [Authorize]
        [HttpPost("checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] MembershipCheckoutSessionRequest request)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));
                var customerId = await _userRepository.GetStripeCustomerIdByUserId(userId);

                var priceId = await _membershipRepository.GetStripePriceIdByPlanId(request.planId);

                var options = new SessionCreateOptions
                {
                    SuccessUrl = request.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = request.CancelUrl,
                    Mode = "subscription",
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = priceId,
                            Quantity = 1,
                        },
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId.ToString() },
                        { "planId", request.planId.ToString() }
                    },
                };
                if (!string.IsNullOrEmpty(customerId))
                {
                    options.Customer = customerId;
                }

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return Ok(new { url = session.Url });
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
