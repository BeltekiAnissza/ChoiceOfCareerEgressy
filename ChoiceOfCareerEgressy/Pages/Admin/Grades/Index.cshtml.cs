using ChoiceOfCareerEgressy.Dto.GradeDto;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Admin.Grades
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly GradeManagementService _service;

        public IndexModel(GradeManagementService service)
        {
            _service = service;
        }

        public List<GradeListItemDto> Grades { get; set; } = new();

        [BindProperty]
        public GradeCreateDto Create { get; set; } = new();

        [BindProperty]
        public GradeRenameDto Rename { get; set; } = new();

        [BindProperty]
        public int DeleteGradeId { get; set; }

        public string? ErrorMessage { get; set; }

        public string? SuccessMessage =>
            TempData["GradeSuccess"] as string;

        // Bejelentkezett adminisztrátor azonosítója
        private int? ActorId =>
            int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var id
            ) ? id : null;

        // Osztályok betöltése
        public async Task OnGetAsync()
        {
            Grades = await _service.GetGradesAsync();
        }

        // Közös műveletkezelés
        private async Task<IActionResult> ExecuteAsync(
            Func<int, Task> operation,
            string success)
        {
            if (ActorId is not int actorId)
            {
                return Challenge();
            }

            try
            {
                await operation(actorId);

                TempData["GradeSuccess"] = success;

                return RedirectToPage();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (DbUpdateException)
            {
                ErrorMessage =
                    "Az adatbázis-művelet nem sikerült. Ellenőrizd a kapcsolódó adatokat.";
            }

            Grades = await _service.GetGradesAsync();

            return Page();
        }

        // Új osztály létrehozása
        public Task<IActionResult> OnPostCreateAsync()
        {
            return ExecuteAsync(
                id => _service.CreateAsync(id, Create),
                "Az osztály sikeresen létrejött."
            );
        }

        // Osztály átnevezése
        public Task<IActionResult> OnPostRenameAsync()
        {
            return ExecuteAsync(
                id => _service.RenameAsync(id, Rename),
                "Az osztály neve sikeresen megváltozott."
            );
        }

        // Üres osztály törlése
        public Task<IActionResult> OnPostDeleteAsync()
        {
            return ExecuteAsync(
                id => _service.DeleteAsync(id, DeleteGradeId),
                "Az osztály sikeresen törölve."
            );
        }

        // Teljes osztály ballagtatása
        public Task<IActionResult> OnPostGraduateAsync(int gradeId)
        {
            return ExecuteAsync(
                id => _service.GraduateAsync(id, gradeId),
                "Az osztály ballagtatása sikeres. A diákok az Elballagott osztályba kerültek."
            );
        }

        public Task<IActionResult> OnPostDeleteGraduatedAsync(
            int gradeId,
            string confirmation)
        {
            return ExecuteAsync(
                async actorId =>
                {
                    if (confirmation != "VÉGLEGES TÖRLÉS")
                    {
                        throw new InvalidOperationException(
                            "A végleges törléshez pontosan ezt kell megadnod: VÉGLEGES TÖRLÉS");
                    }

                    await _service.DeleteGraduatedAsync(
                        actorId,
                        gradeId);
                },
                "Az Elballagott osztály és a hozzá tartozó diákok véglegesen törölve lettek."
            );
        }

    }
}
