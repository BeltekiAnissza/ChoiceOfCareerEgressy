using ChoiceOfCareerEgressy.Dto.UserDto.Student;
using ChoiceOfCareerEgressy.Model.Admin.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChoiceOfCareerEgressy.Pages.Admin.Students
{
    [Authorize(Roles = "Admin")]
    public class ImportModel : PageModel
    {
        private readonly StudentImportService _importService;

        public ImportModel(StudentImportService importService)
        {
            _importService = importService;
        }

        [BindProperty]
        public IFormFile? ExcelFile { get; set; }

        public StudentImportResultDto? Result { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ExcelFile == null)
            {
                ErrorMessage = "Válassz ki egy Excel-fájlt!";
                return Page();
            }

            try
            {
                Result = await _importService.ImportAsync(ExcelFile);
            }
            catch (Exception ex) when (
                ex is InvalidDataException
                || ex is ClosedXML.Excel.Exceptions.ClosedXMLException)
            {
                ErrorMessage = "Nem sikerült feldolgozni az Excel-fájlt. "
                    + "Ellenőrizd a formátumát és az oszlopokat.";
            }

            return Page();
        }
    }
}
