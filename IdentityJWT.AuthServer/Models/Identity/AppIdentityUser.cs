using Microsoft.AspNetCore.Identity;

namespace IdentityJWT.Models.Identity
{
    public class AppIdentityUser : IdentityUser<long>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
    }
}
