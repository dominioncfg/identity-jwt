using IdentityJWT.Infra;
using IdentityJWT.Models.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace IdentityJWT.API
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddJwtAuthentication();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("UserOfAnyRolePolicy", policy =>
                {
                    string[] roles = Enum.GetNames(typeof(Roles));
                    policy.RequireRole(roles);
                });

                options.AddPolicy("AdminUserOnlyPolicy", policy =>
                {
                    policy.RequireRole(Roles.Admin.ToString());
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();                
            });
        }
    }
}
