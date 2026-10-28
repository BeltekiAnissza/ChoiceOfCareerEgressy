using ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Controller.Admin
{
    [ApiController]
    [Route("api/adminregistrations")]
    [Authorize(Roles = "Admin")]
    public class AdminRegistrationsController : ControllerBase
    {
        private readonly AdminRegistrationStudentService _service;
        private readonly ILogger<AdminRegistrationsController> _logger;

        public AdminRegistrationsController(AdminRegistrationStudentService service, ILogger<AdminRegistrationsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =========================================
        // ADMINISZTRÁTORI JELENTKEZÉSEK LEKÉRDEZÉSE
        //
        // GET: api/adminregistrations
        //
        // Opcionális szűrők:
        // - eventId
        // - gradeId
        // - planSessionId
        //
        // Az esemény idősávjait a Service
        // dinamikusan tölti be.
        // =========================================

        [HttpGet]
        public async Task<ActionResult<AdminRegistrationPageDto>> Get([FromQuery] int? eventId, [FromQuery] int? gradeId, [FromQuery] int? planSessionId)
        {
            if (eventId.HasValue && eventId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen eseményazonosító."
                });
            }

            if (gradeId.HasValue && gradeId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen osztályazonosító."
                });
            }

            if (planSessionId.HasValue && planSessionId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen programazonosító."
                });
            }

            var result = await _service.GetPageAsync(
                eventId,
                gradeId,
                planSessionId);

            return Ok(result);
        }

        // =========================================
        // DIÁK JELENTKEZÉSÉNEK MÓDOSÍTÁSA
        //
        // PUT: api/adminregistrations
        //
        // A Service ellenőrzi:
        // - Létezik-e a diák
        // - Létezik-e a program
        // - A program a megadott eseményhez tartozik-e
        // - A program a megadott idősávhoz tartozik-e
        // - Megfelel-e a diák az ágazati feltételeknek
        // - Megfelel-e a diák a nem szerinti feltételeknek
        // - Van-e szabad hely
        // - Nem választotta-e már ugyanazt a programot
        // =========================================

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Change([FromBody] AdminChangeApplicationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (dto.StudentId <= 0 ||
                dto.EventId <= 0 ||
                dto.TimeSlotId <= 0 ||
                dto.PlanSessionId <= 0)
            {
                return BadRequest(new
                {
                    Message = "A jelentkezés adatai hiányosak vagy érvénytelenek."
                });
            }

            try
            {
                await _service.ChangeAsync(dto);

                return Ok(new
                {
                    Message = "A jelentkezés sikeresen módosítva."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Adatbázishiba a jelentkezés módosításakor. " +
                    "StudentId: {StudentId}, EventId: {EventId}",
                    dto.StudentId,
                    dto.EventId);

                return Conflict(new
                {
                    Message = "A módosítás nem sikerült."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogWarning(
                    ex,
                    "SQLite-hiba a jelentkezés módosításakor.");

                return Conflict(new
                {
                    Message = "Adatbázishiba történt. Próbáld újra."
                });
            }
        }

        // =========================================
        // DIÁK JELENTKEZÉSÉNEK TÖRLÉSE
        //
        // DELETE: api/adminregistrations/5
        //
        // Kötelező query paraméterek:
        // - studentId
        // - eventId
        //
        // A Service ellenőrzi, hogy:
        // - A jelentkezés létezik-e
        // - Az adott diákhoz tartozik-e
        // - Az adott eseményhez tartozik-e
        // - A jelentkezés aktív-e
        //
        // A törlés státuszmódosítással történik.
        // =========================================

        [HttpDelete("{applicationId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int applicationId, [FromQuery] int studentId, [FromQuery] int eventId)
        {
            if (applicationId <= 0 ||
                studentId <= 0 ||
                eventId <= 0)
            {
                return BadRequest(new
                {
                    Message = "A törléshez érvényes azonosítók szükségesek."
                });
            }

            try
            {
                await _service.DeleteAsync(
                    studentId,
                    applicationId,
                    eventId);

                return Ok(new
                {
                    Message = "A jelentkezés sikeresen törölve."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Adatbázishiba a jelentkezés törlésekor. " +
                    "ApplicationId: {ApplicationId}",
                    applicationId);

                return Conflict(new
                {
                    Message = "A törlés nem sikerült."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogWarning(
                    ex,
                    "SQLite-hiba a jelentkezés törlésekor.");

                return Conflict(new
                {
                    Message = "Adatbázishiba történt. Próbáld újra."
                });
            }
        }
    }
}