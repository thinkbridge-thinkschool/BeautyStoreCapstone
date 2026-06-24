namespace BeautyStore.Api.Auth.Dtos;

public sealed record RegisterRequest(
    string FullName,
    string Email,
    string Password);
