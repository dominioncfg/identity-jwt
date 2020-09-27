using System;
using System.Security.Cryptography;

namespace IdentityJWT.AuthServer.Models.Identity
{
    public class RefreshToken<TKey> where TKey : IEquatable<TKey>
    {
        public TKey Id { get; private set; }
        public TKey UserId { get; private set; }
        public string Token { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public bool Revoked => RevokedAt.HasValue;
        public bool IsAlive => DateTime.Now <= Expires;

        public DateTime Expires { get; private set; }

        protected RefreshToken()
        {
        }
        public RefreshToken(TKey userId, long dayToExpire = 3)
        {
            UserId = userId;
            CreatedAt = DateTime.Now;
            Token = GenerateToken();
            Expires = DateTime.Now.AddDays(dayToExpire);
        }

        public bool IsValid()
        {
            return !Revoked && IsAlive;
        }
        
        private string GenerateToken(int size = 32)
        {
            var randomNumber = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public void Revoke()
        {
            if (Revoked)
            {
                throw new Exception($"Refresh token: '{Id}' was already revoked at '{RevokedAt}'.");
            }

            RevokedAt = DateTime.Now;
        }
    }
}