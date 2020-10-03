using ClientMVC.Models.Configuration;
using IdentityJWT.ClientMVC.Infra;
using IdentityJWT.Models.Requests;
using IdentityJWT.Models.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace IdentityJWT.Infra
{
    public static class AuthenticationServiceCollectionExtensions
    {
        public static void AddJwtAuthentication(this IServiceCollection services,
            TokenValidationParameters parameters = null)
        {
            IConfiguration configuration;
            JwtOptions jwtOptions = new JwtOptions();
            var authSettings = new AuthenticationSettings();

            using (var serviceProvider = services.BuildServiceProvider())
            {
                configuration = serviceProvider.GetService<IConfiguration>();
            }

            services.AddHttpContextAccessor();
            configuration.GetSection(nameof(AuthenticationSettings)).Bind(authSettings);

            configuration.GetSection(nameof(JwtOptions)).Bind(jwtOptions);
            jwtOptions.SigningCredentials = GetCredentialSigningKey(authSettings);

            services.TryAddSingleton(jwtOptions);
            services.TryAddSingleton(authSettings);
            var validationParameters = new TokenValidationParameters
            {
                // Ensure the token was issued by a trusted authorization server (default true)
                ValidateIssuer = jwtOptions.ValidateIssuer,
                ValidIssuer = jwtOptions.Issuer,
                // Ensure the token audience matches our audience value (default true)
                ValidateAudience = jwtOptions.ValidateAudience,
                ValidAudience = jwtOptions.Audience,
                // Specify the key used to sign the token
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                IssuerSigningKey = jwtOptions.SigningCredentials.Key,
                // Ensure the token hasn't expired
                RequireExpirationTime = true,
                ValidateLifetime = true,
                // Clock skew compensates for server time drift.
                ClockSkew = TimeSpan.FromMinutes(5),


            };

            services.AddAuthentication(authOptions =>
            {
                authOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, configurationOptions =>
            {
                //configurationOptions.ForwardChallenge = CookieAuthenticationDefaults.AuthenticationScheme;
                configurationOptions.Events = new JwtBearerEvents()
                {
                    OnTokenValidated = tokenValidationContext =>
                    {
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = async messageReceivedContext =>
                    {
                        string token = messageReceivedContext.Request.HttpContext.Session.GetString("user_token.Access");
                        string rawExpire = messageReceivedContext.Request.HttpContext.Session.GetString("user_token.Expires");
                        //Try to refresh the token if is about to expire
                        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(rawExpire))
                        {
                            DateTime? expires = null;
                            if (!string.IsNullOrEmpty(rawExpire))
                            {
                                bool v = long.TryParse(rawExpire, out long myLong);
                                if (v)
                                {
                                    expires = myLong.ToUnixTime();
                                    var nowUtc = DateTime.UtcNow;
                                    bool aboutToExpire = expires >= nowUtc.AddMinutes(-2);
                                    if (aboutToExpire)
                                    {
                                        string authServerUrl = configuration["ClientAuthSettings:AuthServerUrl"];
                                        string refreshToken = messageReceivedContext.Request.HttpContext.Session.GetString("user_token.Refresh");
                                        var refreshTokenResult = await RefreshTokenAsync(authServerUrl, refreshToken);

                                        if (refreshTokenResult.Item1)
                                        {
                                            var tokenResult = refreshTokenResult.Item2;
                                            messageReceivedContext.HttpContext.Session.SetString("user_token.Access", tokenResult.AccessToken);
                                            messageReceivedContext.HttpContext.Session.SetString("user_token.Refresh", tokenResult.RefreshToken);
                                            messageReceivedContext.HttpContext.Session.SetString("user_token.Expires", tokenResult.Expires.ToString());
                                            token = tokenResult.AccessToken;
                                        }
                                        else
                                        {
                                            messageReceivedContext.HttpContext.Session.Remove("user_token.Access");
                                            messageReceivedContext.HttpContext.Session.Remove("user_token.Refresh");
                                            messageReceivedContext.HttpContext.Session.Remove("user_token.Expires");
                                        }
                                    }

                                }
                            }
                            messageReceivedContext.Token = token;
                        }
                        //return Task.CompletedTask;
                    },

                    OnAuthenticationFailed = authenticationFailedContext =>
                    {
                        if (authenticationFailedContext.Exception.GetType() ==
                            typeof(SecurityTokenExpiredException))
                        {
                            authenticationFailedContext.Response.Headers.Add("Token-Expired", "true");                         

                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = jwtBearerChallengeContext =>
                    {
                        jwtBearerChallengeContext.Response.Redirect("/Auth/Login");
                        jwtBearerChallengeContext.HandleResponse();

                        //var redirectUri = .red properties.RedirectUri;
                        //if (string.IsNullOrEmpty(redirectUri))
                        //{
                        //    redirectUri = OriginalPathBase + OriginalPath + Request.QueryString;
                        //}

                        //var loginUri = Options.LoginPath + QueryString.Create(Options.ReturnUrlParameter, redirectUri);
                        //var redirectContext = new RedirectContext<CookieAuthenticationOptions>(Context, Scheme, Options, properties, BuildRedirectUri(loginUri));
                        //await configurationOptions.R..RedirectToLogin(redirectContext);

                        return Task.CompletedTask;
                    }
                };

                configurationOptions.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
                //set SaveTokens to save tokens to the AuthenticationProperties
                //https://www.jerriepelser.com/blog/aspnetcore-jwt-saving-bearer-token-as-claim/
                //https://www.jerriepelser.com/blog/accessing-tokens-aspnet-core-2/
                configurationOptions.SaveToken = jwtOptions.SaveToken;
                configurationOptions.ClaimsIssuer = jwtOptions.Issuer;
                configurationOptions.TokenValidationParameters = validationParameters;
            });

        }

        private static async Task<(bool, RefreshTokenResponse)> RefreshTokenAsync(string authServerUrl, string refreshToken)
        {
            string loginUrl = Endpoints.AuthServer.Auth.RefreshToken(authServerUrl);
            HttpClient client = new HttpClient();
            RefreshTokenRequest request = new RefreshTokenRequest()
            {
                AccessToken = "No se usa todavia",
                RefreshToken = refreshToken

            };
            var response = await client.PostAsync(loginUrl, request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var rawResponse = (await response.Content.ReadAsStringAsync());
                var result = rawResponse.Deserialize<RefreshTokenResponse>();

                return (true, result);
            }
            else
            {
                return (false, null);
            }
        }

        public static long ToUnixEpochDate(this DateTime date)
        {
            return (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
                .TotalSeconds);

        }

        #region Private Methods

        private static SigningCredentials GetCredentialSigningKey(AuthenticationSettings setting)
        {
            return GetRsaKey(setting);
        }

        private static SigningCredentials GetRsaKey(AuthenticationSettings setting)
        {
            if (setting.RsaSettings == null)
                return null;

            if (setting.RsaSettings.IsIssuer)
            {
                if (string.IsNullOrWhiteSpace(setting.RsaSettings.RsaPrivateKey))
                {
                    return null;
                }

                RSA privateRsa = RSA.Create();
                var privateKeyXml = File.ReadAllText(setting.RsaSettings.RsaPrivateKey);
                privateRsa.FromXmlString(privateKeyXml);
                var privateKey = new RsaSecurityKey(privateRsa);
                return new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
            }
            else
            {
                RSA publicRsa = RSA.Create();
                var publicKeyXml = File.ReadAllText(setting.RsaSettings.RsaPublicKey);
                publicRsa.FromXmlString(publicKeyXml);
                var publicKey = new RsaSecurityKey(publicRsa);
                return new SigningCredentials(publicKey, SecurityAlgorithms.RsaSha256);
            }
        }

        #endregion
    }
}