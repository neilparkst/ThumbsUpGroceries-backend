using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThumbsUpGroceries_backend.Data.Models;
using ThumbsUpGroceries_backend.Data.Repository;
using ThumbsUpGroceries_backend.Service;
using Stripe.Checkout;

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

        [Authorize]
        [HttpPost("checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutSessionRequest request)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var trolley = await _trolleyRepository.GetTrolleyByTrolleyId(request.TrolleyId);
                var trolleyItems = await _trolleyRepository.GetTrolleyItems(request.TrolleyId);

                // TODO: change ServiceFee, BagFee based on the membership type
                double serviceFee = 8.7;
                double bagFee = 1.5;

                // config stripe session options
                var options = new SessionCreateOptions
                {
                    LineItems = new List<SessionLineItemOptions> // products
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(bagFee * 100),
                                Currency = "nzd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Bag Fee"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    ShippingOptions = new List<SessionShippingOptionOptions> // delivery or pickup fee
                    {
                        new SessionShippingOptionOptions
                        {
                            ShippingRateData = new SessionShippingOptionShippingRateDataOptions
                            {
                                Type = "fixed_amount",
                                FixedAmount = new SessionShippingOptionShippingRateDataFixedAmountOptions
                                {
                                    Amount = (long)(serviceFee * 100),
                                    Currency = "nzd"
                                },
                                DisplayName = trolley.Method.ToString() == "delivery" ? "Delivery Fee" : "Pickup Fee"
                            }
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId.ToString() },
                        { "trolleyId", request.TrolleyId.ToString() },
                        { "chosenDate", request.ChosenDate.ToString("yyyy-MM-ddTHH:mm:ss") },
                        { "chosenAddress", request.ChosenAddress }
                    },
                };

                // add products to the session
                foreach (var trolleyItem in trolleyItems)
                {
                    if(trolleyItem.ProductPriceUnitType == PriceUnitType.ea)
                    {
                        var lineItem = new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(trolleyItem.ProductPrice * 100),
                                Currency = "nzd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = trolleyItem.ProductName
                                }
                            },
                            Quantity = (long)trolleyItem.Quantity
                        };
                        options.LineItems.Add(lineItem);
                    }
                    else if (trolleyItem.ProductPriceUnitType == PriceUnitType.kg)
                    {
                        var lineItem = new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(trolleyItem.TotalPrice * 100),
                                Currency = "nzd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = trolleyItem.ProductName + $" {trolleyItem.Quantity}kg"
                                }
                            },
                            Quantity = 1
                        };
                        options.LineItems.Add(lineItem);
                    }
                }

                var service = new SessionService();
                Session session = service.Create(options);

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
