using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThumbsUpGroceries_backend.Data.Models;
using ThumbsUpGroceries_backend.Data.Repository;
using ThumbsUpGroceries_backend.Service;
using Stripe.Checkout;
using Microsoft.IdentityModel.Tokens;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TrolleyController : ControllerBase
    {
        private readonly ITrolleyRepository _trolleyRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMembershipRepository _membershipRepository;

        public TrolleyController(ITrolleyRepository trolleyRepository, IUserRepository userRepository, IMembershipRepository membershipRepository)
        {
            _trolleyRepository = trolleyRepository;
            _userRepository = userRepository;
            _membershipRepository = membershipRepository;
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
                // set ServiceFee, BagFee, TotalPrice based on the membership type
                var membership = await _membershipRepository.GetCurrentUserMembershipName(userId);
                int serviceFee = (trolley.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                int bagFee = TrolleyConstants.BAG_FEE;
                if (string.IsNullOrEmpty(membership))
                {
                    serviceFee = (trolley.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                    bagFee = TrolleyConstants.BAG_FEE;
                }
                else if (membership == "Saver")
                {
                    if (subTotalPrice >= 8000)
                    {
                        serviceFee = 0;
                        bagFee = TrolleyConstants.BAG_FEE;
                    }
                    else
                    {
                        serviceFee = (trolley.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                        bagFee = TrolleyConstants.BAG_FEE;
                    }
                }
                else if (membership == "Super Saver")
                {
                    if (subTotalPrice >= 8000)
                    {
                        serviceFee = 0;
                        bagFee = 0;
                    }
                    else if (subTotalPrice >= 6000)
                    {
                        serviceFee = 0;
                        bagFee = TrolleyConstants.BAG_FEE;
                    }
                    else
                    {
                        serviceFee = (trolley.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                        bagFee = TrolleyConstants.BAG_FEE;
                    }
                }
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
        [HttpPost("bulk-deletion")]
        public async Task<IActionResult> RemoveTrolleyItems([FromBody] List<int> trolleyItemIds)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var trolleyItems = await _trolleyRepository.RemoveTrolleyItems(userId, trolleyItemIds);
                var trolleyItemDeleteResponse = trolleyItems.Select(item => new TrolleyItemDeleteResponse
                {
                    TrolleyItemId = item.TrolleyItemId,
                    ProductId = item.ProductId
                }).ToList();
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
        [HttpPost("method/{trolleyId}")]
        public async Task<IActionResult> UpdateTrolleyMethod(int trolleyId, [FromBody] TrolleyMethod trolleyMethod)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                var trolley = await _trolleyRepository.UpdateTrolleyMethod(userId, trolleyId, trolleyMethod);
                var trolleyMethodResponse = new
                {
                    TrolleyId = trolley.TrolleyId,
                    Method = trolley.Method
                };
                return Ok(trolleyMethodResponse);
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

        //[Authorize]
        [HttpGet("time-slot/{serviceMethod}")]
        public async Task<IActionResult> GetTimeSlot(TrolleyMethod serviceMethod)
        {
            try
            {
                //var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                //var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));

                // TODO: create database table for time slots
                List<TrolleyTimeSlot> timeSlots = new List<TrolleyTimeSlot>();
                if (serviceMethod == TrolleyMethod.pickup) {
                    DateTime now = DateTime.Now;
                    DateTime today = now.Date;

                    for (int i = 0; i < 7; i++)
                    {
                        DateTime currentDate = today.AddDays(i);
                        for (int hour = 7; hour <= 20; hour++)
                        {
                            for(int minute = 0; minute < 60; minute += 30)
                            {
                                DateTime currentStartTime = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, hour, minute, 0);
                                if (currentStartTime > now)
                                {
                                    timeSlots.Add(new TrolleyTimeSlot
                                    {
                                        TimeSlotId = 10000 + i * 14 + hour + minute,
                                        Start = currentStartTime,
                                        End = currentStartTime.AddMinutes(30),
                                        Status = TrolleyTimeSlotStatus.available
                                    });
                                }
                            }
                        }
                    }
                }
                else if (serviceMethod == TrolleyMethod.delivery)
                {
                    DateTime now = DateTime.Now;
                    DateTime today = now.Date;

                    for (int i = 0; i < 7; i++)
                    {
                        DateTime currentDate = today.AddDays(i);
                        for (int hour = 8; hour <= 17; hour++)
                        {
                            DateTime currentStartTime = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, hour, 30, 0);
                            if (currentStartTime > now)
                            {
                                timeSlots.Add(new TrolleyTimeSlot
                                {
                                    TimeSlotId = 20000 + i * 14 + hour,
                                    Start = currentStartTime,
                                    End = currentStartTime.AddMinutes(150),
                                    Status = TrolleyTimeSlotStatus.available
                                });
                            }
                        }
                    }
                }

                return Ok(timeSlots);
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
        public async Task<IActionResult> CreateCheckoutSession([FromBody] TrolleyCheckoutSessionRequest request)
        {
            try
            {
                var jwtToken = Request.Headers["Authorization"].ToString().Split(" ")[1];
                var userId = Guid.Parse(JwtService.GetClaimFromToken(jwtToken, "userId"));
                var customerId = await _userRepository.GetStripeCustomerIdByUserId(userId);

                var trolley = await _trolleyRepository.GetTrolleyByTrolleyId(request.TrolleyId);
                var trolleyItems = await _trolleyRepository.GetTrolleyItems(request.TrolleyId);

                // set ServiceFee, BagFee based on the membership type
                var membership = await _membershipRepository.GetCurrentUserMembershipName(userId);
                int serviceFee = (trolley.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                int bagFee = TrolleyConstants.BAG_FEE;
                var subTotalPrice = trolleyItems.Sum(item => item.TotalPrice);
                if (string.IsNullOrEmpty(membership))
                {
                    serviceFee = (trolley.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                    bagFee = TrolleyConstants.BAG_FEE;
                }
                else if (membership == "Saver")
                {
                    if (subTotalPrice >= 8000)
                    {
                        serviceFee = 0;
                        bagFee = TrolleyConstants.BAG_FEE;
                    }
                    else
                    {
                        serviceFee = (trolley.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                        bagFee = TrolleyConstants.BAG_FEE;
                    }
                }
                else if (membership == "Super Saver")
                {
                    if (subTotalPrice >= 8000)
                    {
                        serviceFee = 0;
                        bagFee = 0;
                    }
                    else if (subTotalPrice >= 6000)
                    {
                        serviceFee = 0;
                        bagFee = TrolleyConstants.BAG_FEE;
                    }
                    else
                    {
                        serviceFee = (trolley.Method == TrolleyMethod.delivery) ? TrolleyConstants.DELIVERY_FEE : 0;
                        bagFee = TrolleyConstants.BAG_FEE;
                    }
                }

                // config stripe session options
                var options = new SessionCreateOptions
                {
                    LineItems = new List<SessionLineItemOptions> // products
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)bagFee,
                                Currency = "nzd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Bag Fee",
                                }
                            },
                            Quantity = 1,
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
                                    Amount = (long)serviceFee,
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
                        { "serviceMethod", trolley.Method.ToString() },
                        { "chosenDate", request.ChosenDate.ToString("yyyy-MM-ddTHH:mm:ss") },
                        { "chosenAddress", request.ChosenAddress }
                    },
                };
                if (!string.IsNullOrEmpty(customerId))
                {
                    options.Customer = customerId;
                }

                // add products to the session
                foreach (var trolleyItem in trolleyItems)
                {
                    if(trolleyItem.ProductPriceUnitType == PriceUnitType.ea)
                    {
                        var lineItem = new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)trolleyItem.ProductPrice,
                                Currency = "nzd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = trolleyItem.ProductName,
                                    Metadata = new Dictionary<string, string>
                                    {
                                        { "productId", trolleyItem.ProductId.ToString() },
                                        { "productPriceUnitType", trolleyItem.ProductPriceUnitType.ToString() }
                                    }
                                }
                            },
                            Quantity = (long)trolleyItem.Quantity
                        };
                        options.LineItems.Add(lineItem);
                    }
                    else if (trolleyItem.ProductPriceUnitType == PriceUnitType.g)
                    {
                        var lineItem = new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)trolleyItem.TotalPrice,
                                Currency = "nzd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = trolleyItem.ProductName + $" {(trolleyItem.Quantity / (double)1000).ToString("N2")}kg",
                                    Metadata = new Dictionary<string, string>
                                    {
                                        { "productId", trolleyItem.ProductId.ToString() },
                                        { "productPriceUnitType", trolleyItem.ProductPriceUnitType.ToString() },
                                        { "productName", trolleyItem.ProductName },
                                        { "productPrice", trolleyItem.ProductPrice.ToString() },
                                        { "quantity", trolleyItem.Quantity.ToString() }
                                    }
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
