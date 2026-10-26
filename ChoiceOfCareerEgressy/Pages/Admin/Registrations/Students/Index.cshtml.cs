using ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChoiceOfCareerEgressy.Pages.Admin.Registrations.Students
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AdminRegistrationStudentService _service;

        public IndexModel(AdminRegistrationStudentService service)
        {
            _service = service;
        }

        public AdminRegistrationPageDto Data { get; set; } = new();

        [BindProperty]
        public AdminChangeApplicationDto Change { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? OpenStudentId { get; set; }

        public string ViewMode { get; set; } = "grades";

        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(int? eventId, int? gradeId, int? planSessionId, string? viewMode, int? studentId)
        {
            ViewMode = viewMode == "programs" ? "programs" : "grades";

            OpenStudentId = studentId;

            Data = await _service.GetPageAsync(
                eventId,
                gradeId,
                planSessionId);
        }

        public async Task<IActionResult> OnPostChangeAsync(int? gradeId, int? planSessionId, string? viewMode, int? studentId)
        {
            ViewMode = viewMode == "programs" ? "programs" : "grades";

            OpenStudentId = studentId ?? Change.StudentId;

            try
            {
                await _service.ChangeAsync(Change);

                TempData["Success"] = "A jelentkezést sikeresen mentetted.";

                return RedirectToPage(new
                {
                    eventId = Change.EventId,
                    gradeId,
                    planSessionId,
                    viewMode = ViewMode,
                    studentId = ViewMode == "grades" ? OpenStudentId : null
                });
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                ErrorMessage = "A mentés nem sikerült. Frissítsd az oldalt.";
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                ErrorMessage = "Az adatbázis foglalt. Próbáld újra.";
            }

            Data = await _service.GetPageAsync(
                Change.EventId,
                gradeId,
                planSessionId);

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int studentId, int applicationId, int eventId, int? gradeId, int? planSessionId, string? viewMode)
        {
            ViewMode = viewMode == "programs" ? "programs" : "grades";

            OpenStudentId = studentId;

            try
            {
                await _service.DeleteAsync(
                    studentId,
                    applicationId,
                    eventId);

                TempData["Success"] = "A jelentkezést törölted.";

                return RedirectToPage(new
                {
                    eventId,
                    gradeId,
                    planSessionId,
                    viewMode = ViewMode,
                    studentId = ViewMode == "grades" ? studentId : (int?)null
                });
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                ErrorMessage = "A törlés nem sikerült.";
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                ErrorMessage = "Az adatbázis foglalt. Próbáld újra.";
            }

            Data = await _service.GetPageAsync(
                eventId,
                gradeId,
                planSessionId);

            return Page();
        }
    }
}