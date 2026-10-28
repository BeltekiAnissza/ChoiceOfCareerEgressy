using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller.Admin
{
    [ApiController]
    [Route("api/adminalldatadelete")]
    [Authorize(Roles = "Admin")]
    public class AdminAllDataDeleteController : ControllerBase
    {
        private readonly AdminAllDataDeleteService _service;
        private readonly ILogger<AdminAllDataDeleteController> _logger;

        public AdminAllDataDeleteController(
            AdminAllDataDeleteService service,
            ILogger<AdminAllDataDeleteController> logger)
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
        // ÖSSZES PROGRAM TÖRLÉSE
        //
        // DELETE: api/adminalldatadelete/programs
        //
        // Megerősítés: "ÖSSZES PROGRAM TÖRLÉSE"
        // =====================================================

        [HttpDelete("programs")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllPrograms([FromBody] DeleteAllDataRequest request)
        {
            if (!TryGetCurrentUserId(out int adminId))
            {
                return Challenge();
            }

            if (request == null ||
                request.Confirmation != "ÖSSZES PROGRAM TÖRLÉSE")
            {
                return BadRequest(new
                {
                    Message =
                        "A művelethez pontosan az " +
                        "'ÖSSZES PROGRAM TÖRLÉSE' szöveget kell megadni."
                });
            }

            try
            {
                int deletedCount =
                    await _service.DeleteAllProgramsAsync(adminId);

                _logger.LogWarning(
                    "Az adminisztrátor törölte az összes programot. " +
                    "AdminId: {AdminId}, DeletedCount: {DeletedCount}",
                    adminId,
                    deletedCount);

                return Ok(new
                {
                    Message = "Az összes program sikeresen törölve.",
                    DeletedCount = deletedCount
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
                    "Adatbázishiba az összes program törlésekor. " +
                    "AdminId: {AdminId}",
                    adminId);

                return StatusCode(500, new
                {
                    Message =
                        "A programok törlése adatbázishiba miatt sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba az összes program törlésekor. " +
                    "AdminId: {AdminId}",
                    adminId);

                return StatusCode(500, new
                {
                    Message =
                        "Az adatbázis jelenleg nem érhető el. " +
                        "Próbáld újra később."
                });
            }
        }

        // =====================================================
        // ÖSSZES TANÁR TÖRLÉSE
        //
        // DELETE: api/adminalldatadelete/teachers
        //
        // Megerősítés: "ÖSSZES TANÁR TÖRLÉSE"
        // =====================================================

        [HttpDelete("teachers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllTeachers([FromBody] DeleteAllDataRequest request)
        {
            if (!TryGetCurrentUserId(out int adminId))
            {
                return Challenge();
            }

            if (request == null ||
                request.Confirmation != "ÖSSZES TANÁR TÖRLÉSE")
            {
                return BadRequest(new
                {
                    Message =
                        "A művelethez pontosan az " +
                        "'ÖSSZES TANÁR TÖRLÉSE' szöveget kell megadni."
                });
            }

            try
            {
                int deletedCount =
                    await _service.DeleteAllTeachersAsync(adminId);

                _logger.LogWarning(
                    "Az adminisztrátor törölte az összes tanárt. " +
                    "AdminId: {AdminId}, DeletedCount: {DeletedCount}",
                    adminId,
                    deletedCount);

                return Ok(new
                {
                    Message = "Az összes tanár sikeresen törölve.",
                    DeletedCount = deletedCount
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
                    "Adatbázishiba az összes tanár törlésekor. " +
                    "AdminId: {AdminId}",
                    adminId);

                return StatusCode(500, new
                {
                    Message =
                        "A tanárok törlése adatbázishiba miatt sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba az összes tanár törlésekor. " +
                    "AdminId: {AdminId}",
                    adminId);

                return StatusCode(500, new
                {
                    Message =
                        "Az adatbázis jelenleg nem érhető el. " +
                        "Próbáld újra később."
                });
            }
        }

        // =====================================================
        // ÖSSZES DIÁK TÖRLÉSE
        //
        // DELETE: api/adminalldatadelete/students
        //
        // Megerősítés: "ÖSSZES DIÁK TÖRLÉSE"
        // =====================================================

        [HttpDelete("students")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllStudents([FromBody] DeleteAllDataRequest request)
        {
            if (!TryGetCurrentUserId(out int adminId))
            {
                return Challenge();
            }

            if (request == null ||
                request.Confirmation != "ÖSSZES DIÁK TÖRLÉSE")
            {
                return BadRequest(new
                {
                    Message =
                        "A művelethez pontosan az " +
                        "'ÖSSZES DIÁK TÖRLÉSE' szöveget kell megadni."
                });
            }

            try
            {
                int deletedCount =
                    await _service.DeleteAllStudentsAsync(adminId);

                _logger.LogWarning(
                    "Az adminisztrátor törölte az összes diákot. " +
                    "AdminId: {AdminId}, DeletedCount: {DeletedCount}",
                    adminId,
                    deletedCount);

                return Ok(new
                {
                    Message = "Az összes diák sikeresen törölve.",
                    DeletedCount = deletedCount
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
                    "Adatbázishiba az összes diák törlésekor. " +
                    "AdminId: {AdminId}",
                    adminId);

                return StatusCode(500, new
                {
                    Message =
                        "A diákok törlése adatbázishiba miatt sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba az összes diák törlésekor. " +
                    "AdminId: {AdminId}",
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
    // TÖMEGES TÖRLÉS MEGERŐSÍTÉSE
    // =====================================================

    public class DeleteAllDataRequest
    {
        public string Confirmation { get; set; } = string.Empty;
    }
}