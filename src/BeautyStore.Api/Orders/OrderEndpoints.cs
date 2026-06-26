using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BeautyStore.Api.Data;
using BeautyStore.Api.Exceptions;
using BeautyStore.Api.Orders.Dtos;
using BeautyStore.Api.Orders.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace BeautyStore.Api.Orders;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this RouteGroupBuilder group)
    {
        // ── POST /api/orders ──────────────────────────────────────────────────
        group.MapPost("/", async (
            CreateOrderRequest                  req,
            ClaimsPrincipal                     principal,
            HttpContext                         httpContext,
            [FromServices] BeautyStoreDbContext db,
            [FromServices] IConfiguration       config,
            [FromServices] ILogger<Program>     logger,
            [FromServices] ServiceBusClient?    sbClient) =>
        {
            if (req.Quantity < 1)
                throw new ValidationException(
                    "Quantity must be at least 1.",
                    new Dictionary<string, string[]> { ["quantity"] = ["Must be greater than 0."] });

            var product = await db.Products
                .Where(p => p.Id == req.ProductId && p.IsActive)
                .Select(p => new { p.Name, p.Price })
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException($"Product {req.ProductId} not found in the catalog.");

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
                    logger.LogWarning(
                        ex,
                        "Service Bus publish failed for OrderId {OrderId} — order saved, event lost. " +
                        "ExceptionType: {ExceptionType} | TraceId: {TraceId}",
                        order.Id, ex.GetType().Name, httpContext.TraceIdentifier);
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
                .Select(o => new OrderResponse(o.Id, o.ProductName, o.Quantity, o.TotalPrice, o.Status, o.CreatedAtUtc))
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
