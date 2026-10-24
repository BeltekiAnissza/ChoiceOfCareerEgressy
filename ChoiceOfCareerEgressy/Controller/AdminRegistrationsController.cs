using ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChoiceOfCareerEgressy.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRegistrationsController : ControllerBase
    {
        private readonly AdminRegistrationService _service;

        public AdminRegistrationsController(
            AdminRegistrationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<AdminRegistrationPageDto>> Get(
            [FromQuery] int? eventId,
            [FromQuery] int? gradeId,
            [FromQuery] int? planSessionId)
        {
            return Ok(await _service.GetPageAsync(
                eventId, gradeId, planSessionId));
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Change(
            [FromBody] AdminChangeApplicationDto dto)
        {
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
                    Message = "Az adatbázis foglalt. Próbáld újra."
                });
            }
        }

        [HttpDelete("{applicationId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int applicationId,
            [FromQuery] int studentId,
            [FromQuery] int eventId)
        {
            try
            {
                await _service.DeleteAsync(
                    studentId, applicationId, eventId);

                return Ok(new
                {
                    Message = "A jelentkezés sikeresen törölve."
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
                    Message = "A törlés nem sikerült."
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
