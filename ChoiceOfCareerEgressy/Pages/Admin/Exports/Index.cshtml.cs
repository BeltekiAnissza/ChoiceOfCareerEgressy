
using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChoiceOfCareerEgressy.Pages.Admin.Exports
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ExcelExportService _export;

        private const string ExcelContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public IndexModel(ExcelExportService export)
        {
            _export = export;
        }

        // =========================================
        // EXPORTÁLÁSI LEHETŐSÉGEK
        // =========================================

        public List<ExportEventOptionDto> Events { get; private set; }
            = new();

        public List<ExportGradeOptionDto> Grades { get; private set; }
            = new();

        public List<ExportPlanOptionDto> Plans { get; private set; }
            = new();

        public List<ExportTimeSlotOptionDto> TimeSlots { get; private set; }
            = new();

        // =========================================
        // HIBAÜZENET
        // =========================================

        public string? ErrorMessage { get; private set; }

        // =========================================
        // OLDAL BETÖLTÉSE
        // =========================================

        public async Task OnGetAsync()
        {
            await LoadOptionsAsync();
        }

        // =========================================
        // EXPORTÁLÁSI LEHETŐSÉGEK BETÖLTÉSE
        // =========================================

        private async Task LoadOptionsAsync()
        {
            var options = await _export.GetExportOptionsAsync();

            Events = options.Events;
            Grades = options.Grades;
            Plans = options.Plans;
            TimeSlots = options.TimeSlots;
        }

        // =========================================
        // 1. OSZTÁLY EXPORTÁLÁSA
        // GET: ?handler=Grade
        // =========================================

        public async Task<IActionResult> OnGetGradeAsync(int eventId, int gradeId)
        {
            // =====================================
            // PARAMÉTEREK ELLENŐRZÉSE
            // =====================================

            if (eventId <= 0 || gradeId <= 0)
            {
                return BadRequest(
                    "Válassz ki egy eseményt és egy osztályt.");
            }

            // =====================================
            // EXCEL EXPORTÁLÁSA
            // =====================================

            try
            {
                var bytes = await _export.ExportGradeAsync(
                    eventId,
                    gradeId
                );

                var fileName =
                    await _export.GetGradeExportFileNameAsync(
                        eventId,
                        gradeId
                    );

                return File(
                    bytes,
                    ExcelContentType,
                    fileName
                );
            }

            // =====================================
            // HIBAKEZELÉS
            // =====================================

            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;

                await LoadOptionsAsync();

                return Page();
            }
        }

        // =========================================
        // 2. PROGRAM EXPORTÁLÁSA
        // GET: ?handler=Plan
        // =========================================

        public async Task<IActionResult> OnGetPlanAsync(int eventId, int planId, int timeSlotId, bool includeSupervisors = false)
        {
            // =====================================
            // PARAMÉTEREK ELLENŐRZÉSE
            // =====================================

            if (eventId <= 0 ||
                planId <= 0 ||
                timeSlotId <= 0)
            {
                return BadRequest(
                    "Válassz ki egy eseményt, programot és idősávot.");
            }

            // =====================================
            // EXCEL EXPORTÁLÁSA
            // =====================================

            try
            {
                var bytes = await _export.ExportPlanAsync(
                    eventId,
                    planId,
                    timeSlotId,
                    includeSupervisors
                );

                var fileName =
                    await _export.GetPlanExportFileNameAsync(
                        eventId,
                        planId,
                        timeSlotId
                    );

                return File(
                    bytes,
                    ExcelContentType,
                    fileName
                );
            }

            // =====================================
            // HIBAKEZELÉS
            // =====================================

            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;

                await LoadOptionsAsync();

                return Page();
            }
        }

        // =========================================
        // 3. TANÁRI FELÜGYELETEK EXPORTÁLÁSA
        // GET: ?handler=Teachers
        // =========================================

        public async Task<IActionResult> OnGetTeachersAsync(int eventId)
        {
            // =====================================
            // PARAMÉTEREK ELLENŐRZÉSE
            // =====================================

            if (eventId <= 0)
            {
                return BadRequest(
                    "Válassz ki egy eseményt.");
            }

            // =====================================
            // EXCEL EXPORTÁLÁSA
            // =====================================

            try
            {
                var bytes =
                    await _export.ExportTeachersAsync(eventId);

                var fileName =
                    await _export.GetTeachersExportFileNameAsync(
                        eventId
                    );

                return File(
                    bytes,
                    ExcelContentType,
                    fileName
                );
            }

            // =====================================
            // HIBAKEZELÉS
            // =====================================

            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;

                await LoadOptionsAsync();

                return Page();
            }
        }
        // =========================================
        // 4. ÖSSZES DIÁK EXPORTÁLÁSA
        // GET: ?handler=AllStudents
        // =========================================

        public async Task<IActionResult> OnGetAllStudentsAsync(int eventId)
        {
            // =====================================
            // PARAMÉTEREK ELLENŐRZÉSE
            // =====================================

            if (eventId <= 0)
            {
                return BadRequest(
                    "Válassz ki egy eseményt.");
            }

            // =====================================
            // EXCEL EXPORTÁLÁSA
            // =====================================

            try
            {
                var bytes = await _export.ExportAllStudentsAsync(
                    eventId);

                var fileName =
                    await _export.GetAllStudentsExportFileNameAsync(
                        eventId);

                return File(
                    bytes,
                    ExcelContentType,
                    fileName);
            }

            // =====================================
            // HIBAKEZELÉS
            // =====================================

            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;

                await LoadOptionsAsync();

                return Page();
            }
        }
    }
}