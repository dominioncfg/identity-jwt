using ClientMVC.Models.Identity;
using IdentityJWT.ClientMVC.Services.HttpClientService;
using IdentityJWT.Infra;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace IdentityJWT.ClientMVC
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(30);
                options.Cookie.IsEssential = true;
            });
            services.AddDistributedMemoryCache();
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddJwtAuthentication();
            services.AddControllersWithViews();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("UserOfAnyRolePolicy", policy =>
                {
                    policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    string[] roles = Enum.GetNames(typeof(Roles));
                    policy.RequireRole(roles);
                });

                options.AddPolicy("AdminUserOnlyPolicy", policy =>
                {
                    policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireRole(Roles.Admin.ToString());
                });
            });
            services.AddScoped<IHttpClientService, JwtSecuredHttpClientService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();

            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Cars}/{action=IndexPublic}/{id?}");
            });
        }
    }
}
