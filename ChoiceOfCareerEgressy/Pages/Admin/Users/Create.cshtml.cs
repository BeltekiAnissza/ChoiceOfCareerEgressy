using ChoiceOfCareerEgressy.Dto.UserDto.Admin;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly UserService _userService;

        public CreateModel(UserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public AdminUserCreateDto NewUser { get; set; } = new();

        public AdminUserOptionsDto Options { get; set; } = new();

        public async Task OnGetAsync()
        {
            Options = await _userService.GetUserOptionsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Options = await _userService.GetUserOptionsAsync();

            if (!ModelState.IsValid)
                return Page();

            try
            {
                int? adminUserId = GetAdminUserId();

                await _userService.CreateUserAsync(
                    NewUser,
                    adminUserId);

                TempData["SuccessMessage"] =
                    "Az új felhasználót sikeresen létrehoztuk.";

                return RedirectToPage("./Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    ex.Message);

                return Page();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "A felhasználó létrehozása adatbázishiba miatt nem sikerült.");

                return Page();
            }
        }

        private int? GetAdminUserId()
        {
            string? value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id
                : null;
        }
    }
}
