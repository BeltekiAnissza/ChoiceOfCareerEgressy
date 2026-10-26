using ChoiceOfCareerEgressy.Dto.GradeDto;
using ChoiceOfCareerEgressy.Model;
using ChoiceOfCareerEgressy.Model.Admin;
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
        private readonly SchoolYearService _schoolYearService;

        public IndexModel(GradeManagementService service, SchoolYearService schoolYearService)
        {
            _service = service;
            _schoolYearService = schoolYearService;
        }

        public List<GradeListItemDto> Grades { get; set; } = new();

        public bool SchoolYearAlreadyStarted { get; set; }

        public int CurrentYear => DateTime.Now.Year;

        [BindProperty]
        public GradeCreateDto Create { get; set; } = new();

        [BindProperty]
        public GradeRenameDto Rename { get; set; } = new();

        [BindProperty]
        public int DeleteGradeId { get; set; }

        public string? ErrorMessage { get; set; }

        public string? SuccessMessage => TempData["GradeSuccess"] as string;

        // Bejelentkezett adminisztrátor azonosítója
        private int? ActorId =>
            int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var id
            ) ? id : null;

        // Osztályok és tanévindítási állapot betöltése
        private async Task LoadDataAsync()
        {
            Grades = await _service.GetGradesAsync();

            SchoolYearAlreadyStarted =
                await _schoolYearService.HasStartedThisYearAsync();
        }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        // Közös műveletkezelés
        private async Task<IActionResult> ExecuteAsync(Func<int, Task> operation, string success)
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

            await LoadDataAsync();

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

        // Elballagott osztály végleges törlése
        public Task<IActionResult> OnPostDeleteGraduatedAsync(int gradeId, string confirmation)
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

        // ======================================================
        // ÚJ TANÉV INDÍTÁSA
        // ======================================================

        public async Task<IActionResult> OnPostStartSchoolYearAsync(string confirmation)
        {
            if (ActorId is not int actorId)
            {
                return Challenge();
            }

            try
            {
                if (confirmation != "ÚJ TANÉV")
                {
                    throw new InvalidOperationException(
                        "A tanévindításhoz pontosan ezt kell megadnod: ÚJ TANÉV");
                }

                var result = await _schoolYearService.StartAsync(actorId);

                TempData["GradeSuccess"] =
                    "Az új tanév sikeresen elindult! " +
                    $"Léptetett osztályok: {result.Advanced}, " +
                    $"elballagott osztályok: {result.Graduated}, " +
                    $"elballagott diákok: {result.Students}. " +
                    "Az új kilencedikes osztályok is létrejöttek.";

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
                    "A tanévindítás adatbázishiba miatt nem sikerült.";
            }

            await LoadDataAsync();

            return Page();
        }
    }
}