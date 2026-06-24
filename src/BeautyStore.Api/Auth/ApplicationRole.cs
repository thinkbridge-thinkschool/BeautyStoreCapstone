using Microsoft.AspNetCore.Identity;

namespace BeautyStore.Api.Auth;

public sealed class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
