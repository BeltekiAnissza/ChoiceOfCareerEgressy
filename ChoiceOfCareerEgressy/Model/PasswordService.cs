using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Identity;

namespace ChoiceOfCareerEgressy.Model
{
    public class PasswordService
    {

        private readonly PasswordHasher<User> _passwordHasher = new();

        public string HashPassword(User user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
        }

        public PasswordVerificationResult VerifyPassword(User user, string password)
        {
            return _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password
            );
        }

    }
}
