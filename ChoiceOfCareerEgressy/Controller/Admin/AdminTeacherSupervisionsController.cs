using ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Controller.Admin
{
    [ApiController]
    [Route("api/adminteachersupervisions")]
    [Authorize(Roles = "Admin")]
    public class AdminTeacherSupervisionsController : ControllerBase
    {
        private readonly AdminRegistrationTeacherService _service;
        private readonly ILogger<AdminTeacherSupervisionsController> _logger;

        public AdminTeacherSupervisionsController(AdminRegistrationTeacherService service, ILogger<AdminTeacherSupervisionsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =========================================
        // TANÁRI FELÜGYELETEK LEKÉRDEZÉSE
        //
        // GET: api/adminteachersupervisions
        //
        // Opcionális paraméter:
        // - eventId
        //
        // A Service visszaadja:
        // - Események listáját
        // - Tanárok listáját
        // - Tanárok aktuális felügyeleteit
        // - Az esemény összes idősávját
        // - Idősávonként a választható programokat
        // =========================================

        [HttpGet]
        public async Task<ActionResult<AdminTeacherSupervisionPageDto>> Get([FromQuery] int? eventId)
        {
            if (eventId.HasValue && eventId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen eseményazonosító."
                });
            }

            var result = await _service.GetPageAsync(eventId);

            return Ok(result);
        }

        // =========================================
        // TANÁRI FELÜGYELET MENTÉSE
        //
        // PUT: api/adminteachersupervisions
        //
        // Új felügyelet létrehozása vagy
        // meglévő felügyelet módosítása.
        //
        // A Service ellenőrzi:
        // - A tanár létezését
        // - A program létezését
        // - Az esemény és idősáv kapcsolatát
        // - Az egy idősávhoz tartozó felügyeleteket
        // - A program felügyelői kapacitását
        // - Az azonos program ismételt választását
        // =========================================

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromBody] AdminSetTeacherSupervisionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (dto.TeacherId <= 0 ||
                dto.EventId <= 0 ||
                dto.TimeSlotId <= 0 ||
                dto.PlanSessionId <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "A felügyelet adatai hiányosak vagy érvénytelenek."
                });
            }

            try
            {
                await _service.SetAsync(dto);

                return Ok(new
                {
                    Message = "Felügyelet sikeresen mentve."
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
                    "Adatbázishiba a tanári felügyelet mentésekor. " +
                    "TeacherId: {TeacherId}, EventId: {EventId}, " +
                    "TimeSlotId: {TimeSlotId}, PlanSessionId: {PlanSessionId}",
                    dto.TeacherId,
                    dto.EventId,
                    dto.TimeSlotId,
                    dto.PlanSessionId);

                return Conflict(new
                {
                    Message = "A mentés nem sikerült."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogWarning(
                    ex,
                    "SQLite-hiba a tanári felügyelet mentésekor.");

                return Conflict(new
                {
                    Message = "Az adatbázis foglalt."
                });
            }
        }

        // =========================================
        // TANÁRI FELÜGYELET TÖRLÉSE
        //
        // DELETE: api/adminteachersupervisions/5
        //
        // Kötelező query paraméterek:
        // - teacherId
        // - eventId
        //
        // A Service ellenőrzi:
        // - A felügyelet létezését
        // - A felügyelet tulajdonosát
        // - A felügyelet eseményhez tartozását
        //
        // A törlés az adatbázisból történik.
        // =========================================

        [HttpDelete("{supervisionId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int supervisionId, [FromQuery] int teacherId, [FromQuery] int eventId)
        {
            if (supervisionId <= 0 ||
                teacherId <= 0 ||
                eventId <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "A törléshez érvényes azonosítók szükségesek."
                });
            }

            try
            {
                await _service.DeleteAsync(
                    teacherId,
                    supervisionId,
                    eventId);

                return Ok(new
                {
                    Message = "Felügyelet sikeresen törölve."
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
                    "Adatbázishiba a tanári felügyelet törlésekor. " +
                    "SupervisionId: {SupervisionId}, TeacherId: {TeacherId}",
                    supervisionId,
                    teacherId);

                return Conflict(new
                {
                    Message = "A törlés nem sikerült."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogWarning(
                    ex,
                    "SQLite-hiba a tanári felügyelet törlésekor.");

                return Conflict(new
                {
                    Message = "Az adatbázis foglalt."
                });
            }
        }
    }
}