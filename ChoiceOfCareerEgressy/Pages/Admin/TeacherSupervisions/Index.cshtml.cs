using ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Pages.Admin.TeacherSupervisions
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AdminTeacherSupervisionService _service;

        public IndexModel(AdminTeacherSupervisionService service)
        {
            _service = service;
        }

        public AdminTeacherSupervisionPageDto Data { get; set; } = new();

        [BindProperty]
        public AdminSetTeacherSupervisionDto Change { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(int? eventId)
        {
            Data = await _service.GetPageAsync(eventId);
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            try
            {
                await _service.SetAsync(Change);

                TempData["Success"] =
                    "A tanár felügyeleti jelentkezését sikeresen mentetted.";

                return RedirectToPage(new { eventId = Change.EventId });
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "A mentés nem sikerült.";
            }
            catch (SqliteException)
            {
                ErrorMessage = "Az adatbázis foglalt. Próbáld újra.";
            }

            Data = await _service.GetPageAsync(Change.EventId);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            int teacherId, int supervisionId, int eventId)
        {
            try
            {
                await _service.DeleteAsync(
                    teacherId, supervisionId, eventId);

                TempData["Success"] =
                    "A felügyeleti jelentkezést törölted.";

                return RedirectToPage(new { eventId });
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "A törlés nem sikerült.";
            }
            catch (SqliteException)
            {
                ErrorMessage = "Az adatbázis foglalt. Próbáld újra.";
            }

            Data = await _service.GetPageAsync(eventId);
            return Page();
        }
    }
}
