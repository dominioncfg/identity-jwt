using IdentityJWT.API.Data;
using IdentityJWT.AuthServer.Models.Identity;
using IdentityJWT.AuthServer.Services.Email;
using IdentityJWT.Models.Configuration;
using IdentityJWT.Models.Identity;
using IdentityJWT.Models.Requests;
using IdentityJWT.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace IdentityJWT.AuthServer.Controllers
{
    [ApiController]
    [Route("api/{controller}")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppIdentityUser> _userManager;
        private readonly SignInManager<AppIdentityUser> _signInManager;
        private readonly UsersDBContext _usersDBContext;
        private readonly JwtOptions _options;
        private readonly IEmailService _emailService;

        public AuthController(
                                UserManager<AppIdentityUser> userManager,
                                SignInManager<AppIdentityUser> signInManager,
                                IEmailService emailService,
                                UsersDBContext usersDBContext,
                                JwtOptions options
                             )
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._emailService = emailService;
            this._options = options;
            this._usersDBContext = usersDBContext;
        }

        #region Private Methods
        private long ToUnixEpochDate(DateTime date)
           => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
               .TotalSeconds);

        private async Task<string> GenerateTokenAsync(AppIdentityUser user)
        {
            var roles = (await _userManager.GetRolesAsync(user)).ToImmutableList();
            var jwtClaims = new List<Claim>
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                        new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_options.IssuedAt).ToString(),
                        ClaimValueTypes.Integer64)
                    };

            if (roles != null && roles.Any())
            {
                var rolesConcatString = string.Join(",", roles);
                jwtClaims.Add(new Claim(ClaimTypes.Role, rolesConcatString));
            }

            var jwt = new JwtSecurityToken(
                                                _options.Issuer,
                                                claims: jwtClaims,
                                                expires: _options.Expiration,
                                                notBefore: _options.NotBefore,
                                                audience: _options.Audience,
                                                signingCredentials: _options.SigningCredentials
                                            );
            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return token;
        }

        private async Task SendConfirmationEmailAsync(AppIdentityUser user, string callbackUrl)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var queryBuilder = HttpUtility.ParseQueryString(string.Empty);
            queryBuilder["user-id"] = user.Id.ToString();
            queryBuilder["token"] = token;
            string queryString = queryBuilder.ToString();
            string confirmationLink = $"{callbackUrl}?{queryString}";
            string body = $"<a href='{confirmationLink}'>Confirm Email</a>";
            await _emailService.SendEmailAsync("info@mydomain.com", user.Email, "Confirm your email address", body);
        }
        #endregion

        #region Login
        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            IActionResult response = BadRequest();

            string requestEmail = request.Email;
            string requestPass = request.Password;

            if(!string.IsNullOrEmpty(requestEmail) && !string.IsNullOrEmpty(requestPass))
            {
                var user = await _userManager.FindByEmailAsync(requestEmail);
                if (user != null)
                {
                    bool isValidPass = await _userManager.CheckPasswordAsync(user, requestPass);
                    if (isValidPass)
                    {
                        string token = await GenerateTokenAsync(user);

                        var refreshToken = new RefreshToken<long>(user.Id, 4);

                        await _usersDBContext.AddAsync(refreshToken);
                        await _usersDBContext.SaveChangesAsync();

                        response = Ok(new LoginResponse
                        {
                            AccessToken = token,
                            RefreshToken = refreshToken.Token,
                            Expires = ToUnixEpochDate(_options.Expiration),
                            UserId = user.Id.ToString()
                        });
                    }
                    else
                    {
                        return BadRequest("Invalid User/Password");
                    }

                }
                else
                {
                    return BadRequest("Invalid User/Password");
                }
            }
            
            return response;
        }
        #endregion

        #region Refresh Token
        [HttpPost]
        [Route("refresh-token")]
        [AllowAnonymous]
        public async Task<RefreshTokenResponse> RefreshToken(RefreshTokenRequest request)
        {
            RefreshTokenResponse response = new RefreshTokenResponse();

            var refreshToken = await _usersDBContext.Set<RefreshToken<long>>()
                 .SingleOrDefaultAsync(x => x.Token == request.RefreshToken);

            if (refreshToken != null && refreshToken.IsValid())
            {
                var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
                if (user != null)
                {
                    var newRefreshToken = new RefreshToken<long>(user.Id, 4);

                    bool success = true;
                    try
                    {
                        _usersDBContext.Set<RefreshToken<long>>().Remove(refreshToken);
                        await _usersDBContext.Set<RefreshToken<long>>().AddAsync(newRefreshToken);
                        await _usersDBContext.SaveChangesAsync();
                    }
                    catch
                    {


                    }

                    if (success)
                    {
                        var token = await GenerateTokenAsync(user);
                        return new RefreshTokenResponse
                        {
                            AccessToken = token,
                            RefreshToken = newRefreshToken.Token,
                            Expires = ToUnixEpochDate(_options.Expiration),
                            UserId = user.Id.ToString()
                        };
                    }
                }
                else
                {

                }
            }
            else
            {

            }


            return response;
        }
        #endregion

        #region Sign Up    
        [HttpPost("sign-up")]
        [AllowAnonymous]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest model, [FromQuery] string callbackUrl)
        {
            IActionResult result = BadRequest("Bad Request");
            if (!string.IsNullOrEmpty(callbackUrl))
            {
                if (ModelState.IsValid)
                {
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser == null)
                    {
                        var user = new AppIdentityUser
                        {
                            UserName = model.UserName,
                            Email = model.Email,
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            Age = model.Age,
                        };

                        var creteResult = await _userManager.CreateAsync(user, model.Password);

                        if (creteResult.Succeeded)
                        {
                            user = await _userManager.FindByEmailAsync(model.Email);

                            await SendConfirmationEmailAsync(user, callbackUrl);
                            await _userManager.AddToRoleAsync(user, Roles.RegularUser.ToString());

                            var response = new SignUpResponse()
                            {
                                Id = user.Id,
                                UserName = user.UserName,
                                Email = user.Email,
                                FirstName = user.FirstName,
                                LastName = user.LastName,
                                Age = user.Age,
                            };
                            result = Ok(response);
                        }
                        else
                        {
                            result = StatusCode(StatusCodes.Status500InternalServerError, "Fail to create the user");
                        }
                    }
                    else
                    {
                        result = BadRequest("User Already Exists");
                    }
                }
            }
            else
            {
                result = BadRequest("Supply a calback Url");
            }


            return result;
        }
        #endregion

        #region Confirm Email
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmAccountRequest request)
        {
            IActionResult response = BadRequest();
            if (request.UserId > 0 && !string.IsNullOrEmpty(request.Token))
            {
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user != null)
                {
                    var result = await _userManager.ConfirmEmailAsync(user, request.Token);
                    if (result.Succeeded)
                    {
                        response = NoContent();
                    }
                    else
                    {
                        response = BadRequest("Invalid Token");
                    }
                }
                else
                {
                    response = NotFound("User does not exist");

                }
            }
            return response;
        }
        #endregion

        #region Forgot Password
        [HttpPost("reset-password-send-token")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ResetPasswordSendTokenRequest request, [FromQuery] string callbackUrl)
        {
            IActionResult result = BadRequest();
            if (!string.IsNullOrEmpty(request.Email) && !string.IsNullOrEmpty(callbackUrl))
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    result = NotFound(user);
                }
                else
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                    var queryBuilder = HttpUtility.ParseQueryString(string.Empty);
                    queryBuilder["email"] = user.Email.ToString();
                    queryBuilder["token"] = token;
                    string queryString = queryBuilder.ToString();
                    string callback = $"{callbackUrl}?{queryString}";
                    string body = $"<a href='{callback}'>Click to reset your password</a> or open in the browser {callback}";
                    await _emailService.SendEmailAsync("info@mydomain.com", user.Email, "Reset Your Account Password", body);
                    result = NoContent();
                }
            }
            return result;
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            IActionResult result = BadRequest();

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null)
            {
                var resetPassResult = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);
                if (!resetPassResult.Succeeded)
                {
                    foreach (var error in resetPassResult.Errors)
                    {
                        result = BadRequest("Error Reseting Password");
                    }
                }
                else
                {
                    result = NoContent();
                }
            }
            else
            {
                result = NotFound("There is no account for that email");
            }

            return result;
        }
        #endregion
    }
}
