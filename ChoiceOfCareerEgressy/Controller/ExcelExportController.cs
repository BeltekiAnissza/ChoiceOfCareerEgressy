using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExcelExportController : ControllerBase
    {
        private readonly ExcelExportService _export;

        public ExcelExportController(ExcelExportService export)
        {
            _export = export;
        }

        // GET: api/export/grade?eventId=1&gradeId=2

        [HttpGet("grade")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportGrade([FromQuery] int eventId, [FromQuery] int gradeId)
        {
            if (eventId <= 0 || gradeId <= 0)
            {
                return BadRequest(
                    "Érvényes eseményt és osztályt kell választani.");
            }

            try
            {
                var bytes = await _export.ExportGradeAsync(eventId, gradeId);

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"osztaly_{gradeId}_esemeny_{eventId}.xlsx");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/export/plan?eventId=1&planId=3

        [HttpGet("plan")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportPlan([FromQuery] int eventId, [FromQuery] int planId)
        {
            if (eventId <= 0 || planId <= 0)
            {
                return BadRequest("Érvényes eseményt és programot kell választani.");
            }

            try
            {
                var bytes = await _export.ExportPlanAsync(eventId, planId);

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"program_{planId}_esemeny_{eventId}.xlsx");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // GET: api/export/my-grade?eventId=1

        [HttpGet("my-grade")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ExportMyGrade([FromQuery] int eventId)
        {
            if (eventId <= 0)
            {
                return BadRequest(
                    "Érvényes eseményt kell választani.");
            }

            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int actorId))
            {
                return Challenge();
            }

            try
            {
                // A tanár csak a saját osztályát exportálhatja.
                int gradeId = await _export.GetOwnGradeIdAsync(actorId);

                var bytes = await _export.ExportGradeAsync(eventId, gradeId);

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"osztaly_{gradeId}_esemeny_{eventId}.xlsx");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
