using ChoiceOfCareerEgressy.Dto.UserDto.Student;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Student
{
    [Authorize(Roles = "Student")]
    public class IndexModel : PageModel
    {
        private readonly StudentRegistrationService _service;

        public IndexModel(StudentRegistrationService service)
        {
            _service = service;
        }

        public List<StudentEventDto> Events { get; set; } = new();

        [BindProperty]
        public StudentRegistrationDto Registration { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(
                ClaimTypes.NameIdentifier)!);

        public async Task OnGetAsync()
        {
            SuccessMessage = TempData["SuccessMessage"] as string;
            Events = await _service.GetEventsAsync(CurrentUserId);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return await SaveAsync(false);
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            return await SaveAsync(true);
        }

        private async Task<IActionResult> SaveAsync(bool isUpdate)
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Hiányzó vagy hibás jelentkezési adatok.";
            }
            else
            {
                try
                {
                    if (isUpdate)
                    {
                        await _service.UpdateRegistrationAsync(
                            CurrentUserId,
                            Registration);
                    }
                    else
                    {
                        await _service.RegisterAsync(
                            CurrentUserId,
                            Registration);
                    }

                    TempData["SuccessMessage"] = isUpdate
                        ? "Sikeresen módosítottad a jelentkezéseidet!"
                        : "Sikeres jelentkezés! Mindkét programot rögzítettük.";

                    return RedirectToPage();
                }
                catch (InvalidOperationException ex)
                {
                    ErrorMessage = ex.Message;
                }
                catch (DbUpdateException)
                {
                    ErrorMessage =
                        "A mentés sikertelen. Frissítsd az oldalt, " +
                        "és próbáld újra.";
                }
                catch (SqliteException)
                {
                    ErrorMessage =
                        "Az adatbázis jelenleg foglalt. " +
                        "Próbáld újra később.";
                }
            }

            Events = await _service.GetEventsAsync(CurrentUserId);
            return Page();
        }
    }
}