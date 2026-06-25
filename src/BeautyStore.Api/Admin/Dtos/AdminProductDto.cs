namespace BeautyStore.Api.Admin.Dtos;

public sealed record AdminProductDto(
    int      Id,
    int      CategoryId,
    string   CategoryName,
    string   Name,
    string   Brand,
    string?  Description,
    decimal  Price,
    float    Rating,
    int      Stock,
    string?  ImageUrl,
    bool     IsFeatured,
    bool     IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateProductRequest(
    int      CategoryId,
    string   Name,
    string   Brand,
    string?  Description,
    decimal  Price,
    float    Rating,
    int      Stock,
    string?  ImageUrl,
    bool     IsFeatured);

public sealed record UpdateProductRequest(
    int      CategoryId,
    string   Name,
    string   Brand,
    string?  Description,
    decimal  Price,
    float    Rating,
    int      Stock,
    string?  ImageUrl,
    bool     IsFeatured,
    bool     IsActive);
