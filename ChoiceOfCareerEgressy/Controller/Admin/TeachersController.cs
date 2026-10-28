using ChoiceOfCareerEgressy.Dto.UserDto.Teacher;
using ChoiceOfCareerEgressy.Model.Admin.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoiceOfCareerEgressy.Controller.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class TeachersController : ControllerBase
    {
        private readonly TeacherImportService _teacherImportService;
        private readonly ILogger<TeachersController> _logger;

        public TeachersController(TeacherImportService teacherImportService, ILogger<TeachersController> logger)
        {
            _teacherImportService = teacherImportService;
            _logger = logger;
        }

        // =========================================
        // TANÁROK IMPORTÁLÁSA EXCEL-FÁJLBÓL
        // POST: api/teachers/import
        // =========================================

        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<TeacherImportResultDto>> Import([FromForm] IFormFile? file)
        {
            // =====================================
            // FÁJL MEGLÉTÉNEK ELLENŐRZÉSE
            // =====================================

            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    Message = "Válassz ki egy Excel-fájlt!"
                });
            }

            // =====================================
            // TANÁROK IMPORTÁLÁSA
            // =====================================

            try
            {
                var result =
                    await _teacherImportService.ImportAsync(file);

                _logger.LogInformation(
                    "Tanárimport befejezve. " +
                    "Importálva: {ImportedCount}, " +
                    "Kihagyva: {SkippedCount}",
                    result.ImportedCount,
                    result.SkippedCount
                );

                return Ok(result);
            }

            // =====================================
            // HIBÁS FÁJL VAGY ADAT
            // =====================================

            catch (InvalidDataException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }

            // =====================================
            // VÁRATLAN HIBA
            // =====================================

            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Hiba történt a tanárok importálásakor."
                );

                return StatusCode(500, new
                {
                    Message = "A tanárok importálása nem sikerült."
                });
            }
        }
    }
}