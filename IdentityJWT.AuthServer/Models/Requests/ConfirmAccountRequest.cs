namespace IdentityJWT.Models.Requests
{
    public class ConfirmAccountRequest
    {
        public int UserId { get; set; }
        public string Token { get; set; }
    }
}
