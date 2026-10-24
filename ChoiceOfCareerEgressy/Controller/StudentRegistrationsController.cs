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

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("events")]
        public async Task<ActionResult<List<StudentEventDto>>> GetEvents()
        {
            var events = await _service.GetEventsAsync(CurrentUserId);
            return Ok(events);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            [FromBody] StudentRegistrationDto dto)
        {
            try
            {
                await _service.RegisterAsync(CurrentUserId, dto);

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

