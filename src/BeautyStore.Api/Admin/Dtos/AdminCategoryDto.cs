namespace BeautyStore.Api.Admin.Dtos;

public sealed record AdminCategoryDto(
    int     Id,
    string  Name,
    string  Slug,
    string? Description,
    string? ImageUrl,
    int     DisplayOrder,
    bool    IsActive,
    int     ProductCount);

public sealed record CreateCategoryRequest(
    string  Name,
    string  Slug,
    string? Description,
    string? ImageUrl,
    int     DisplayOrder);

public sealed record UpdateCategoryRequest(
    string  Name,
    string  Slug,
    string? Description,
    string? ImageUrl,
    int     DisplayOrder,
    bool    IsActive);
