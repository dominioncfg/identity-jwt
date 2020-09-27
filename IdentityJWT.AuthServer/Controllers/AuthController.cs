using IdentityJWT.Models.Configuration;
using IdentityJWT.Models.Identity;
using IdentityJWT.Models.Requests;
using IdentityJWT.Models.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityJWT.AuthServer.Controllers
{
    [ApiController]
    [Route("api/{controller}")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppIdentityUser> _userManager;
        private readonly SignInManager<AppIdentityUser> _signInManager;
        private readonly JwtOptions _options;

        public AuthController(
                                UserManager<AppIdentityUser> userManager,
                                SignInManager<AppIdentityUser> signInManager,
                                JwtOptions options
                             )
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._options = options;
        }

        private static long ToUnixEpochDate(DateTime date)
           => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
               .TotalSeconds);

        [HttpPost]
        [Route("login")]
        public async Task<LoginResponse> Login(LoginRequest login)
        {
            var user = await _userManager.FindByEmailAsync(login.Email);
            if (user != null)
            {
                bool isValidPass = await _userManager.CheckPasswordAsync(user, login.Password);
                if (isValidPass)
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

                    return new LoginResponse
                    {
                        AccessToken = token,
                        RefreshToken = string.Empty,
                        Expires = ToUnixEpochDate(_options.Expiration),
                        UserId = user.Id.ToString()
                    };
                }


            }
            return new LoginResponse { };
        }
    }
}
