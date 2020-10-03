using System.ComponentModel.DataAnnotations;

namespace IdentityJWT.ClientMVC.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }
}
