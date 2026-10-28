using ChoiceOfCareerEgressy.Dto.Homeroom;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller.Teacher
{
    [ApiController]
    [Route("api/homeroom")]
    [Authorize(Roles = "Teacher,Admin")]
    public class HomeroomController : ControllerBase
    {
        private readonly HomeroomService _service;
        private readonly ILogger<HomeroomController> _logger;

        public HomeroomController(HomeroomService service, ILogger<HomeroomController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =========================================
        // BEJELENTKEZETT FELHASZNÁLÓ AZONOSÍTÓJA
        // =========================================

        private bool TryGetCurrentUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }

        // =========================================
        // OSZTÁLYFŐNÖKI JOGOSULTSÁG ELLENŐRZÉSE
        // =========================================

        private async Task<bool> IsHomeroomTeacherAsync(int teacherId)
        {
            return await _service.IsHomeroomTeacherAsync(teacherId);
        }

        // =========================================
        // OLDAL ADATAINAK LEKÉRÉSE
        //
        // GET: api/homeroom?eventId=1
        // =========================================

        [HttpGet]
        public async Task<ActionResult<HomeroomPageDto>> Get([FromQuery] int? eventId)
        {
            if (!TryGetCurrentUserId(out int teacherId))
            {
                return Challenge();
            }

            if (!await IsHomeroomTeacherAsync(teacherId))
            {
                return Forbid();
            }

            try
            {
                var data = await _service.GetPageAsync(
                    teacherId,
                    eventId);

                return Ok(data);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // =========================================
        // JELENTKEZÉS LÉTREHOZÁSA / MÓDOSÍTÁSA
        //
        // PUT: api/homeroom/applications
        // =========================================

        [HttpPut("applications")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeApplication([FromBody] HomeroomChangeApplicationDto dto)
        {
            if (!TryGetCurrentUserId(out int teacherId))
            {
                return Challenge();
            }

            if (!await IsHomeroomTeacherAsync(teacherId))
            {
                return Forbid();
            }

            try
            {
                await _service.ChangeApplicationAsync(
                    teacherId,
                    dto);

                return await StudentSuccessResponseAsync(
                    teacherId,
                    dto.StudentId,
                    "A diák jelentkezését sikeresen mentetted.");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Hiba a diák jelentkezésének módosításakor. " +
                    "TeacherId: {TeacherId}, StudentId: {StudentId}",
                    teacherId,
                    dto.StudentId);

                return Conflict(new
                {
                    success = false,
                    message = "A módosítás nem sikerült. Frissítsd az oldalt."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba a diák jelentkezésének módosításakor. " +
                    "TeacherId: {TeacherId}, StudentId: {StudentId}",
                    teacherId,
                    dto.StudentId);

                return Conflict(new
                {
                    success = false,
                    message = "Az adatbázis foglalt. Próbáld újra."
                });
            }
        }

        // =========================================
        // JELENTKEZÉS TÖRLÉSE
        //
        // DELETE:
        // api/homeroom/applications/5?studentId=2&eventId=1
        // =========================================

        [HttpDelete("applications/{applicationId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApplication(int applicationId, [FromQuery] int studentId, [FromQuery] int eventId)
        {
            if (!TryGetCurrentUserId(out int teacherId))
            {
                return Challenge();
            }

            if (!await IsHomeroomTeacherAsync(teacherId))
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

                return await StudentSuccessResponseAsync(
                    teacherId,
                    studentId,
                    "A jelentkezést sikeresen törölted.");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Hiba a diák jelentkezésének törlésekor. " +
                    "TeacherId: {TeacherId}, StudentId: {StudentId}, " +
                    "ApplicationId: {ApplicationId}",
                    teacherId,
                    studentId,
                    applicationId);

                return Conflict(new
                {
                    success = false,
                    message = "A törlés nem sikerült. Frissítsd az oldalt."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba a diák jelentkezésének törlésekor. " +
                    "TeacherId: {TeacherId}, StudentId: {StudentId}, " +
                    "ApplicationId: {ApplicationId}",
                    teacherId,
                    studentId,
                    applicationId);

                return Conflict(new
                {
                    success = false,
                    message = "Az adatbázis foglalt. Próbáld újra."
                });
            }
        }

        // =========================================
        // SIKERES MŰVELET UTÁNI FRISSÍTETT DIÁKADATOK
        // =========================================

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

            return Ok(new
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
    }
}