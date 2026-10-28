using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller.Admin
{
    [ApiController]
    [Route("api/schoolyear")]
    [Authorize(Roles = "Admin")]
    public class SchoolYearController : ControllerBase
    {
        private readonly SchoolYearService _service;
        private readonly ILogger<SchoolYearController> _logger;

        public SchoolYearController(SchoolYearService service, ILogger<SchoolYearController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =====================================================
        // BEJELENTKEZETT ADMINISZTRÁTOR AZONOSÍTÓJA
        // =====================================================

        private bool TryGetCurrentUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }

        // =====================================================
        // TANÉVINDÍTÁS ÁLLAPOTÁNAK LEKÉRDEZÉSE
        //
        // GET: api/schoolyear/status
        // =====================================================

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                bool hasStarted =
                    await _service.HasStartedThisYearAsync();

                return Ok(new
                {
                    HasStartedThisYear = hasStarted,
                    CanStart = !hasStarted
                });
            }
            catch (Exception ex) when (
                ex is DbUpdateException ||
                ex is SqliteException)
            {
                _logger.LogError(
                    ex,
                    "Hiba a tanévindítás állapotának lekérdezésekor.");

                return StatusCode(500, new
                {
                    Message = "A tanévindítás állapota nem kérdezhető le."
                });
            }
        }

        // =====================================================
        // ÚJ TANÉV INDÍTÁSA
        //
        // POST: api/schoolyear/start
        //
        // Megerősítés:
        // "ÚJ TANÉV"
        // =====================================================

        [HttpPost("start")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start([FromBody] StartSchoolYearRequest request)
        {
            if (!TryGetCurrentUserId(out int adminId))
            {
                return Challenge();
            }

            if (request == null ||
                request.Confirmation != "ÚJ TANÉV")
            {
                return BadRequest(new
                {
                    Message =
                        "A tanévindításhoz pontosan az " +
                        "'ÚJ TANÉV' szöveget kell megadni."
                });
            }

            try
            {
                var result = await _service.StartAsync(adminId);

                return Ok(new
                {
                    Message = "Az új tanév sikeresen elindult.",
                    AdvancedGrades = result.Advanced,
                    GraduatedGrades = result.Graduated,
                    GraduatedStudents = result.Students
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
                    "Adatbázishiba a tanévindítás során. AdminId: {AdminId}",
                    adminId);

                return StatusCode(500, new
                {
                    Message =
                        "Az új tanév indítása adatbázishiba miatt sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba a tanévindítás során. AdminId: {AdminId}",
                    adminId);

                return StatusCode(500, new
                {
                    Message =
                        "Az adatbázis jelenleg nem érhető el. " +
                        "Próbáld újra később."
                });
            }
        }
    }

    // =====================================================
    // TANÉVINDÍTÁSI KÉRÉS
    // =====================================================

    public class StartSchoolYearRequest
    {
        public string Confirmation { get; set; } = string.Empty;
    }
}