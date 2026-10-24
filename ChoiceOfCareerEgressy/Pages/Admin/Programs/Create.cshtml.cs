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
    public class CreateModel : PageModel
    {
        private readonly AdminProgramService _service;

        public CreateModel(AdminProgramService service)
        {
            _service = service;
        }

        [BindProperty]
        public AdminProgramDto Program { get; set; } = new();

        public AdminProgramOptionsDto Options { get; set; } = new();

        public async Task OnGetAsync()
        {
            Options = await _service.GetOptionsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Options = await _service.GetOptionsAsync();

            if (!ModelState.IsValid)
                return Page();

            if (!Program.LocationId.HasValue &&
                string.IsNullOrWhiteSpace(Program.NewLocationName))
            {
                ModelState.AddModelError(
                    "Program.LocationId",
                    "Válassz egy meglévő termet, vagy adj meg új teremnevet.");

                return Page();
            }

            try
            {
                int? adminUserId = GetAdminUserId();

                await _service.CreateProgramAsync(
                    Program,
                    adminUserId);

                TempData["SuccessMessage"] =
                    "Az új programot sikeresen létrehoztuk.";

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
                    "A program létrehozása adatbázishiba miatt nem sikerült.");

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
