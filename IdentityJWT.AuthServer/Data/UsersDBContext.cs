using IdentityJWT.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityJWT.API.Data
{
    public class UsersDBContext :
       IdentityDbContext
       <
           AppIdentityUser,
           AppIdentityRole,
           long,
           IdentityUserClaim<long>,
           AppIdentityUserRole,
           IdentityUserLogin<long>,
           IdentityRoleClaim<long>,
           IdentityUserToken<long>
       >
    {
        public UsersDBContext() { }
        public UsersDBContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            string UsersShcema = "Core";

            modelBuilder.Entity<AppIdentityUser>(b =>
            {
                b.ToTable("Users", UsersShcema);
            });

            modelBuilder.Entity<AppIdentityRole>(b =>
            {
                b.ToTable("Roles", UsersShcema);
            });

            modelBuilder.Entity<IdentityUserClaim<long>>(b =>
            {
                b.ToTable("Claims", UsersShcema);
            });

            modelBuilder.Entity<AppIdentityUserRole>(b =>
            {
                b.ToTable("User_Roles", UsersShcema);
            });

            modelBuilder.Entity<IdentityUserLogin<long>>(b =>
            {
                b.ToTable("UserLogins", UsersShcema);
            });

            modelBuilder.Entity<IdentityUserToken<long>>(b =>
            {
                b.ToTable("UserTokens", UsersShcema);
            });

            modelBuilder.Entity<IdentityRoleClaim<long>>(b =>
            {
                b.ToTable("Role_Claims", UsersShcema);
            });
        }
    }
}
