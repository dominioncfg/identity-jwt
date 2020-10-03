using Microsoft.AspNetCore.Authorization;

namespace IdentityJWT.Infra
{
    public class AuthorizeAdminUserOnlyPolicyAttribute : AuthorizeAttribute
    {
        public AuthorizeAdminUserOnlyPolicyAttribute() : base("AdminUserOnlyPolicy") { }
    }
}
