using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller.Shared
{
    [ApiController]
    [Route("api/excelexport")]
    [Authorize]
    public class ExcelExportController : ControllerBase
    {
        private readonly ExcelExportService _service;
        private readonly ILogger<ExcelExportController> _logger;

        private const string ExcelContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public ExcelExportController(ExcelExportService service, ILogger<ExcelExportController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =====================================================
        // BEJELENTKEZETT FELHASZNÁLÓ AZONOSÍTÓJA
        // =====================================================

        private bool TryGetCurrentUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }

        // =====================================================
        // ADMIN EXPORTÁLÁSI LEHETŐSÉGEK LEKÉRDEZÉSE
        //
        // GET: api/excelexport/options
        // =====================================================

        [HttpGet("options")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ExcelExportOptionsDto>> GetOptions()
        {
            try
            {
                var options = await _service.GetExportOptionsAsync();

                return Ok(options);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Adatbázishiba az exportálási lehetőségek lekérdezésekor.");

                return StatusCode(500, new
                {
                    Message = "Az exportálási lehetőségek lekérdezése sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba az exportálási lehetőségek lekérdezésekor.");

                return StatusCode(500, new
                {
                    Message = "Az adatbázis jelenleg nem érhető el."
                });
            }
        }

        // =====================================================
        // TANÁRI EXPORTÁLÁSI LEHETŐSÉGEK LEKÉRDEZÉSE
        //
        // GET: api/excelexport/my-grade/options
        // =====================================================

        [HttpGet("my-grade/options")]
        [Authorize(Roles = "Teacher")]
        public async Task<ActionResult<List<ExportEventOptionDto>>> GetMyGradeOptions()
        {
            if (!TryGetCurrentUserId(out int teacherId))
            {
                return Challenge();
            }

            try
            {
                var events =
                    await _service.GetMyGradeExportOptionsAsync(
                        teacherId);

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
                    "Adatbázishiba a tanári exportálási lehetőségek lekérdezésekor. " +
                    "TeacherId: {TeacherId}",
                    teacherId);

                return StatusCode(500, new
                {
                    Message = "Az események lekérdezése sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba a tanári exportálási lehetőségek lekérdezésekor. " +
                    "TeacherId: {TeacherId}",
                    teacherId);

                return StatusCode(500, new
                {
                    Message = "Az adatbázis jelenleg nem érhető el."
                });
            }
        }

        // =====================================================
        // OSZTÁLY EXCEL EXPORTÁLÁSA
        //
        // GET: api/excelexport/grade?eventId=1&gradeId=2
        // =====================================================

        [HttpGet("grade")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportGrade([FromQuery] int eventId, [FromQuery] int gradeId)
        {
            if (eventId <= 0 || gradeId <= 0)
            {
                return BadRequest(
                    "Válassz ki egy eseményt és egy osztályt.");
            }

            try
            {
                var bytes = await _service.ExportGradeAsync(
                    eventId,
                    gradeId);

                var fileName =
                    await _service.GetGradeExportFileNameAsync(
                        eventId,
                        gradeId);

                return File(
                    bytes,
                    ExcelContentType,
                    fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Adatbázishiba az osztály exportálásakor. " +
                    "EventId: {EventId}, GradeId: {GradeId}",
                    eventId,
                    gradeId);

                return StatusCode(500, new
                {
                    Message = "Az osztály exportálása sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba az osztály exportálásakor. " +
                    "EventId: {EventId}, GradeId: {GradeId}",
                    eventId,
                    gradeId);

                return StatusCode(500, new
                {
                    Message = "Az adatbázis jelenleg nem érhető el."
                });
            }
        }

        // =====================================================
        // PROGRAM EXCEL EXPORTÁLÁSA
        //
        // GET: api/excelexport/plan
        //
        // Paraméterek:
        // eventId
        // planId
        // timeSlotId
        // includeSupervisors
        // =====================================================

        [HttpGet("plan")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportPlan([FromQuery] int eventId, [FromQuery] int planId, [FromQuery] int timeSlotId, [FromQuery] bool includeSupervisors = false)
        {
            if (eventId <= 0 ||
                planId <= 0 ||
                timeSlotId <= 0)
            {
                return BadRequest(
                    "Válassz ki egy eseményt, programot és idősávot.");
            }

            try
            {
                var bytes = await _service.ExportPlanAsync(
                    eventId,
                    planId,
                    timeSlotId,
                    includeSupervisors);

                var fileName =
                    await _service.GetPlanExportFileNameAsync(
                        eventId,
                        planId,
                        timeSlotId);

                return File(
                    bytes,
                    ExcelContentType,
                    fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Adatbázishiba a program exportálásakor. " +
                    "EventId: {EventId}, PlanId: {PlanId}, " +
                    "TimeSlotId: {TimeSlotId}",
                    eventId,
                    planId,
                    timeSlotId);

                return StatusCode(500, new
                {
                    Message = "A program exportálása sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba a program exportálásakor. " +
                    "EventId: {EventId}, PlanId: {PlanId}, " +
                    "TimeSlotId: {TimeSlotId}",
                    eventId,
                    planId,
                    timeSlotId);

                return StatusCode(500, new
                {
                    Message = "Az adatbázis jelenleg nem érhető el."
                });
            }
        }

        // =====================================================
        // TANÁRI FELÜGYELETI JELENTKEZÉSEK EXPORTÁLÁSA
        //
        // GET: api/excelexport/teachers?eventId=1
        // =====================================================

        [HttpGet("teachers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportTeachers([FromQuery] int eventId)
        {
            if (eventId <= 0)
            {
                return BadRequest(
                    "Válassz ki egy eseményt.");
            }

            try
            {
                var bytes =
                    await _service.ExportTeachersAsync(eventId);

                var fileName =
                    await _service.GetTeachersExportFileNameAsync(
                        eventId);

                return File(
                    bytes,
                    ExcelContentType,
                    fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Adatbázishiba a tanári felügyeletek exportálásakor. " +
                    "EventId: {EventId}",
                    eventId);

                return StatusCode(500, new
                {
                    Message = "A tanári felügyeletek exportálása sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba a tanári felügyeletek exportálásakor. " +
                    "EventId: {EventId}",
                    eventId);

                return StatusCode(500, new
                {
                    Message = "Az adatbázis jelenleg nem érhető el."
                });
            }
        }

        // =====================================================
        // SAJÁT OSZTÁLY EXCEL EXPORTÁLÁSA
        //
        // GET: api/excelexport/my-grade?eventId=1
        // =====================================================

        [HttpGet("my-grade")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ExportMyGrade([FromQuery] int eventId)
        {
            if (!TryGetCurrentUserId(out int teacherId))
            {
                return Challenge();
            }

            if (eventId <= 0)
            {
                return BadRequest(
                    "Válassz ki egy eseményt.");
            }

            int gradeId;

            try
            {
                gradeId = await _service.GetOwnGradeIdAsync(
                    teacherId);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            try
            {
                var bytes = await _service.ExportGradeAsync(
                    eventId,
                    gradeId);

                var fileName =
                    await _service.GetGradeExportFileNameAsync(
                        eventId,
                        gradeId);

                return File(
                    bytes,
                    ExcelContentType,
                    fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Adatbázishiba a saját osztály exportálásakor. " +
                    "TeacherId: {TeacherId}, EventId: {EventId}",
                    teacherId,
                    eventId);

                return StatusCode(500, new
                {
                    Message = "Az osztály exportálása sikertelen."
                });
            }
            catch (SqliteException ex)
            {
                _logger.LogError(
                    ex,
                    "SQLite-hiba a saját osztály exportálásakor. " +
                    "TeacherId: {TeacherId}, EventId: {EventId}",
                    teacherId,
                    eventId);

                return StatusCode(500, new
                {
                    Message = "Az adatbázis jelenleg nem érhető el."
                });
            }
        }
    }
}