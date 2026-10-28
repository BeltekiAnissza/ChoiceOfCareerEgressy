using ChoiceOfCareerEgressy.Dto;
using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Teacher
{
    [Authorize(Roles = "Teacher")]
    public class ExportGradeModel : PageModel
    {
        private readonly ExcelExportService _exportService;

        public ExportGradeModel(ExcelExportService exportService)
        {
            _exportService = exportService;
        }

        // =========================================
        // ESEMÉNYEK
        // =========================================

        public List<ExportEventOptionDto> Events { get; set; } = new();

        // =========================================
        // OLDAL BETÖLTÉSE
        // =========================================

        public async Task<IActionResult> OnGetAsync()
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int teacherId))
            {
                return Challenge();
            }

            try
            {
                Events = await _exportService
                    .GetMyGradeExportOptionsAsync(teacherId);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            return Page();
        }

        // =========================================
        // OSZTÁLY EXCEL EXPORTÁLÁSA
        // =========================================

        public async Task<IActionResult> OnGetDownloadAsync(int eventId)
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int teacherId))
            {
                return Challenge();
            }

            if (eventId <= 0)
            {
                return BadRequest("Válassz ki egy eseményt.");
            }

            int gradeId;

            try
            {
                gradeId = await _exportService
                    .GetOwnGradeIdAsync(teacherId);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            try
            {
                // Excel elkészítése

                var bytes = await _exportService.ExportGradeAsync(
                    eventId,
                    gradeId);

                // Excel fájlnevének lekérdezése

                var fileName = await _exportService
                    .GetGradeExportFileNameAsync(
                        eventId,
                        gradeId);

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}