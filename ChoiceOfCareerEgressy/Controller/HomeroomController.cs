using ChoiceOfCareerEgressy.Dto.Homeroom;
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
    public class HomeroomController : ControllerBase
    {
        private readonly HomeroomService _service;

        public HomeroomController(HomeroomService service)
        {
            _service = service;
        }

        private bool TryGetCurrentUserId(out int userId) =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

        [HttpGet]
        public async Task<ActionResult<HomeroomPageDto>> Get(
            [FromQuery] int? eventId)
        {
            if (!TryGetCurrentUserId(out int currentUserId))
                return Unauthorized(new { Message = "Érvénytelen bejelentkezési azonosító." });
            try
            {
                if (!await _service.IsHomeroomTeacherAsync(currentUserId))
                {
                    return Forbid();
                }
                return Ok(await _service.GetPageAsync(currentUserId, eventId));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPut("applications")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeApplication(
            [FromBody] HomeroomChangeApplicationDto dto)
        {
            if (!TryGetCurrentUserId(out int currentUserId))
                return Unauthorized(new { Message = "Érvénytelen bejelentkezési azonosító." });
            try
            {
                await _service.ChangeApplicationAsync(
                    currentUserId, dto);

                return Ok(new
                {
                    Message = "A jelentkezés sikeresen módosítva."
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new
                {
                    Message = "A módosítás nem sikerült."
                });
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                return Conflict(new
                {
                    Message = "Az adatbázis foglalt."
                });
            }
        }

        [HttpDelete("applications/{applicationId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApplication(
            int applicationId,
            [FromQuery] int studentId,
            [FromQuery] int eventId)
        {
            if (!TryGetCurrentUserId(out int currentUserId))
                return Unauthorized(new { Message = "Érvénytelen bejelentkezési azonosító." });
            try
            {
                await _service.DeleteApplicationAsync(
                    currentUserId,
                    studentId,
                    applicationId,
                    eventId);

                return Ok(new
                {
                    Message = "A jelentkezés sikeresen törölve."
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new
                {
                    Message = "A törlés nem sikerült."
                });
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                return Conflict(new
                {
                    Message = "Az adatbázis foglalt."
                });
            }
        }
    }
}
