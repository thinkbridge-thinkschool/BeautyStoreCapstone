namespace BeautyStore.Api.Admin.Dtos;

public sealed record AnalyticsDto(
    decimal                 RevenueToday,
    decimal                 RevenueThisMonth,
    int                     OrdersToday,
    int                     OrdersThisMonth,
    int                     CustomerCount,
    int                     ProductCount,
    int                     CategoryCount,
    int                     LowStockCount,
    IList<TopProductDto>    TopProducts,
    IList<TopCategoryDto>   TopCategories,
    IList<RevenueTrendDto>  RevenueTrend);

public sealed record TopProductDto(
    int     ProductId,
    string  ProductName,
    decimal Revenue,
    int     TotalSold);

public sealed record TopCategoryDto(
    string  CategoryName,
    decimal Revenue,
    int     OrderCount);

public sealed record RevenueTrendDto(
    DateOnly Date,
    decimal  Revenue,
    int      OrderCount);
