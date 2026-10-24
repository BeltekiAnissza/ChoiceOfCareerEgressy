using ChoiceOfCareerEgressy.Dto.UserDto.Teacher;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Teacher
{
    [Authorize(Roles = "Teacher")]
    public class IndexModel : PageModel
    {
        private readonly TeacherSupervisionService _service;

        public IndexModel(TeacherSupervisionService service)
        {
            _service = service;
        }

        public List<TeacherSupervisionEventDto> Events { get; set; }
            = new();

        public string? ErrorMessage { get; set; }

        public string? SuccessMessage { get; set; }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task OnGetAsync()
        {
            SuccessMessage = TempData["SuccessMessage"] as string;
            Events = await _service.GetEventsAsync(CurrentUserId);
        }

        public async Task<IActionResult> OnPostRegisterAsync(
            int planSessionId)
        {
            try
            {
                await _service.RegisterAsync(
                    CurrentUserId, planSessionId);

                TempData["SuccessMessage"] =
                    "Sikeresen jelentkeztél felügyelőtanárnak.";

                return RedirectToPage();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                ErrorMessage =
                    "A jelentkezés mentése sikertelen. Próbáld újra.";
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                ErrorMessage =
                    "Az adatbázis jelenleg foglalt. Próbáld újra.";
            }

            Events = await _service.GetEventsAsync(CurrentUserId);
            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(
            int planSessionId)
        {
            try
            {
                await _service.CancelAsync(
                    CurrentUserId, planSessionId);

                TempData["SuccessMessage"] =
                    "A felügyeleti jelentkezést lemondtad.";

                return RedirectToPage();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                ErrorMessage = "A lemondás mentése sikertelen.";
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                ErrorMessage = "Az adatbázis jelenleg foglalt.";
            }

            Events = await _service.GetEventsAsync(CurrentUserId);
            return Page();
        }
    }
}
