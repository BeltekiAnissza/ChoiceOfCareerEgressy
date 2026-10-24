using ChoiceOfCareerEgressy.Dto.Homeroom;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Teacher
{
    [Authorize(Roles = "Teacher, Admin")]
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

        private bool TryGetCurrentUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }

        public async Task<IActionResult> OnGetAsync(int? eventId)
        {
            if (!TryGetCurrentUserId(out int teacherId))
                return Challenge();

            if (!await _service.IsHomeroomTeacherAsync(teacherId))
                return Forbid();

            try
            {
                Data = await _service.GetPageAsync(
                    teacherId, eventId);

                return Page();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        public async Task<IActionResult> OnPostChangeAsync()
        {
            if (!TryGetCurrentUserId(out int teacherId))
                return Challenge();

            if (!await _service.IsHomeroomTeacherAsync(teacherId))
                return Forbid();

            try
            {
                await _service.ChangeApplicationAsync(
                    teacherId, Change);

                TempData["Success"] =
                    "A diák jelentkezését sikeresen mentetted.";

                return RedirectToPage(
                    new { eventId = Change.EventId });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (DbUpdateException)
            {
                ErrorMessage =
                    "A módosítás nem sikerült. Frissítsd az oldalt.";
            }
            catch (SqliteException)
            {
                ErrorMessage =
                    "Az adatbázis foglalt. Próbáld újra.";
            }

            return await ReloadPageAsync(
                teacherId, Change.EventId);
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            int studentId, int applicationId, int eventId)
        {
            if (!TryGetCurrentUserId(out int teacherId))
                return Challenge();

            if (!await _service.IsHomeroomTeacherAsync(teacherId))
                return Forbid();

            try
            {
                await _service.DeleteApplicationAsync(
                    teacherId,
                    studentId,
                    applicationId,
                    eventId);

                TempData["Success"] =
                    "A jelentkezést sikeresen törölted.";

                return RedirectToPage(new { eventId });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
                ErrorMessage =
                    "Az adatbázis foglalt. Próbáld újra.";
            }

            return await ReloadPageAsync(teacherId, eventId);
        }

        private async Task<IActionResult> ReloadPageAsync(
            int teacherId, int? eventId)
        {
            try
            {
                Data = await _service.GetPageAsync(
                    teacherId, eventId);

                return Page();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}


