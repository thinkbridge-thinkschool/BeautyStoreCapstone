namespace BeautyStore.Api.Admin.Dtos;

public sealed record AdminUserDto(
    string        Id,
    string        Email,
    string        UserName,
    IList<string> Roles);

public sealed record AssignRoleRequest(string Role);
