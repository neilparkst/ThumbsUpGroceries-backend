using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using Stripe.Checkout;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly string webhookSecretKey;

        public WebhookController(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionStrings:DefaultConnection"];
            webhookSecretKey = configuration["Stripe:WebhookSecretKey"];
        }

        [HttpPost("trolley/checkout")]
        public async Task<IActionResult> TrolleyCheckoutWebhook()
        {
            try
            {
                // Configure stripe event
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    webhookSecretKey
                );

                // Handle the event
                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session == null)
                    {
                        return BadRequest("Invalid session data");
                    }

                    var options = new SessionGetOptions
                    {
                        Expand = new List<string> { "line_items", "line_items.data.price.product" },
                    };
                    var service = new SessionService();
                    session = service.Get(session.Id, options);

                    // about payment information
                    var transactionId = session.PaymentIntentId;
                    
                    // about metadata
                    var userId = session.Metadata["userId"];
                    var trolleyId = session.Metadata["trolleyId"];
                    var serviceMethod = session.Metadata["serviceMethod"];
                    var chosenDate = session.Metadata["chosenDate"];
                    var chosenAddress = session.Metadata["chosenAddress"];

                    // about products
                    var lineItems = session.LineItems;
                    var products = lineItems.Data.Select(item => new
                    {
                        item.Price.UnitAmount,
                        item.Price.Product.Name,
                        item.Price.Product.Metadata,
                        item.Quantity,
                    }).ToList();

                    // calculate service and bag fee
                    var serviceFeeInCents = session.ShippingCost.AmountTotal;

                    var bagFeeProduct = products.FirstOrDefault(product => product.Name == "Bag Fee" && product.Metadata.IsNullOrEmpty());
                    long bagFeeInCents = 150;
                    if(bagFeeProduct != null)
                    {
                        bagFeeInCents = bagFeeProduct.UnitAmount ?? 150;
                        products.Remove(bagFeeProduct);
                    }

                    // calculate the subtotal amount
                    long subTotalAmountInCents = 0;
                    foreach (var product in products)
                    {
                        if (product.Metadata["productPriceUnitType"] == "ea")
                        {
                            subTotalAmountInCents += (long)product.UnitAmount * (long)product.Quantity;
                        }
                        else if (product.Metadata["productPriceUnitType"] == "kg")
                        {
                            subTotalAmountInCents += (long)(long.Parse(product.Metadata["productPrice"]) * double.Parse(product.Metadata["quantity"]));
                        }
                    }

                    // calculate the total amount
                    long totalAmountInCents = subTotalAmountInCents + serviceFeeInCents + bagFeeInCents;

                    // manipulate the database
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        using (var transaction = await connection.BeginTransactionAsync())
                        {
                            try
                            {
                                // reduce the product quantity
                                foreach (var product in products)
                                {
                                    var productPriceUnitType = product.Metadata["productPriceUnitType"];
                                    var productId = int.Parse(product.Metadata["productId"]);
                                    var quantity = productPriceUnitType == "ea" ? (double)product.Quantity : double.Parse(product.Metadata["quantity"]);

                                    // Update the product quantity in the database
                                    await connection.ExecuteAsync(
                                        "UPDATE Product SET Quantity = Quantity - @Quantity WHERE ProductId = @ProductId",
                                        new { Quantity = quantity, ProductId = productId },
                                        transaction
                                    );
                                }

                                // delete trolley
                                await connection.ExecuteAsync(
                                    "DELETE FROM Trolley WHERE TrolleyId = @TrolleyId",
                                    new { TrolleyId = trolleyId },
                                    transaction
                                );

                                // create order
                                var orderId = await connection.ExecuteScalarAsync<int>(
                                    "INSERT INTO ProductOrder (UserId, ServiceMethod, ChosenDate, ChosenAddress, TransactionId, ServiceFee, BagFee, SubTotalAmount, TotalAmount, OrderStatus) " +
                                    "OUTPUT INSERTED.OrderId " +
                                    "VALUES (@UserId, @ServiceMethod, @ChosenDate, @ChosenAddress, @TransactionId, @ServiceFee, @BagFee, @SubTotalAmount, @TotalAmount, @OrderStatus)",
                                    new
                                    {
                                        UserId = Guid.Parse(userId),
                                        ServiceMethod = serviceMethod,
                                        ChosenDate = DateTime.Parse(chosenDate),
                                        ChosenAddress = chosenAddress,
                                        TransactionId = transactionId,
                                        ServiceFee = (double)serviceFeeInCents / 100,
                                        BagFee = (double)bagFeeInCents / 100,
                                        SubTotalAmount = (double)subTotalAmountInCents / 100,
                                        TotalAmount = (double)totalAmountInCents / 100,
                                        OrderStatus = "registered"
                                    },
                                    transaction
                                );

                                // create order items
                                foreach (var product in products)
                                {
                                    var productPriceUnitType = product.Metadata["productPriceUnitType"];
                                    var productId = int.Parse(product.Metadata["productId"]);
                                    var quantity = productPriceUnitType == "ea" ? (double)product.Quantity : double.Parse(product.Metadata["quantity"]);
                                    var productName = productPriceUnitType == "ea" ? product.Name : product.Metadata["productName"];

                                    await connection.ExecuteAsync(
                                        "INSERT INTO ProductOrderItem (OrderId, ProductId, Price, PriceUnitType, Quantity, TotalPrice, ProductName) " +
                                        "VALUES (@OrderId, @ProductId, @Price, @PriceUnitType, @Quantity, @TotalPrice, @ProductName)",
                                        new
                                        {
                                            OrderId = orderId,
                                            ProductId = productId,
                                            Price = (double)product.UnitAmount / 100,
                                            PriceUnitType = productPriceUnitType,
                                            Quantity = quantity,
                                            TotalPrice = (double)product.UnitAmount * quantity / 100,
                                            ProductName = productName
                                        },
                                        transaction
                                    );
                                }

                                await transaction.CommitAsync();
                            }
                            catch (Exception e)
                            {
                                // Rollback the transaction if any error occurs
                                await transaction.RollbackAsync();
                                throw;
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }
    }
}
