namespace BeautyStore.Api.Auth.Dtos;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string Email,
    string FullName,
    IList<string> Roles);
