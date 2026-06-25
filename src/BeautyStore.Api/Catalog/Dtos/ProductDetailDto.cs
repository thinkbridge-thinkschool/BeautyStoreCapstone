namespace BeautyStore.Api.Catalog.Dtos;

public sealed record ProductDetailDto(
    int              Id,
    int              CategoryId,
    string           CategoryName,
    string           CategorySlug,
    string           Name,
    string           Brand,
    string?          Description,
    decimal          Price,
    float            Rating,
    int              Stock,
    string?          ImageUrl,
    bool             IsFeatured,
    IList<ProductDto> RelatedProducts);
