using ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Pages.Admin.Registrations.Teachers
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AdminRegistrationTeacherService _service;

        public IndexModel(AdminRegistrationTeacherService service)
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

                if (IsAjaxRequest())
                {
                    return new JsonResult(new
                    {
                        success = true,
                        message = "A tanár felügyeleti jelentkezését sikeresen mentetted."
                    });
                }

                TempData["Success"] =
                    "A tanár felügyeleti jelentkezését sikeresen mentetted.";

                return RedirectToPage(new { eventId = Change.EventId });
            }
            catch (InvalidOperationException ex)
            {
                return await HandleErrorAsync(ex.Message, Change.EventId);
            }
            catch (DbUpdateException)
            {
                return await HandleErrorAsync(
                    "A mentés nem sikerült.",
                    Change.EventId);
            }
            catch (SqliteException)
            {
                return await HandleErrorAsync(
                    "Az adatbázis foglalt. Próbáld újra.",
                    Change.EventId);
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            int teacherId,
            int supervisionId,
            int eventId)
        {
            try
            {
                await _service.DeleteAsync(
                    teacherId,
                    supervisionId,
                    eventId);

                if (IsAjaxRequest())
                {
                    return new JsonResult(new
                    {
                        success = true,
                        message = "A felügyeleti jelentkezést törölted."
                    });
                }

                TempData["Success"] =
                    "A felügyeleti jelentkezést törölted.";

                return RedirectToPage(new { eventId });
            }
            catch (InvalidOperationException ex)
            {
                return await HandleErrorAsync(ex.Message, eventId);
            }
            catch (DbUpdateException)
            {
                return await HandleErrorAsync(
                    "A törlés nem sikerült.",
                    eventId);
            }
            catch (SqliteException)
            {
                return await HandleErrorAsync(
                    "Az adatbázis foglalt. Próbáld újra.",
                    eventId);
            }
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        private async Task<IActionResult> HandleErrorAsync(
            string message,
            int eventId)
        {
            if (IsAjaxRequest())
            {
                return new JsonResult(new
                {
                    success = false,
                    message
                })
                {
                    StatusCode = 400
                };
            }

            ErrorMessage = message;
            Data = await _service.GetPageAsync(eventId);

            return Page();
        }
    }
}