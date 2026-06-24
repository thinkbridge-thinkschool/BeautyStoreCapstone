namespace BeautyStore.Api.Orders.Events;

public record OrderCreatedEvent(
    int      OrderId,
    string   UserId,
    int      ProductId,
    int      Quantity,
    DateTime OccurredAtUtc
);
