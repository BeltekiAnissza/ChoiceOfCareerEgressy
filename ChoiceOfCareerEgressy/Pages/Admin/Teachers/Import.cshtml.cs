using ChoiceOfCareerEgressy.Dto.UserDto.Teacher;
using ChoiceOfCareerEgressy.Model.Admin.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChoiceOfCareerEgressy.Pages.Admin.Teacher
{
    [Authorize(Roles = "Admin")]
    public class ImportModel : PageModel
    {
        private readonly TeacherImportService _importService;

        public ImportModel(TeacherImportService importService)
        {
            _importService = importService;
        }

        [BindProperty]
        public IFormFile? ExcelFile { get; set; }

        public TeacherImportResultDto? Result { get; set; }

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
            catch (InvalidDataException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex) when (
                ex is ClosedXML.Excel.Exceptions.InvalidReferenceException
                || ex is System.IO.InvalidDataException)
            {
                ErrorMessage = "Az Excel-fájl nem olvasható.";
            }

            return Page();
        }
    }
}
