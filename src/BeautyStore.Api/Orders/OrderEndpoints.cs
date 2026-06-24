using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BeautyStore.Api.Data;
using BeautyStore.Api.Orders.Dtos;
using BeautyStore.Api.Orders.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace BeautyStore.Api.Orders;

public static class OrderEndpoints
{
    private static readonly IReadOnlyDictionary<int, (string Name, decimal Price)> Catalog =
        new Dictionary<int, (string Name, decimal Price)>
        {
            [1] = ("Pro Filt'r Soft Matte Foundation",      3800m),
            [2] = ("Pillow Talk Matte Revolution Lipstick",  2850m),
            [3] = ("Orgasm Blush Powder",                    2200m),
            [4] = ("Protini Polypeptide Moisturiser",        5600m),
            [5] = ("Facial Treatment Essence",              12500m),
            [6] = ("Rose Gold Eyeshadow Palette",            4800m),
        };

    public static void MapOrderEndpoints(this RouteGroupBuilder group)
    {
        // ── POST /api/orders ──────────────────────────────────────────────────
        group.MapPost("/", async (
            CreateOrderRequest                  req,
            ClaimsPrincipal                     principal,
            [FromServices] BeautyStoreDbContext db,
            [FromServices] IConfiguration       config,
            [FromServices] ILogger<Program>     logger,
            [FromServices] ServiceBusClient?    sbClient) =>
        {
            if (req.Quantity < 1)
                return Results.BadRequest(new { error = "Quantity must be at least 1." });

            if (!Catalog.TryGetValue(req.ProductId, out var product))
                return Results.BadRequest(new { error = "Product not found." });

            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userId is null) return Results.Unauthorized();

            var now = DateTime.UtcNow;
            var order = new Order
            {
                UserId       = userId,
                ProductId    = req.ProductId,
                ProductName  = product.Name,
                Quantity     = req.Quantity,
                UnitPrice    = product.Price,
                TotalPrice   = product.Price * req.Quantity,
                Status       = "Created",
                CreatedAtUtc = now,
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            if (sbClient is not null)
            {
                try
                {
                    var topicName = config["ServiceBus:OrderEventsTopic"] ?? "order-events";
                    await using var sender = sbClient.CreateSender(topicName);
                    var evt = new OrderCreatedEvent(order.Id, order.UserId, order.ProductId, order.Quantity, now);
                    await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(evt))
                    {
                        ContentType = "application/json",
                        Subject     = "OrderCreated",
                        MessageId   = order.Id.ToString(),
                    });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "OrderCreated publish failed for order {OrderId}", order.Id);
                }
            }

            return Results.Created(
                $"/api/orders/{order.Id}",
                new { orderId = order.Id, status = order.Status });
        })
        .WithName("PlaceOrder")
        .WithSummary("Place a new order.");

        // ── GET /api/orders/my ────────────────────────────────────────────────
        group.MapGet("/my", async (
            ClaimsPrincipal                     principal,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userId is null) return Results.Unauthorized();

            var orders = await db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAtUtc)
                .Select(o => new OrderResponse(o.Id, o.ProductName, o.Quantity, o.TotalPrice, o.Status))
                .ToListAsync();

            return Results.Ok(orders);
        })
        .WithName("GetMyOrders")
        .WithSummary("Returns all orders for the authenticated user.");

        // ── DELETE /api/orders/{id} ───────────────────────────────────────────
        group.MapDelete("/{id:int}", async (
            int                                 id,
            ClaimsPrincipal                     principal,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userId is null) return Results.Unauthorized();

            var order = await db.Orders.FindAsync(id);
            if (order is null) return Results.NotFound();
            if (order.UserId != userId) return Results.Forbid();

            db.Orders.Remove(order);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteOrder")
        .WithSummary("Deletes an order belonging to the authenticated user.");
    }
}
