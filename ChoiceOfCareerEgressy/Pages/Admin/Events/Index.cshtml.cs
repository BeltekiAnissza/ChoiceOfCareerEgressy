using ChoiceOfCareerEgressy.Model.Admin;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Pages.Admin.Events
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ChoiceOfCareerPersistence _context;
        private readonly EventService _eventService;

        public IndexModel(ChoiceOfCareerPersistence context, EventService eventService)
        {
            _context = context;
            _eventService = eventService;
        }

        // =========================================
        // ESEMÉNYEK LISTÁJA
        // =========================================

        public List<Event> Events { get; set; } = new();

        // =========================================
        // ÁTNEVEZÉSHEZ SZÜKSÉGES ADATOK
        // =========================================

        [BindProperty]
        public int EventId { get; set; }

        [BindProperty]
        public string NewEventName { get; set; } = string.Empty;

        // =========================================
        // ÜZENETEK
        // =========================================

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        // =========================================
        // ESEMÉNYEK BETÖLTÉSE
        // =========================================

        public async Task OnGetAsync()
        {
            Events = await _context.Events
                .AsNoTracking()
                .ToListAsync();

            // SQLite esetén a DateTimeOffset szerinti
            // rendezést memóriában végezzük.

            Events = Events
                .OrderByDescending(e => e.EventDate)
                .ToList();
        }

        // =========================================
        // ESEMÉNY ÁTNEVEZÉSE
        // =========================================

        public async Task<IActionResult> OnPostRenameAsync()
        {
            try
            {
                await _eventService.RenameAsync(
                    EventId,
                    NewEventName);

                SuccessMessage =
                    "Az esemény nevét sikeresen módosítottuk.";
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (KeyNotFoundException ex)
            {
                ErrorMessage = ex.Message;
            }

            return RedirectToPage();
        }

        // =========================================
        // ESEMÉNY TÖRLÉSE
        // =========================================

        public async Task<IActionResult> OnPostDeleteAsync(
            int eventId)
        {
            try
            {
                await _eventService.DeleteAsync(eventId);

                SuccessMessage =
                    "Az eseményt és a hozzá tartozó adatokat sikeresen töröltük.";
            }
            catch (KeyNotFoundException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (DbUpdateException)
            {
                ErrorMessage =
                    "Az esemény törlése adatbázis-hiba miatt nem sikerült.";
            }

            return RedirectToPage();
        }

        // =========================================
        // JELENTKEZÉS INDÍTÁSA / LEÁLLÍTÁSA
        // =========================================

        public async Task<IActionResult> OnPostToggleRegistrationAsync(int eventId, bool isEnabled)
        {
            try
            {
                await _eventService.SetRegistrationEnabledAsync(
                    eventId,
                    isEnabled);

                SuccessMessage = isEnabled
                    ? "A jelentkezést sikeresen elindítottuk."
                    : "A jelentkezést sikeresen leállítottuk.";
            }
            catch (KeyNotFoundException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }

            return RedirectToPage();
        }
    }
}