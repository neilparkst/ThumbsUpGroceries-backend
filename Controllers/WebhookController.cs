using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using Stripe.Checkout;
using ThumbsUpGroceries_backend.Data.Models;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly string checkoutWebhookSecretKey;
        private readonly string ordersWebhookSecretKey;

        public WebhookController(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionStrings:DefaultConnection"];
            checkoutWebhookSecretKey = configuration["Stripe:CheckoutWebhookSecretKey"];
            ordersWebhookSecretKey = configuration["Stripe:OrdersWebhookSecretKey"];
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CheckoutWebhook()
        {
            try
            {
                // Configure stripe event
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    checkoutWebhookSecretKey
                );

                // Handle the event
                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session == null)
                    {
                        return BadRequest("Invalid session data");
                    }
                    switch (session.Mode)
                    {
                        case "payment":
                            {
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
                                if (bagFeeProduct != null)
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
                            break;
                        case "setup":
                            break;
                        case "subscription":
                            {
                                var options = new SessionGetOptions
                                {
                                    Expand = new List<string> { "customer", "subscription" },
                                };
                                var service = new SessionService();
                                session = service.Get(session.Id, options);

                                // about customer
                                var customerId = session.CustomerId;

                                // about subscription
                                var subscriptionId = session.SubscriptionId;
                                var subscription = new SubscriptionService().Get(subscriptionId);

                                // about metadata
                                var userId = session.Metadata["userId"];
                                var planId = session.Metadata["planId"];

                                // manipulate the database
                                using (var connection = new SqlConnection(_connectionString))
                                {
                                    await connection.OpenAsync();
                                    using (var transaction = await connection.BeginTransactionAsync())
                                    {
                                        try
                                        {
                                            // register customer id
                                            await connection.ExecuteAsync(
                                                "UPDATE AppUser SET StripeCustomerId = @StripeCustomerId WHERE UserId = @UserId",
                                                new { StripeCustomerId = customerId, UserId = userId },
                                                transaction
                                            );

                                            // create membership
                                            await connection.ExecuteAsync(
                                                "INSERT INTO UserMembership (UserId, PlanId, StartDate, RenewalDate, Status, StripeSubscriptionId) " +
                                                "VALUES (@UserId, @PlanId, @StartDate, @RenewalDate, @Status, @StripeSubscriptionId)",
                                                new
                                                {
                                                    UserId = Guid.Parse(userId),
                                                    PlanId = int.Parse(planId),
                                                    StartDate = subscription.Items.Data[0].CurrentPeriodStart,
                                                    RenewalDate = subscription.Items.Data[0].CurrentPeriodEnd,
                                                    Status = "active",
                                                    StripeSubscriptionId = subscriptionId
                                                },
                                                transaction
                                            );

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
                            break;
                        default:
                            return BadRequest("Invalid session mode");
                    }
                }
                else if (stripeEvent.Type == EventTypes.InvoicePaid)
                {
                    var invoice = stripeEvent.Data.Object as Invoice;
                    if (invoice == null)
                    {
                        return BadRequest("Invalid invoice data");
                    }

                    if (invoice.BillingReason == "subscription_create")
                    {
                        return BadRequest("Not recurring payment");
                    }

                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        var customerId = invoice.CustomerId;
                        // get userId
                        var userId = await connection.QueryFirstOrDefaultAsync<Guid>(
                            "SELECT UserId FROM AppUser WHERE StripeCustomerId = @StripeCustomerId",
                            new { StripeCustomerId = customerId }
                        );
                        // get subscriptionId by userId
                        var subscriptionId = await connection.QueryFirstOrDefaultAsync<string>(
                            "SELECT StripeSubscriptionId FROM UserMembership WHERE UserId = @UserId",
                            new { UserId = userId }
                        );

                        // get subscription info
                        var subscription = new SubscriptionService().Get(subscriptionId);

                        // update membership
                        await connection.ExecuteAsync(
                            "UPDATE UserMembership " +
                            "SET RenewalDate = @RenewalDate, Status = @Status " +
                            "WHERE StripeSubscriptionId = @StripeSubscriptionId",
                            new
                            {
                                RenewalDate = subscription.Items.Data[0].CurrentPeriodEnd,
                                Status = "active",
                                StripeSubscriptionId = subscriptionId
                            }
                        );
                    }
                }
                else if (stripeEvent.Type == EventTypes.CustomerSubscriptionUpdated)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    if (subscription == null)
                    {
                        return BadRequest("Invalid subscription data");
                    }

                    var subscriptionId = subscription.Id;
                    var priceId = subscription.Items.Data[0].Price.Id;
                    
                    using( var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        // get planId from priceId
                        var planId = await connection.QueryFirstOrDefaultAsync<int>(
                            "SELECT PlanId FROM MembershipPlan WHERE StripePriceId = @StripePriceId",
                            new { StripePriceId = priceId }
                        );

                        // update membership
                        await connection.ExecuteAsync(
                            "UPDATE UserMembership " +
                            "SET PlanId = @PlanId, StartDate = @StartDate, RenewalDate = @RenewalDate, Status = @Status " +
                            "WHERE StripeSubscriptionId = @StripeSubscriptionId",
                            new
                            {
                                PlanId = planId,
                                StartDate = subscription.Items.Data[0].CurrentPeriodStart,
                                RenewalDate = subscription.Items.Data[0].CurrentPeriodEnd,
                                Status = "active",
                                StripeSubscriptionId = subscriptionId
                            }
                        );
                    }
                }
                else if (stripeEvent.Type == EventTypes.CustomerSubscriptionDeleted)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    if (subscription == null)
                    {
                        return BadRequest("Invalid subscription data");
                    }

                    var subscriptionId = subscription.Id;

                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        // update membership
                        await connection.ExecuteAsync(
                            "UPDATE UserMembership SET Status = @Status WHERE StripeSubscriptionId = @StripeSubscriptionId",
                            new { Status = "canceled", StripeSubscriptionId = subscriptionId }
                        );
                    }
                }
                else if (stripeEvent.Type == EventTypes.InvoicePaymentFailed)
                {
                    var invoice = stripeEvent.Data.Object as Invoice;
                    if (invoice == null)
                    {
                        return BadRequest("Invalid invoice data");
                    }

                    if (!(invoice.BillingReason == "subscription_cycle"))
                    {
                        return BadRequest("Not recurring payment");
                    }

                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        var customerId = invoice.CustomerId;
                        // get userId
                        var userId = await connection.QueryFirstOrDefaultAsync<Guid>(
                            "SELECT UserId FROM AppUser WHERE StripeCustomerId = @StripeCustomerId",
                            new { StripeCustomerId = customerId }
                        );
                        // get subscriptionId by userId
                        var subscriptionId = await connection.QueryFirstOrDefaultAsync<string>(
                            "SELECT StripeSubscriptionId FROM UserMembership WHERE UserId = @UserId",
                            new { UserId = userId }
                        );

                        // update membership
                        await connection.ExecuteAsync(
                            "UPDATE UserMembership SET Status = @Status WHERE StripeSubscriptionId = @StripeSubscriptionId",
                            new { Status = "canceled", StripeSubscriptionId = subscriptionId }
                        );
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        [HttpPost("orders/refund")]
        public async Task<IActionResult> OrderRefundWebhook()
        {
            try
            {
                // Configure stripe event
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    ordersWebhookSecretKey
                );

                // Handle the event
                if (stripeEvent.Type == EventTypes.ChargeRefunded)
                {
                    var charge = stripeEvent.Data.Object as Charge;
                    var paymentIntentId = charge.PaymentIntentId;

                    // manipulate the database
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        using (var transaction = await connection.BeginTransactionAsync())
                        {
                            try
                            {
                                // update order status
                                await connection.ExecuteAsync(
                                    "UPDATE ProductOrder SET OrderStatus = @OrderStatus WHERE TransactionId = @TransactionId",
                                    new { OrderStatus = OrderStatus.canceled.ToString(), TransactionId = paymentIntentId },
                                    transaction
                                );

                                // restore the product quantity
                                var orderItems = await connection.QueryAsync<OrderItem>(
                                    "SELECT * FROM ProductOrderItem WHERE OrderId = (SELECT OrderId FROM ProductOrder WHERE TransactionId = @TransactionId)",
                                    new { TransactionId = paymentIntentId },
                                    transaction
                                );
                                foreach (var orderItem in orderItems)
                                {
                                    await connection.ExecuteAsync(
                                        "UPDATE Product SET Quantity = Quantity + @Quantity WHERE ProductId = @ProductId",
                                        new { Quantity = orderItem.Quantity, ProductId = orderItem.ProductId },
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
