using Microsoft.AspNetCore.Identity;

namespace IdentityJWT.Models.Identity
{
    public class AppIdentityRole : IdentityRole<long>
    {
        public AppIdentityRole() : base() { }
        public AppIdentityRole(string roleName) : base(roleName) { }
    }
}
