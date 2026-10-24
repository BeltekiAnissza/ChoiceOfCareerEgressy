using ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChoiceOfCareerEgressy.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminTeacherSupervisionsController : ControllerBase
    {
        private readonly AdminTeacherSupervisionService _service;

        public AdminTeacherSupervisionsController(
            AdminTeacherSupervisionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<AdminTeacherSupervisionPageDto>> Get(
            [FromQuery] int? eventId)
        {
            return Ok(await _service.GetPageAsync(eventId));
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            [FromBody] AdminSetTeacherSupervisionDto dto)
        {
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
                return BadRequest(new { Message = ex.Message });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new { Message = "A mentés nem sikerült." });
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                return Conflict(new { Message = "Az adatbázis foglalt." });
            }
        }

        [HttpDelete("{supervisionId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int supervisionId,
            [FromQuery] int teacherId,
            [FromQuery] int eventId)
        {
            try
            {
                await _service.DeleteAsync(
                    teacherId, supervisionId, eventId);

                return Ok(new
                {
                    Message = "Felügyelet sikeresen törölve."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new { Message = "A törlés nem sikerült." });
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                return Conflict(new { Message = "Az adatbázis foglalt." });
            }
        }
    }
}
