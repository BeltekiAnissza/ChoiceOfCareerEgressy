using ChoiceOfCareerEgressy.Dto.UserDto.Student;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class StudentRegistrationsController : ControllerBase
    {
        private readonly StudentRegistrationService _service;

        public StudentRegistrationsController(
            StudentRegistrationService service)
        {
            _service = service;
        }

        private bool TryGetCurrentUserId(out int userId) =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

        [HttpGet("events")]
        public async Task<ActionResult<List<StudentEventDto>>> GetEvents()
        {
            if (!TryGetCurrentUserId(out int currentUserId))
                return Unauthorized(new { Message = "Érvénytelen bejelentkezési azonosító." });
            var events = await _service.GetEventsAsync(currentUserId);
            return Ok(events);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            [FromBody] StudentRegistrationDto dto)
        {
            if (!TryGetCurrentUserId(out int currentUserId))
                return Unauthorized(new { Message = "Érvénytelen bejelentkezési azonosító." });
            try
            {
                await _service.RegisterAsync(currentUserId, dto);

                return Ok(new
                {
                    Message = "Sikeres jelentkezés mindkét programra."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new
                {
                    Message = "A jelentkezés nem menthető. Frissítsd az adatokat."
                });
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                return Conflict(new
                {
                    Message = "A jelentkezés ütközött egy másik művelettel. Próbáld újra."
                });
            }
        }
    }
}

