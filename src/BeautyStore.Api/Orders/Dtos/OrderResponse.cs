namespace BeautyStore.Api.Orders.Dtos;

public record OrderResponse(
    int      OrderId,
    string   ProductName,
    int      Quantity,
    decimal  TotalPrice,
    string   Status,
    DateTime CreatedAtUtc
);
