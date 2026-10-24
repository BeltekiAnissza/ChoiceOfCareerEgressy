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

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(
                ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<ActionResult<HomeroomPageDto>> Get(
            [FromQuery] int? eventId)
        {
            try
            {
                if (!await _service.IsHomeroomTeacherAsync(CurrentUserId))
                {
                    return Forbid();
                }
                return Ok(await _service.GetPageAsync(CurrentUserId, eventId));
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
            try
            {
                await _service.ChangeApplicationAsync(
                    CurrentUserId, dto);

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
            try
            {
                await _service.DeleteApplicationAsync(
                    CurrentUserId,
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
