using IdentityJWT.ClientMVC.Infra;
using IdentityJWT.ClientMVC.Models.ViewModels;
using IdentityJWT.Models.Requests;
using IdentityJWT.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace IdentityJWT.ClientMVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly string _authServerUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _authServerUrl = configuration["ClientAuthSettings:AuthServerUrl"];
        }


        #region Login
        [HttpGet]
        public IActionResult Login([FromQuery(Name = "ReturnUrl")] string returnUrl)
        {
            return View(new LoginViewModel() {ReturnUrl = returnUrl });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel viewModel, [FromQuery(Name = "ReturnUrl")] string returnUrl)
        {
            string loginUrl = Endpoints.AuthServer.Auth.Login(_authServerUrl);
            HttpClient client = new HttpClient();
            LoginRequest request = new LoginRequest()
            {
                Email = viewModel.Email,
                Password = viewModel.Password
            };
            var response = await client.PostAsync(loginUrl, request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var rawResponse = (await response.Content.ReadAsStringAsync());
                var login = rawResponse.Deserialize<LoginResponse>();
                _httpContextAccessor.HttpContext.Session.SetString("user_token.Access", login.AccessToken);
                _httpContextAccessor.HttpContext.Session.SetString("user_token.Refresh", login.RefreshToken);
                _httpContextAccessor.HttpContext.Session.SetString("user_token.Expires", login.Expires.ToString());

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return LocalRedirect(returnUrl);

                }
                else
                {
                    return RedirectToAction(nameof(CarsController.IndexPublic), "Cars");
                }
            }
            else
            {

            }
            return View(viewModel);
        }
        [HttpGet]
        public IActionResult ShowTokens()
        {
            string rawExpire = _httpContextAccessor.HttpContext.Session.GetString("user_token.Expires");
            DateTime? expires = null;
            if (!string.IsNullOrEmpty(rawExpire))
            {
                bool v = long.TryParse(rawExpire, out long myLong);
                if (v)
                {
                    expires = myLong.ToUnixTime();
                }
            }
            var response = new
            {
                Token = _httpContextAccessor.HttpContext.Session.GetString("user_token.Access"),
                RefreshToken = _httpContextAccessor.HttpContext.Session.GetString("user_token.Refresh"),
                Expires = expires,
            };

            return Json(response);
        }
        #endregion

        #region Sign Up
        [HttpGet]
        public IActionResult SignUp()
        {
            return View(new SignUpViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUpViewModel viewModel)
        {
            IActionResult result = View(viewModel);
            if (ModelState.IsValid)
            {
                try
                {
                    string callbackUrl = Url.ActionLink(nameof(ConfirmEmail));
                    string signUpUrl = Endpoints.AuthServer.Auth.SignUp(_authServerUrl, callbackUrl);
                    HttpClient client = new HttpClient();
                    SignUpRequest request = new SignUpRequest()
                    {
                        UserName = viewModel.UserName,
                        Email = viewModel.Email,
                        FirstName = viewModel.FirstName,
                        LastName = viewModel.LastName,
                        Age = viewModel.Age,
                        Password = viewModel.Password,
                    };
                    var response = await client.PostAsync(signUpUrl, request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        result = RedirectToAction(nameof(Login));
                    }
                    else
                    {
                        var rawResponse = (await response.Content.ReadAsStringAsync());
                        ModelState.AddModelError("SignUp", rawResponse);
                    }
                }
                catch
                {
                    ModelState.AddModelError("SignUp", "Fail to create the use");
                }
            }
            return result;
        }
        #endregion

        #region Confirm Email
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail([FromQuery(Name = "user-id")] string userId, [FromQuery] string token)
        {
            string message = "Invalid User/Token";
            if (int.TryParse(userId, out int uId) && !string.IsNullOrEmpty(token))
            {
                try
                {
                    string signUpUrl = Endpoints.AuthServer.Auth.ConfirmAccount(_authServerUrl);
                    HttpClient client = new HttpClient();
                    ConfirmAccountRequest request = new ConfirmAccountRequest()
                    {
                        UserId = uId,
                        Token = token,
                    };
                    var response = await client.PostAsync(signUpUrl, request);
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        var rawResponse = (await response.Content.ReadAsStringAsync());
                        message = "Account Confirmed";
                    }
                    else
                    {
                        message = "Account Confirm Failed";
                    }
                }
                catch
                {
                    message = "Account Confirm Failed";

                }
            }
            return View(new ConfirmAccountViewModel() { Message = message });
        }
        #endregion

        #region Forgot Password
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel viewModel)
        {
            IActionResult result = View(viewModel);
            if (ModelState.IsValid)
            {
                try
                {
                    string callbackUrl = Url.ActionLink(nameof(ResetPassword));
                    string signUpUrl = Endpoints.AuthServer.Auth.ResetPasswordSendToken(_authServerUrl, callbackUrl);
                    HttpClient client = new HttpClient();
                    ResetPasswordSendTokenRequest request = new ResetPasswordSendTokenRequest()
                    {
                        Email = viewModel.Email
                    };
                    var response = await client.PostAsync(signUpUrl, request);
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        var rawResponse = (await response.Content.ReadAsStringAsync());
                        result = RedirectToAction(nameof(Login));
                    }
                    else
                    {
                        ModelState.AddModelError("Email", "Fail for some reason");
                    }
                }
                catch
                {
                    ModelState.AddModelError("Email", "Fail for some reason");
                }
            }
            return result;
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            var viewModel = new ResetPasswordViewModel() { Email = email, Token = token };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel viewModel)
        {
            IActionResult result = View(viewModel);
            if (ModelState.IsValid)
            {
                try
                {
                    string signUpUrl = Endpoints.AuthServer.Auth.ResetPassword(_authServerUrl);
                    HttpClient client = new HttpClient();
                    ResetPasswordRequest request = new ResetPasswordRequest()
                    {
                        Email = viewModel.Email,
                        Token = viewModel.Token,
                        Password = viewModel.Password

                    };
                    var response = await client.PostAsync(signUpUrl, request);
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        var rawResponse = (await response.Content.ReadAsStringAsync());
                        result = RedirectToAction(nameof(Login));
                    }
                    else
                    {
                        ModelState.AddModelError("Email", "Fail for some reason");
                    }
                }
                catch
                {
                    ModelState.AddModelError("Email", "Fail for some reason");
                }
            }
            return result;
        }
        #endregion

        #region Logout
        [HttpGet]
        public IActionResult SignOut()
        {
            _httpContextAccessor.HttpContext.Session.Remove("user_token.Access");
            _httpContextAccessor.HttpContext.Session.Remove("user_token.Refresh");
            _httpContextAccessor.HttpContext.Session.Remove("user_token.Expires");
            return RedirectToAction(nameof(Login));
        }
        #endregion
    }
}
