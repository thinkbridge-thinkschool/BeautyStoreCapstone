namespace BeautyStore.Api.Admin.Dtos;

public sealed record InventoryItemDto(
    int     ProductId,
    string  ProductName,
    string  CategoryName,
    int     CurrentStock,
    decimal Price,
    bool    IsActive,
    bool    LowStock);

public sealed record UpdateStockRequest(int Stock);
