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
    public class EditModel : PageModel
    {
        private readonly UserService _userService;

        public EditModel(UserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public AdminUserEditDto EditedUser { get; set; } = new();

        public AdminUserOptionsDto Options { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userService.GetUserForEditAsync(id);

            if (user == null)
                return NotFound();

            EditedUser = user;
            Options = await _userService.GetUserOptionsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Options = await _userService.GetUserOptionsAsync();

            if (id != EditedUser.UserId)
                return BadRequest();

            if (!ModelState.IsValid)
                return Page();

            try
            {
                int? adminUserId = GetAdminUserId();

                await _userService.UpdateUserAsync(
                    EditedUser,
                    adminUserId);

                TempData["SuccessMessage"] =
                    "A felhasználó adatait sikeresen módosítottuk.";

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
                    "Az adatok mentése adatbázishiba miatt nem sikerült.");

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

