namespace IdentityJWT.Models.Configuration
{
    public class RsaSettings
    {
        public bool IsIssuer { get; set; }
        public string RsaPrivateKey { get; set; }
        public string RsaPublicKey { get; set; }
    }
}
