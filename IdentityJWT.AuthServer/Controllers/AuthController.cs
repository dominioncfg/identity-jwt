using IdentityJWT.API.Data;
using IdentityJWT.AuthServer.Models.Identity;
using IdentityJWT.Models.Configuration;
using IdentityJWT.Models.Identity;
using IdentityJWT.Models.Requests;
using IdentityJWT.Models.Responses;
using Microsoft.AspNetCore.Authorization;
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

        public AuthController(
                                UserManager<AppIdentityUser> userManager,
                                SignInManager<AppIdentityUser> signInManager,
                                UsersDBContext usersDBContext,
                                JwtOptions options
                             )
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._options = options;
            this._usersDBContext = usersDBContext;
        }

        private static long ToUnixEpochDate(DateTime date)
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

        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<LoginResponse> Login(LoginRequest request)
        {
            string requestEmail = request.Email;
            string requestPass = request.Password;

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

                    return new LoginResponse
                    {
                        AccessToken = token,
                        RefreshToken = refreshToken.Token,
                        Expires = ToUnixEpochDate(_options.Expiration),
                        UserId = user.Id.ToString()
                    };
                }


            }
            else
            {

            }
            return new LoginResponse { };
        }

        

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

                    if(success)
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
    }
}
