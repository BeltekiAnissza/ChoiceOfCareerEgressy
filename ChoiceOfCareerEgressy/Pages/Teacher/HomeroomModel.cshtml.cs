using ChoiceOfCareerEgressy.Dto.Homeroom;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Teacher
{
    public class HomeroomModel : PageModel
    {
        private readonly HomeroomService _service;

        public HomeroomModel(HomeroomService service)
        {
            _service = service;
        }

        public HomeroomPageDto Data { get; set; } = new();

        [BindProperty]
        public HomeroomChangeApplicationDto Change { get; set; }
            = new();

        public string? ErrorMessage { get; set; }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(
                ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> OnGetAsync(int? eventId)
        {
            if (!await _service.IsHomeroomTeacherAsync(CurrentUserId))
            {
                return Forbid();
            }

            Data = await _service.GetPageAsync(CurrentUserId, eventId);

            return Page();
        }

        public async Task<IActionResult> OnPostChangeAsync()
        {
            try
            {
                await _service.ChangeApplicationAsync(
                    CurrentUserId, Change);

                TempData["Success"] =
                    "A diák jelentkezését sikeresen mentetted.";

                return RedirectToPage(
                    new { eventId = Change.EventId });
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                ErrorMessage =
                    "A módosítás nem sikerült. Frissítsd az oldalt.";
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                ErrorMessage =
                    "Az adatbázis foglalt. Próbáld újra.";
            }

            Data = await _service.GetPageAsync(
                CurrentUserId, Change.EventId);

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            int studentId, int applicationId, int eventId)
        {
            try
            {
                await _service.DeleteApplicationAsync(
                    CurrentUserId,
                    studentId,
                    applicationId,
                    eventId);

                TempData["Success"] =
                    "A jelentkezést sikeresen törölted.";

                return RedirectToPage(new { eventId });
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                ErrorMessage = "A törlés nem sikerült.";
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                ErrorMessage =
                    "Az adatbázis foglalt. Próbáld újra.";
            }

            Data = await _service.GetPageAsync(
                CurrentUserId, eventId);

            return Page();
        }
    }
}

