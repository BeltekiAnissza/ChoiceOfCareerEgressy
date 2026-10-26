using ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms;
using ChoiceOfCareerEgressy.Model.Admin.Programs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        // =========================================
        // PROGRAMOK
        // =========================================

        public List<AdminProgramListDto> Programs { get; set; } = new();

        // =========================================
        // ESEMÉNYEK
        // =========================================

        public List<AdminProgramOptionDto> Events { get; set; } = new();

        // =========================================
        // KERESÉSI FELTÉTELEK
        // =========================================

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EventId { get; set; }

        // =========================================
        // VISSZAJELZÉSEK
        // =========================================

        public string? SuccessMessage { get; set; }

        public string? ErrorMessage { get; set; }

        // =========================================
        // OLDAL BETÖLTÉSE
        // =========================================

        public async Task OnGetAsync()
        {
            SuccessMessage = TempData["SuccessMessage"] as string;
            ErrorMessage = TempData["ErrorMessage"] as string;

            var options = await _service.GetOptionsAsync();

            Events = options.Events;

            // Ha nincs kiválasztott esemény,
            // automatikusan az elsőt választjuk.
            // A szolgáltatás az eseményeket
            // dátum szerint csökkenő sorrendben adja vissza.

            if (!EventId.HasValue && Events.Any())
            {
                EventId = Events.First().Id;
            }

            // Csak létező eseményt engedünk.

            if (EventId.HasValue &&
                !Events.Any(e => e.Id == EventId.Value))
            {
                EventId = null;

                ErrorMessage = "A kiválasztott esemény nem található.";
            }

            // Csak a kiválasztott esemény
            // programjait töltjük be.

            if (EventId.HasValue)
            {
                Programs = (await _service.GetProgramsAsync(
                    EventId,
                    Search))
                    .OrderBy(p => p.PlanName)
                    .ThenBy(p => p.BeginDate)
                    .ToList();
            }
        }

        // =========================================
        // ÖSSZES PROGRAM TÖRLÉSE
        // =========================================

        public async Task<IActionResult> OnPostDeleteAllAsync(
            int eventId)
        {
            if (eventId <= 0)
            {
                TempData["ErrorMessage"] =
                    "A törléshez válassz ki egy eseményt.";

                return RedirectToPage();
            }

            try
            {
                int? adminUserId = GetAdminUserId();

                int deletedCount =
                    await _service.DeleteAllProgramsAsync(
                        eventId,
                        adminUserId);

                TempData["SuccessMessage"] =
                    $"Sikeresen töröltünk {deletedCount} programalkalmat.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "Adatbázishiba történt a programok törlése során.";
            }

            return RedirectToPage(new
            {
                EventId = eventId
            });
        }

        // =========================================
        // ADMIN AZONOSÍTÓ
        // =========================================

        private int? GetAdminUserId()
        {
            string? value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id
                : null;
        }
    }
}