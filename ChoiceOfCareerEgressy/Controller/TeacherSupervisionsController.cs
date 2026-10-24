using ChoiceOfCareerEgressy.Dto.UserDto.Teacher;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TeacherSupervisionsController : ControllerBase
    {
        private readonly TeacherSupervisionService _service;

        public TeacherSupervisionsController(
            TeacherSupervisionService service)
        {
            _service = service;
        }

        private bool TryGetCurrentUserId(out int userId) =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

        [HttpGet("events")]
        public async Task<ActionResult<List<TeacherSupervisionEventDto>>>
            GetEvents()
        {
            if (!TryGetCurrentUserId(out int currentUserId))
                return Unauthorized(new { Message = "Érvénytelen bejelentkezési azonosító." });
            return Ok(await _service.GetEventsAsync(currentUserId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            [FromBody] TeacherSupervisionRequestDto dto)
        {
            if (!TryGetCurrentUserId(out int currentUserId))
                return Unauthorized(new { Message = "Érvénytelen bejelentkezési azonosító." });
            try
            {
                await _service.RegisterAsync(
                    currentUserId, dto.PlanSessionId);

                return Ok(new
                {
                    Message = "Sikeres felügyeleti jelentkezés."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new
                {
                    Message = "A jelentkezés mentése sikertelen."
                });
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                return Conflict(new
                {
                    Message = "Az adatbázis foglalt. Próbáld újra."
                });
            }
        }

        [HttpDelete("{planSessionId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int planSessionId)
        {
            if (!TryGetCurrentUserId(out int currentUserId))
                return Unauthorized(new { Message = "Érvénytelen bejelentkezési azonosító." });
            try
            {
                await _service.CancelAsync(
                    currentUserId, planSessionId);

                return Ok(new
                {
                    Message = "A felügyeleti jelentkezés törölve."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new
                {
                    Message = "A lemondás mentése sikertelen."
                });
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                return Conflict(new
                {
                    Message = "Az adatbázis foglalt. Próbáld újra."
                });
            }
        }
    }
}

