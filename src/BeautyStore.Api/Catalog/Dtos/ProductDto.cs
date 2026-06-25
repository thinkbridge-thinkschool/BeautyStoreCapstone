namespace BeautyStore.Api.Catalog.Dtos;

public sealed record ProductDto(
    int     Id,
    int     CategoryId,
    string  CategoryName,
    string  Name,
    string  Brand,
    decimal Price,
    float   Rating,
    int     Stock,
    string? ImageUrl,
    bool    IsFeatured);
