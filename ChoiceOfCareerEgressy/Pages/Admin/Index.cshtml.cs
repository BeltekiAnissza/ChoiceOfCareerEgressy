using ChoiceOfCareerEgressy.Model;
using ChoiceOfCareerEgressy.Model.Admin;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Events
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ChoiceOfCareerPersistence _context;
        private readonly AdminAllDataDeleteService _deleteService;

        public IndexModel(
            ChoiceOfCareerPersistence context,
            AdminAllDataDeleteService deleteService)
        {
            _context = context;
            _deleteService = deleteService;
        }

        public List<EventOverviewDto> Events { get; set; } = new();

        public int TotalProgramCount { get; set; }

        public async Task OnGetAsync()
        {
            await LoadEventsAsync();
        }

        // ============================================
        // JELENTKEZÉS ENGEDÉLYEZÉSE VAGY LETILTÁSA
        // ============================================

        public async Task<IActionResult> OnPostToggleRegistrationAsync(
            int eventId)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (ev == null)
            {
                TempData["Error"] =
                    "A kiválasztott esemény nem található.";

                return RedirectToPage();
            }

            ev.IsRegistrationEnabled = !ev.IsRegistrationEnabled;

            await _context.SaveChangesAsync();

            TempData["Success"] = ev.IsRegistrationEnabled
                ? $"A jelentkezés engedélyezve lett: {ev.EventName}"
                : $"A jelentkezés letiltva lett: {ev.EventName}";

            return RedirectToPage();
        }

        // ============================================
        // ÖSSZES PROGRAM TÖRLÉSE
        // ============================================

        public async Task<IActionResult> OnPostDeleteAllProgramsAsync()
        {
            try
            {
                string? userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out int adminId))
                {
                    TempData["Error"] =
                        "A felhasználó azonosítása sikertelen.";

                    return RedirectToPage();
                }

                int deletedCount =
                    await _deleteService.DeleteAllProgramsAsync(adminId);

                TempData["Success"] =
                    $"Sikeresen töröltünk {deletedCount} programot. " +
                    "A programokhoz tartozó jelentkezések " +
                    "és felügyeletek is törlődtek.";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] =
                    "Nincs jogosultságod a programok törléséhez.";
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "Hiba történt a programok törlése közben. " +
                    "A művelet nem fejeződött be.";
            }

            return RedirectToPage();
        }

        // ============================================
        // ESEMÉNYEK ÉS PROGRAMOK BETÖLTÉSE
        // ============================================

        private async Task LoadEventsAsync()
        {
            TotalProgramCount = await _context.Plans.CountAsync();

            var events = await _context.Events
                .AsNoTracking()
                .AsSplitQuery()
                .Include(e => e.TimeSlots)
                    .ThenInclude(t => t.PlanSessions)
                        .ThenInclude(s => s.Plan)
                            .ThenInclude(p => p.Branch)
                .Include(e => e.TimeSlots)
                    .ThenInclude(t => t.PlanSessions)
                        .ThenInclude(s => s.Plan)
                            .ThenInclude(p => p.Sex)
                .Include(e => e.TimeSlots)
                    .ThenInclude(t => t.PlanSessions)
                        .ThenInclude(s => s.Location)
                .ToListAsync();

            var applicationCounts = await _context.Applications
                .AsNoTracking()
                .Where(a => a.Status == ApplicationStatus.Active)
                .GroupBy(a => a.PlanSessionId)
                .Select(g => new
                {
                    PlanSessionId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var counts = applicationCounts.ToDictionary(
                x => x.PlanSessionId,
                x => x.Count);

            Events = events
                .OrderByDescending(e => e.EventDate)
                .Select(e => new EventOverviewDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDate = e.EventDate,
                    RegistrationStart = e.RegistrationStart,
                    RegistrationEnd = e.RegistrationEnd,
                    IsRegistrationEnabled = e.IsRegistrationEnabled,

                    TimeSlots = e.TimeSlots
                        .OrderBy(t => t.BeginDate)
                        .Select(t => new TimeSlotOverviewDto
                        {
                            TimeSlotName = t.TimeSlotName,
                            BeginDate = t.BeginDate,
                            EndDate = t.EndDate,

                            Programs = t.PlanSessions
                                .OrderBy(s => s.Plan.PlanName)
                                .ThenBy(s => s.Location.LocationName)
                                .Select(s =>
                                {
                                    int registered = counts.GetValueOrDefault(
                                        s.PlanSessionId, 0);

                                    return new ProgramOverviewDto
                                    {
                                        PlanName = s.Plan.PlanName,
                                        Description = s.Plan.PlanDescription,
                                        LocationName = s.Location.LocationName,
                                        Capacity = s.Capacity,
                                        RegisteredCount = registered,
                                        AvailablePlaces = Math.Max(
                                            0, s.Capacity - registered),

                                        SexName = s.Plan.Sex?.SexName
                                            ?? "Unisex",

                                        BranchName = s.Plan.Branch?.BranchName
                                            ?? "Minden ágazat"
                                    };
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList();
        }

        // ============================================
        // DTO-K
        // ============================================

        public class EventOverviewDto
        {
            public int EventId { get; set; }

            public string EventName { get; set; } = "";

            public DateTimeOffset EventDate { get; set; }

            public DateTimeOffset RegistrationStart { get; set; }

            public DateTimeOffset RegistrationEnd { get; set; }

            public bool IsRegistrationEnabled { get; set; }

            public List<TimeSlotOverviewDto> TimeSlots { get; set; } = new();
        }

        public class TimeSlotOverviewDto
        {
            public string TimeSlotName { get; set; } = "";

            public DateTimeOffset BeginDate { get; set; }

            public DateTimeOffset EndDate { get; set; }

            public List<ProgramOverviewDto> Programs { get; set; } = new();
        }

        public class ProgramOverviewDto
        {
            public string PlanName { get; set; } = "";

            public string? Description { get; set; }

            public string LocationName { get; set; } = "";

            public int Capacity { get; set; }

            public int RegisteredCount { get; set; }

            public int AvailablePlaces { get; set; }

            public string SexName { get; set; } = "";

            public string BranchName { get; set; } = "";
        }
    }
}