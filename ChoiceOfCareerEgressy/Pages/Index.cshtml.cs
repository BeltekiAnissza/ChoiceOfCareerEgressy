using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChoiceOfCareerEgressy.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToPage("/Account/Login");
            }

            if (User.IsInRole("Admin"))
            {
                return RedirectToPage("/Admin/Index");
            }

            if (User.IsInRole("Teacher"))
            {
                return RedirectToPage("/Teacher/Index");
            }

            if (User.IsInRole("Student"))
            {
                return RedirectToPage("/Student/Index");
            }

            return RedirectToPage("/Account/Login");
        }
    }
}
