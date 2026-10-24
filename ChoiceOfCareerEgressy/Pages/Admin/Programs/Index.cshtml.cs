using ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms;
using ChoiceOfCareerEgressy.Model.Admin.Programs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChoiceOfCareerEgressy.Pages.Admin.Programs
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AdminProgramService _service;

        public IndexModel(AdminProgramService service)
        {
            _service = service;
        }

        // A megjelenítendő programok
        public List<AdminProgramListDto> Programs { get; set; } = new();

        // Események a szűréshez
        public List<AdminProgramOptionDto> Events { get; set; } = new();

        // Keresési feltételek
        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EventId { get; set; }

        // Visszajelzések
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            SuccessMessage = TempData["SuccessMessage"] as string;
            ErrorMessage = TempData["ErrorMessage"] as string;

            // Programok betöltése
            Programs = await _service.GetProgramsAsync(
                EventId,
                Search);

            // Események betöltése
            var options = await _service.GetOptionsAsync();
            Events = options.Events;
        }
    }
}

