using IdentityJWT.AuthServer.Models.Identity;
using IdentityJWT.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

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
        public DbSet<RefreshToken<long>> RefreshTokens { get; set; }

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

            #region Configure Refresh Token Table
            modelBuilder.Entity<RefreshToken<long>>()
                .ToTable("RefreshTokens", UsersShcema);

            modelBuilder.Entity<RefreshToken<long>>()
               .HasOne<AppIdentityUser>()
               .WithMany()
               .HasForeignKey(e => e.UserId);

            modelBuilder.Entity<RefreshToken<long>>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<RefreshToken<long>>()
                .Property(s => s.Token)
                .IsRequired();

            modelBuilder.Entity<RefreshToken<long>>()
                .Property(s => s.RevokedAt)
                .IsRequired(false);

            modelBuilder.Entity<RefreshToken<long>>()
                .Property(s => s.CreatedAt)
                .IsRequired()
                .HasDefaultValue(DateTime.Now);

            modelBuilder.Entity<RefreshToken<long>>()
                .Property(s => s.Expires)
                .IsRequired();
            #endregion
        }
    }
}
