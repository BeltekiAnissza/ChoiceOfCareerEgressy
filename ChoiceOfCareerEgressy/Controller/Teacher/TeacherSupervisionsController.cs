using ChoiceOfCareerEgressy.Dto.UserDto.Teacher;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller.Teacher
{
    [Route("api/teachersupervisions")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TeacherSupervisionsController : ControllerBase
    {
        private readonly TeacherSupervisionService _service;
        private readonly ILogger<TeacherSupervisionsController> _logger;

        public TeacherSupervisionsController(TeacherSupervisionService service, ILogger<TeacherSupervisionsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // =========================================
        // ESEMÉNYEK ÉS FELÜGYELETEK LEKÉRDEZÉSE
        // =========================================

        [HttpGet("events")]
        public async Task<ActionResult<List<TeacherSupervisionEventDto>>> GetEvents()
        {
            var events = await _service.GetEventsAsync(CurrentUserId);

            return Ok(events);
        }

        // =========================================
        // FELÜGYELET VÁLLALÁSA
        // =========================================

        // =========================================
        // FELÜGYELET VÁLLALÁSA
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromBody] TeacherSupervisionRequestDto dto)
        {
            try
            {
                await _service.RegisterAsync(
                    CurrentUserId,
                    dto.PlanSessionId);

                return Ok(new
                {
                    Message = "Sikeresen jelentkeztél felügyelőtanárnak."
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
                _logger.LogError(
                    ex,
                    "Hiba a tanári felügyeleti jelentkezés mentésekor. " +
                    "UserId: {UserId}, PlanSessionId: {PlanSessionId}",
                    CurrentUserId,
                    dto.PlanSessionId);

                return Conflict(new
                {
                    Message = "A jelentkezés mentése sikertelen. Próbáld újra."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba a tanári felügyeleti jelentkezés során. " +
                    "UserId: {UserId}, PlanSessionId: {PlanSessionId}",
                    CurrentUserId,
                    dto.PlanSessionId);

                return Conflict(new
                {
                    Message = "Az adatbázis jelenleg foglalt. Próbáld újra."
                });
            }
        }

        // =========================================
        // FELÜGYELET VISSZAVONÁSA
        // =========================================

        [HttpDelete("{planSessionId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int planSessionId)
        {
            try
            {
                await _service.CancelAsync(
                    CurrentUserId,
                    planSessionId);

                return Ok(new
                {
                    Message = "A felügyeleti jelentkezést lemondtad."
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
                _logger.LogError(
                    ex,
                    "Hiba a tanári felügyelet visszavonásakor. " +
                    "UserId: {UserId}, PlanSessionId: {PlanSessionId}",
                    CurrentUserId,
                    planSessionId);

                return Conflict(new
                {
                    Message = "A lemondás mentése sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba a tanári felügyelet visszavonásakor. " +
                    "UserId: {UserId}, PlanSessionId: {PlanSessionId}",
                    CurrentUserId,
                    planSessionId);

                return Conflict(new
                {
                    Message = "Az adatbázis jelenleg foglalt."
                });
            }
        }
    }
}