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

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("events")]
        public async Task<ActionResult<List<TeacherSupervisionEventDto>>>
            GetEvents()
        {
            return Ok(await _service.GetEventsAsync(CurrentUserId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            [FromBody] TeacherSupervisionRequestDto dto)
        {
            try
            {
                await _service.RegisterAsync(
                    CurrentUserId, dto.PlanSessionId);

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
            try
            {
                await _service.CancelAsync(
                    CurrentUserId, planSessionId);

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

