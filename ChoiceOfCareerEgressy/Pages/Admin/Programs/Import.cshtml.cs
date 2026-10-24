using ChoiceOfCareerEgressy.Dto.ProgramDto.ImportPrograms;
using ChoiceOfCareerEgressy.Model.Admin.Imports;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Pages.Admin.Programs
{
    [Authorize(Roles = "Admin")]
    public class ImportModel : PageModel
    {
        private readonly ProgramImportService _importService;
        private readonly ChoiceOfCareerPersistence _context;

        public ImportModel(ProgramImportService importService, ChoiceOfCareerPersistence context)
        {
            _importService = importService;
            _context = context;
        }

        [BindProperty]
        public IFormFile? ExcelFile { get; set; }

        [BindProperty]
        public int EventId { get; set; }

        public List<EventOptionDto> Events { get; set; } = new();

        public ProgramImportResultDto? Result { get; set; }

        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadEventsAsync();
        }

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
                    ExcelFile, EventId);
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

        private async Task LoadEventsAsync()
        {
            Events = await _context.Events
                .AsNoTracking()
                .OrderByDescending(e => e.EventId)
                .Select(e => new EventOptionDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName
                })
                .ToListAsync();
        }
    }

    public class EventOptionDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = "";
    }
}
