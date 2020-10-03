namespace IdentityJWT.ClientMVC.Infra
{
    public static class Endpoints
    {
        public static class AuthServer
        {
            public static class Auth
            {
                public static string Login(string baseUrl) => $"{baseUrl}/api/auth/login";
                public static string RefreshToken(string baseUrl) => $"{baseUrl}/api/auth/refresh-token";
                public static string SignUp(string baseUrl, string callbackUrl) => $"{baseUrl}/api/auth/sign-up?callbackUrl={callbackUrl}";
                public static string ConfirmAccount(string baseUrl) => $"{baseUrl}/api/auth/confirm-email";
                public static string ResetPasswordSendToken(string baseUrl, string callbackUrl) => $"{baseUrl}/api/auth/reset-password-send-token?callbackUrl={callbackUrl}";
                public static string ResetPassword(string baseUrl) => $"{baseUrl}/api/auth/reset-password";


            }
        }
      
        public static class Api
        {
            public static class Cars
            {
                public static string Public(string baseUrl) => $"{baseUrl}/api/cars/public";
                public static string AnyLoggedOn(string baseUrl) => $"{baseUrl}/api/cars/any-logged-on";
                public static string AnyRole(string baseUrl) => $"{baseUrl}/api/cars/any-role";
                public static string Admin(string baseUrl) => $"{baseUrl}/api/cars/admin";
            }
        }
    }
}
