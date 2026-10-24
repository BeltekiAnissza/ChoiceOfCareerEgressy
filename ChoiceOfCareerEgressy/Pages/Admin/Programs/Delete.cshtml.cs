using ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms;
using ChoiceOfCareerEgressy.Model.Admin.Programs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Admin.Programs
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly AdminProgramService _service;

        public DeleteModel(AdminProgramService service)
        {
            _service = service;
        }

        public AdminProgramDeleteDto Program { get; set; } = new();

        [BindProperty]
        public bool ConfirmDelete { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var program = await _service.GetDeleteInfoAsync(id);

            if (program == null)
                return NotFound();

            Program = program;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var program = await _service.GetDeleteInfoAsync(id);

            if (program == null)
                return NotFound();

            Program = program;

            if (!ConfirmDelete)
            {
                ModelState.AddModelError(
                    nameof(ConfirmDelete),
                    "A törléshez meg kell erősítened a műveletet.");

                return Page();
            }

            try
            {
                int? adminUserId = GetAdminUserId();

                await _service.DeleteProgramAsync(
                    id,
                    adminUserId);

                TempData["SuccessMessage"] =
                    $"A(z) „{program.PlanName}” programot " +
                    "sikeresen töröltük.";

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
                    "A törlés adatbázishiba miatt nem sikerült.");

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
