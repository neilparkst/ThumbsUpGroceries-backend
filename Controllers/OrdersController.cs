using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var orders = await _orderRepository.GetOrders(userId);
                var orderManyResponse = orders.Select(o => new OrderMany
                {
                    OrderId = o.OrderId,
                    ServiceMethod = o.ServiceMethod,
                    TotalAmount = o.TotalAmount,
                    ChosenAddress = o.ChosenAddress,
                    ChosenDate = o.ChosenDate,
                    OrderStatus = o.OrderStatus,
                    OrderDate = o.OrderDate,
                }).ToList();
                return Ok(orderManyResponse);
            }
            catch (Exception e)
            {
                return StatusCode(500, "An error occurred while fetching orders");
            }
        }
    }
}
