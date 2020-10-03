using Microsoft.AspNetCore.Authorization;

namespace IdentityJWT.Infra
{
    public class AuthorizeAnyAppRolePolicyAttribute : AuthorizeAttribute
    {
        public AuthorizeAnyAppRolePolicyAttribute() : base("UserOfAnyRolePolicy") { }
    }
}
