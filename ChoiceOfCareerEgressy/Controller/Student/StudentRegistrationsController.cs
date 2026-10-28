using ChoiceOfCareerEgressy.Dto.UserDto.Student;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller.Student
{
    [ApiController]
    [Route("api/studentregistrations")]
    [Authorize(Roles = "Student")]
    public class StudentRegistrationsController : ControllerBase
    {
        private readonly StudentRegistrationService _service;
        private readonly ILogger<StudentRegistrationsController> _logger;

        public StudentRegistrationsController(StudentRegistrationService service, ILogger<StudentRegistrationsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =====================================================
        // BEJELENTKEZETT DIÁK AZONOSÍTÓJA
        // =====================================================

        private bool TryGetCurrentUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }

        // =====================================================
        // ESEMÉNYEK ÉS JELENTKEZÉSEK LEKÉRDEZÉSE
        //
        // GET: api/studentregistrations/events
        // =====================================================

        [HttpGet("events")]
        public async Task<ActionResult<List<StudentEventDto>>> GetEvents()
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Challenge();
            }

            try
            {
                var events = await _service.GetEventsAsync(userId);

                return Ok(events);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Adatbázishiba a diák eseményeinek lekérdezésekor. " +
                    "UserId: {UserId}",
                    userId);

                return StatusCode(500, new
                {
                    Message = "Az események lekérdezése sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba a diák eseményeinek lekérdezésekor. " +
                    "UserId: {UserId}",
                    userId);

                return StatusCode(500, new
                {
                    Message = "Az adatbázis jelenleg nem érhető el."
                });
            }
        }

        // =====================================================
        // ÚJ JELENTKEZÉS
        //
        // POST: api/studentregistrations
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromBody] StudentRegistrationDto dto)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Hiányzó vagy hibás jelentkezési adatok."
                });
            }

            try
            {
                await _service.RegisterAsync(userId, dto);

                return Ok(new
                {
                    Message = "Sikeres jelentkezés! A kiválasztott programokat rögzítettük."
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
                    "Adatbázishiba új diákjelentkezés mentésekor. " +
                    "UserId: {UserId}, EventId: {EventId}",
                    userId,
                    dto.EventId);

                return StatusCode(500, new
                {
                    Message = "A jelentkezés mentése sikertelen. Próbáld újra."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba új diákjelentkezés mentésekor. " +
                    "UserId: {UserId}, EventId: {EventId}",
                    userId,
                    dto.EventId);

                return StatusCode(500, new
                {
                    Message = "Az adatbázis jelenleg foglalt. Próbáld újra később."
                });
            }
        }

        // =====================================================
        // MEGLÉVŐ JELENTKEZÉS MÓDOSÍTÁSA
        //
        // PUT: api/studentregistrations
        // =====================================================

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] StudentRegistrationDto dto)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Hiányzó vagy hibás jelentkezési adatok."
                });
            }

            try
            {
                await _service.UpdateRegistrationAsync(userId, dto);

                return Ok(new
                {
                    Message = "Sikeresen módosítottad a jelentkezéseidet!"
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
                    "Adatbázishiba diákjelentkezés módosításakor. " +
                    "UserId: {UserId}, EventId: {EventId}",
                    userId,
                    dto.EventId);

                return StatusCode(500, new
                {
                    Message = "A jelentkezés módosítása sikertelen. Próbáld újra."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba diákjelentkezés módosításakor. " +
                    "UserId: {UserId}, EventId: {EventId}",
                    userId,
                    dto.EventId);

                return StatusCode(500, new
                {
                    Message = "Az adatbázis jelenleg foglalt. Próbáld újra később."
                });
            }
        }
    }
}