using Microsoft.AspNetCore.Identity;

namespace ClientMVC.Models.Identity
{
    public class AppIdentityUser : IdentityUser<long>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
    }
}
