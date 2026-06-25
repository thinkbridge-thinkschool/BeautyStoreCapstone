namespace BeautyStore.Api.Admin.Dtos;

public sealed record DashboardDto(
    int     TotalOrders,
    decimal TotalRevenue,
    int     TotalProducts,
    int     TotalUsers,
    IList<AdminOrderDto> RecentOrders);
