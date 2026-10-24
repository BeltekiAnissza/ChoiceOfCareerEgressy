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
    public class EditModel : PageModel
    {
        private readonly AdminProgramService _service;

        public EditModel(AdminProgramService service)
        {
            _service = service;
        }

        [BindProperty]
        public AdminProgramDto Program { get; set; } = new();

        public AdminProgramOptionsDto Options { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var program = await _service.GetProgramAsync(id);

            if (program == null)
                return NotFound();

            Program = program;
            Options = await _service.GetOptionsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Options = await _service.GetOptionsAsync();

            if (id != Program.PlanSessionId)
                return BadRequest();

            if (!ModelState.IsValid)
                return Page();

            try
            {
                int? adminUserId = GetAdminUserId();

                await _service.UpdateProgramAsync(
                    Program,
                    adminUserId);

                TempData["SuccessMessage"] =
                    "A program módosítása sikeres volt.";

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
                    "A program módosítása adatbázishiba miatt nem sikerült.");

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
