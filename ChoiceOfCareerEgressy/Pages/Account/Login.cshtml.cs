using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;

        public LoginModel(AuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public LoginDto Login { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _authService.LoginAsync(Login);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Hibás email-cím vagy jelszó."
                );

                return Page();
            }

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.EmailAddress),
            new Claim(ClaimTypes.Role, user.Role)
        };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

           
            return user.Role switch
            {
                "Admin" => RedirectToPage("/Admin/Users/Index"),
                "Teacher" => RedirectToPage("/Teacher/Index"),
                "Student" => RedirectToPage("/Student/Index"),
                _ => RedirectToPage("/Index")
            };

        }
    }
}
