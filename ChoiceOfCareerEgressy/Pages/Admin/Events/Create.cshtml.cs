using ChoiceOfCareerEgressy.Dto.ProgramDto;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChoiceOfCareerEgressy.Pages.Events
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly EventService _eventService;

        public CreateModel(EventService eventService)
        {
            _eventService = eventService;
        }

        [BindProperty]
        public CreateEventDto EventInput { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var created = await _eventService.CreateAsync(EventInput);

                TempData["SuccessMessage"] =
                    $"Az esemény sikeresen létrejött: {created.EventName}";

                return RedirectToPage("/Admin/Programs/Import");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return Page();
            }
        }
    }
}
