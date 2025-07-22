using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using ThumbsUpGroceries_backend.Data.Models;
using ThumbsUpGroceries_backend.Data.Repository;
using ThumbsUpGroceries_backend.Service;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;

        public OrdersController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var orders = await _orderRepository.GetOrders(userId);
                return Ok(orders);
            }
            catch (Exception e)
            {
                return StatusCode(500, "An error occurred while fetching orders");
            }
        }

        [Authorize]
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var orderContent = await _orderRepository.GetOrder(orderId, userId);

                return Ok(orderContent);
            }
            catch (InvalidDataException e)
            {
                return BadRequest(new { message = e.Message });
            }
            catch (Exception e)
            {
                return StatusCode(500, "An error occurred while fetching the order");
            }
        }

        [Authorize]
        [HttpDelete("{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var order = await _orderRepository.CancelOrder(orderId, userId);

                // request a refund through Stripe
                var refundService = new RefundService();
                var refund = refundService.Create(new RefundCreateOptions
                {
                    PaymentIntent = order.TransactionId,
                });

                return Ok(new CancelOrderResponse
                {
                    OrderId = order.OrderId,
                    OrderStatus = order.OrderStatus,
                });
            }
            catch (InvalidDataException e)
            {
                return BadRequest(new { message = e.Message });
            }
            catch (Exception e)
            {
                return StatusCode(500, "An error occurred while canceling the order");
            }
        }
    }
}
