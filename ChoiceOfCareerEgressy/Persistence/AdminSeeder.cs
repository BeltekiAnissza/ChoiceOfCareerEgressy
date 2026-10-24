using ChoiceOfCareerEgressy.Model;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Persistence
{
    public class AdminSeeder
    {
        public static async Task SeedAsync(
       ChoiceOfCareerPersistence context,
       PasswordService passwordService,
       IConfiguration configuration)
        {
            // Ha már létezik admin, nem hozunk létre újat
            bool adminExists = await context.Users
                .AnyAsync(u => u.Role == RoleChoice.Admin);

            if (adminExists)
                return;

            var email = configuration["InitialAdmin:Email"];
            var password = configuration["InitialAdmin:Password"];

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Az első admin email-címe vagy jelszava nincs beállítva."
                );
            }

            var admin = new User
            {
                UserName = "admin",
                EmailAddress = email.Trim().ToLowerInvariant(),
                PasswordHash = "",
                Role = RoleChoice.Admin,
                MustChangePassword = false
            };

            // Jelszó hash-elése
            admin.PasswordHash = passwordService.HashPassword(
                admin,
                password
            );

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
