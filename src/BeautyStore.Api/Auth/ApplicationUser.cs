using Microsoft.AspNetCore.Identity;

namespace BeautyStore.Api.Auth;

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
