using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model
{
    public class AuthService
    {
        private readonly ChoiceOfCareerPersistence _context;
        private readonly PasswordService _passwordService;

        public AuthService(
            ChoiceOfCareerPersistence context,
            PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<LoggedInUserDto?> LoginAsync(LoginDto login)
        {
            var email = login.EmailAddress.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailAddress.ToLower() == email);

            if (user == null)
                return null;

            var result = _passwordService.VerifyPassword(
                user,
                login.Password
            );

            if (result == PasswordVerificationResult.Failed)
                return null;

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordService.HashPassword(
                    user,
                    login.Password
                );

                await _context.SaveChangesAsync();
            }

            return new LoggedInUserDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                EmailAddress = user.EmailAddress,
                Role = user.Role.ToString(),
                MustChangePassword = user.MustChangePassword
            };
        }
    }
}

