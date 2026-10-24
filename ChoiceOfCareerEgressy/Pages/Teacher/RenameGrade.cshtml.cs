using ChoiceOfCareerEgressy.Dto.GradeDto;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Teacher
{
    [Authorize(Roles = "Teacher")]
    public class RenameGradeModel : PageModel
    {
        private readonly GradeManagementService _service;
        public RenameGradeModel(GradeManagementService service) => _service = service;
        [BindProperty] public GradeRenameDto Rename { get; set; } = new();
        public string? CurrentGradeName { get; set; }
        public string? ErrorMessage { get; set; }
        private int? ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        private async Task<bool> LoadAsync(int actorId)
        {
            var gradeId = await _service.GetHomeroomGradeIdAsync(actorId);
            if (!gradeId.HasValue) return false;
            var grade = (await _service.GetGradesAsync()).FirstOrDefault(g => g.GradeId == gradeId.Value);
            if (grade == null) return false;
            CurrentGradeName = grade.GradeName;
            Rename.GradeId = gradeId.Value;
            return true;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            if (ActorId is not int id) return Challenge();
            return await LoadAsync(id) ? Page() : Forbid();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (ActorId is not int id) return Challenge();
            if (!await LoadAsync(id)) return Forbid();
            try
            {
                await _service.RenameAsync(id, Rename);
                TempData["Success"] = "Az osztály neve sikeresen megváltozott.";
                return RedirectToPage("/Teacher/Homeroom");
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { ErrorMessage = ex.Message; }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            { ErrorMessage = "A módosítás nem sikerült."; }
            return Page();
        }
    }
}
