namespace BeautyStore.Api.Admin.Dtos;

public sealed record AdminOrderDto(
    int      Id,
    string   UserId,
    string   ProductName,
    int      Quantity,
    decimal  TotalPrice,
    string   Status,
    DateTime CreatedAtUtc,
    string   UserEmail = "",
    string   UserName  = "");

public sealed record UpdateOrderStatusRequest(string Status);
