using ChoiceOfCareerEgressy.Dto.Homeroom;
using ChoiceOfCareerEgressy.Model;
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

        // =====================================================
        // OLDAL ADATAI
        // =====================================================

        public HomeroomPageDto Data { get; set; } = new();

        [BindProperty]
        public HomeroomChangeApplicationDto Change { get; set; }
            = new();

        public string? ErrorMessage { get; set; }

        // =====================================================
        // SEGÉDMETÓDUSOK
        // =====================================================

        private bool TryGetCurrentUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] ==
                   "XMLHttpRequest";
        }

        private IActionResult ErrorResponse(string message)
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new
                {
                    success = false,
                    message
                });
            }

            ErrorMessage = message;

            return Page();
        }

        // =====================================================
        // OLDAL BETÖLTÉSE
        // =====================================================

        public async Task<IActionResult> OnGetAsync(int? eventId)
        {
            if (!TryGetCurrentUserId(out int teacherId))
            {
                return Challenge();
            }

            if (!await _service.IsHomeroomTeacherAsync(teacherId))
            {
                return Forbid();
            }

            try
            {
                Data = await _service.GetPageAsync(
                    teacherId,
                    eventId);

                return Page();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;

                Data = await _service.GetPageAsync(
                    teacherId,
                    null);

                return Page();
            }
        }

        // =====================================================
        // JELENTKEZÉS LÉTREHOZÁSA / MÓDOSÍTÁSA
        // =====================================================

        public async Task<IActionResult> OnPostChangeAsync()
        {
            if (!TryGetCurrentUserId(out int teacherId))
            {
                return Challenge();
            }

            if (!await _service.IsHomeroomTeacherAsync(teacherId))
            {
                return Forbid();
            }

            try
            {
                await _service.ChangeApplicationAsync(
                    teacherId,
                    Change);

                const string message =
                    "A diák jelentkezését sikeresen mentetted.";

                if (IsAjaxRequest())
                {
                    return await StudentSuccessResponseAsync(
                        teacherId,
                        Change.StudentId,
                        message);
                }

                TempData["Success"] = message;

                return RedirectToPage();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return await HandleErrorAsync(
                    teacherId,
                    ex.Message);
            }
            catch (DbUpdateException)
            {
                return await HandleErrorAsync(
                    teacherId,
                    "A módosítás nem sikerült. Frissítsd az oldalt.");
            }
            catch (SqliteException)
            {
                return await HandleErrorAsync(
                    teacherId,
                    "Az adatbázis foglalt. Próbáld újra.");
            }
        }

        // =====================================================
        // JELENTKEZÉS TÖRLÉSE
        // =====================================================

        public async Task<IActionResult> OnPostDeleteAsync(int studentId, int applicationId, int eventId)
        {
            if (!TryGetCurrentUserId(out int teacherId))
            {
                return Challenge();
            }

            if (!await _service.IsHomeroomTeacherAsync(teacherId))
            {
                return Forbid();
            }

            try
            {
                await _service.DeleteApplicationAsync(
                    teacherId,
                    studentId,
                    applicationId,
                    eventId);

                const string message =
                    "A jelentkezést sikeresen törölted.";

                if (IsAjaxRequest())
                {
                    return await StudentSuccessResponseAsync(
                        teacherId,
                        studentId,
                        message);
                }

                TempData["Success"] = message;

                return RedirectToPage();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return await HandleErrorAsync(
                    teacherId,
                    ex.Message);
            }
            catch (DbUpdateException)
            {
                return await HandleErrorAsync(
                    teacherId,
                    "A törlés nem sikerült. Frissítsd az oldalt.");
            }
            catch (SqliteException)
            {
                return await HandleErrorAsync(
                    teacherId,
                    "Az adatbázis foglalt. Próbáld újra.");
            }
        }

        // =====================================================
        // SIKERES AJAX-VÁLASZ
        // =====================================================

        private async Task<IActionResult> StudentSuccessResponseAsync(int teacherId, int studentId, string message)
        {
            var pageData = await _service.GetPageAsync(
                teacherId,
                null);

            var student = pageData.Grades
                .SelectMany(g => g.Students)
                .FirstOrDefault(s => s.UserId == studentId);

            if (student == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "A diák nem található."
                });
            }

            return new JsonResult(new
            {
                success = true,
                message,
                studentId = student.UserId,
                registrationCount = student.RegistrationCount,
                totalSlots = student.TimeSlots.Count,

                applications = student.Applications.Select(a => new
                {
                    applicationId = a.ApplicationId,
                    planSessionId = a.PlanSessionId,
                    planName = a.PlanName,
                    locationName = a.LocationName,
                    timeSlotName = a.TimeSlotName,
                    beginDate = a.BeginDate.ToString("HH:mm"),
                    endDate = a.EndDate.ToString("HH:mm")
                }),

                timeSlots = student.TimeSlots.Select(slot => new
                {
                    timeSlotId = slot.TimeSlotId,
                    timeSlotName = slot.TimeSlotName,
                    applicationId = slot.ApplicationId,
                    selectedPlanSessionId = slot.SelectedPlanSessionId,

                    programs = slot.Programs.Select(p => new
                    {
                        planSessionId = p.PlanSessionId,
                        planName = p.PlanName,
                        locationName = p.LocationName,
                        availablePlaces = p.AvailablePlaces
                    })
                })
            });
        }

        // =====================================================
        // HIBAKEZELÉS
        // =====================================================

        private async Task<IActionResult> HandleErrorAsync(int teacherId, string message)
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new
                {
                    success = false,
                    message
                });
            }

            ErrorMessage = message;

            return await ReloadPageAsync(teacherId);
        }

        // =====================================================
        // OLDAL ÚJRATÖLTÉSE HIBA ESETÉN
        // =====================================================

        private async Task<IActionResult> ReloadPageAsync(int teacherId)
        {
            try
            {
                Data = await _service.GetPageAsync(
                    teacherId,
                    null);

                return Page();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }
    }
}