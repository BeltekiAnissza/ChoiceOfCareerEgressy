using ChoiceOfCareerEgressy.Dto.ProgramDto.ImportPrograms;
using ChoiceOfCareerEgressy.Model.Admin.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChoiceOfCareerEgressy.Pages.Admin.Programs
{
    [Authorize(Roles = "Admin")]
    public class ImportModel : PageModel
    {
        private readonly ProgramImportService _importService;

        public ImportModel(ProgramImportService importService)
        {
            _importService = importService;
        }

        [BindProperty]
        public IFormFile? ExcelFile { get; set; }

        [BindProperty]
        public int EventId { get; set; }

        public List<EventOptionDto> Events { get; set; } = new();

        public ProgramImportResultDto? Result { get; set; }

        public string? ErrorMessage { get; set; }

        // =========================================
        // OLDAL BETÖLTÉSE
        // =========================================

        public async Task OnGetAsync()
        {
            await LoadEventsAsync();
        }

        // =========================================
        // EXCEL IMPORTÁLÁSA
        // =========================================

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadEventsAsync();

            if (ExcelFile == null || EventId <= 0)
            {
                ErrorMessage = "Válassz eseményt és Excel-fájlt!";
                return Page();
            }

            try
            {
                Result = await _importService.ImportAsync(
                    ExcelFile,
                    EventId);
            }
            catch (InvalidDataException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex) when (
                ex is ClosedXML.Excel.Exceptions.EmptyTableException
                || ex is FormatException)
            {
                ErrorMessage = "Nem sikerült feldolgozni az Excel-fájlt.";
            }

            return Page();
        }

        // =========================================
        // ESEMÉNYEK BETÖLTÉSE
        // =========================================

        private async Task LoadEventsAsync()
        {
            Events = await _importService.GetAvailableEventsAsync();
        }
    }
}