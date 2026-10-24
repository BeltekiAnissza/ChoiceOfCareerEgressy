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
    public class DeleteModel : PageModel
    {
        private readonly UserService _userService;

        public DeleteModel(UserService userService)
        {
            _userService = userService;
        }

        public AdminUserDeleteDto DeletedUser { get; set; } = new();

        [BindProperty]
        public bool ConfirmDelete { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userService.GetUserDeleteInfoAsync(id);

            if (user == null)
                return NotFound();

            DeletedUser = user;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var user = await _userService.GetUserDeleteInfoAsync(id);

            if (user == null)
                return NotFound();

            DeletedUser = user;

            if (!ConfirmDelete)
            {
                ModelState.AddModelError(
                    nameof(ConfirmDelete),
                    "A törlést meg kell erősítened.");

                return Page();
            }

            try
            {
                int? adminUserId = GetAdminUserId();

                if (!adminUserId.HasValue)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Nem sikerült azonosítani az adminisztrátort.");

                    return Page();
                }

                await _userService.DeleteUserAsync(
                    id,
                    adminUserId);

                TempData["SuccessMessage"] =
                    "A felhasználót sikeresen töröltük.";

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
                    "A felhasználó törlése adatbázishiba miatt nem sikerült.");

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
