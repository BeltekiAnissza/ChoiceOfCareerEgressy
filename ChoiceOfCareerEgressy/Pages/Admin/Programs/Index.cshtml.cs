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
        // ESEMÉNYEK ÉS IDŐSÁVOK
        // =========================================

        public List<AdminProgramOptionDto> Events { get; set; } = new();

        public List<AdminTimeSlotOptionDto> TimeSlots { get; set; } = new();

        // =========================================
        // KERESÉSI FELTÉTELEK
        // =========================================

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EventId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? TimeSlotId { get; set; }

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

            // =========================================
            // ESEMÉNYEK ÉS IDŐSÁVOK BETÖLTÉSE
            // =========================================

            var options = await _service.GetOptionsAsync();

            Events = options.Events;
            TimeSlots = options.TimeSlots;

            // =========================================
            // ALAPÉRTELMEZETT ESEMÉNY KIVÁLASZTÁSA
            // =========================================

            // Ha nincs kiválasztott esemény,
            // automatikusan az elsőt választjuk.

            if (!EventId.HasValue && Events.Any())
            {
                EventId = Events.First().Id;
            }

            // =========================================
            // ESEMÉNY ELLENŐRZÉSE
            // =========================================

            // Csak létező eseményt engedünk.

            if (EventId.HasValue &&
                !Events.Any(e => e.Id == EventId.Value))
            {
                EventId = null;

                ErrorMessage = "A kiválasztott esemény nem található.";
            }

            // =========================================
            // IDŐSÁVOK SZŰRÉSE ESEMÉNY ALAPJÁN
            // =========================================

            if (EventId.HasValue)
            {
                // Kizárólag a kiválasztott eseményhez
                // tartozó idősávokat jelenítjük meg.

                TimeSlots = TimeSlots
                    .Where(t => t.EventId == EventId.Value)
                    .OrderBy(t => t.BeginDate)
                    .ToList();

                // =========================================
                // KIVÁLASZTOTT IDŐSÁV ELLENŐRZÉSE
                // =========================================

                // Ha a kiválasztott idősáv nem tartozik
                // az aktuális eseményhez, megszüntetjük
                // az idősáv szerinti szűrést.

                if (TimeSlotId.HasValue &&
                    !TimeSlots.Any(t =>
                        t.TimeSlotId == TimeSlotId.Value))
                {
                    TimeSlotId = null;
                }
            }
            else
            {
                // Ha nincs érvényes esemény,
                // nem jelenítünk meg idősávokat.

                TimeSlots.Clear();
                TimeSlotId = null;
            }

            // =========================================
            // PROGRAMOK BETÖLTÉSE
            // =========================================

            if (EventId.HasValue)
            {
                Programs = (await _service.GetProgramsAsync(
                    eventId: EventId,
                    search: Search,
                    timeSlotId: TimeSlotId))
                    .OrderBy(p => p.PlanName)
                    .ThenBy(p => p.BeginDate)
                    .ToList();
            }
        }

        // =========================================
        // ÖSSZES PROGRAM TÖRLÉSE
        // =========================================

        public async Task<IActionResult> OnPostDeleteAllAsync(int eventId)
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