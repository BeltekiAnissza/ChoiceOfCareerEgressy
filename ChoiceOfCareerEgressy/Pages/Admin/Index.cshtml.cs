using ChoiceOfCareerEgressy.Persistence;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ChoiceOfCareerPersistence _context;

        public IndexModel(ChoiceOfCareerPersistence context)
        {
            _context = context;
        }

        // ============================================
        // ESEMÉNYSZŰRÉS
        // ============================================

        [BindProperty(SupportsGet = true, Name = "eventId")]
        public int? SelectedEventId { get; set; }

        public List<EventDto> AvailableEvents { get; set; } = new();

        // ============================================
        // STATISZTIKAI KÁRTYÁK
        // ============================================

        public int StudentCount { get; set; }

        public int TeacherCount { get; set; }

        public int ProgramCount { get; set; }

        public int ApplicationCount { get; set; }

        public int EventCount { get; set; }

        public int SupervisionCount { get; set; }

        // ============================================
        // ÁGAZATI DIAGRAM
        // ============================================

        public List<string> BranchChartLabels { get; set; } = new();

        public List<int> BranchChartValues { get; set; } = new();

        // ============================================
        // KIHASZNÁLTSÁGI DIAGRAM
        // ============================================

        public List<int> CapacityChartValues { get; set; } = new();

        // ============================================
        // JELENTKEZÉSI DIAGRAM
        // ============================================

        public List<string> TrendChartLabels { get; set; } = new();

        public List<int> TrendChartValues { get; set; } = new();

        // ============================================
        // RÉSZLETES STATISZTIKÁK
        // ============================================

        public List<PopularProgramDto> PopularPrograms { get; set; } = new();

        public List<GradeStatisticDto> GradeStatistics { get; set; } = new();


        // ============================================
        // OLDAL BETÖLTÉSE
        // ============================================

        public async Task OnGetAsync()
        {
            await LoadEventsAsync();

            await LoadStatisticsAsync();

            await LoadBranchChartAsync();

            await LoadCapacityChartAsync();

            await LoadApplicationTrendAsync();

            await LoadPopularProgramsAsync();

            await LoadGradeStatisticsAsync();
        }


        // ============================================
        // ESEMÉNYEK BETÖLTÉSE
        // ============================================

        private async Task LoadEventsAsync()
        {
            var events = await _context.Events
                .AsNoTracking()
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.EventDate
                })
                .ToListAsync();

            AvailableEvents = events
                .OrderByDescending(e => e.EventDate)
                .Select(e => new EventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName
                })
                .ToList();

            if (SelectedEventId.HasValue &&
                !AvailableEvents.Any(e =>
                    e.EventId == SelectedEventId.Value))
            {
                SelectedEventId = null;
            }
        }


        // ============================================
        // STATISZTIKAI KÁRTYÁK
        // ============================================

        private async Task LoadStatisticsAsync()
        {
            StudentCount = await _context.Users
                .CountAsync(u => u.Role == RoleChoice.Student);

            TeacherCount = await _context.Users
                .CountAsync(u => u.Role == RoleChoice.Teacher);

            EventCount = await _context.Events.CountAsync();

            var sessions = _context.PlanSessions
                .AsNoTracking()
                .AsQueryable();

            var applications = _context.Applications
                .AsNoTracking()
                .Where(a => a.Status == ApplicationStatus.Active);

            var supervisions = _context.Supervisions
                .AsNoTracking()
                .AsQueryable();

            if (SelectedEventId.HasValue)
            {
                int eventId = SelectedEventId.Value;

                sessions = sessions.Where(s =>
                    s.TimeSlot.EventId == eventId);

                applications = applications.Where(a =>
                    a.PlanSession.TimeSlot.EventId == eventId);

                supervisions = supervisions.Where(s =>
                    (s.PlanSessionId != null &&
                     s.PlanSession!.TimeSlot.EventId == eventId)
                    ||
                    (s.SupervisionPlaceId != null &&
                     s.SupervisionPlace!.TimeSlot.EventId == eventId));
            }

            if (SelectedEventId.HasValue)
            {
                ProgramCount = await sessions
                    .Select(s => s.PlanId)
                    .Distinct()
                    .CountAsync();
            }
            else
            {
                ProgramCount = await _context.Plans.CountAsync();
            }

            ApplicationCount = await applications.CountAsync();

            SupervisionCount = await supervisions.CountAsync();
        }


        // ============================================
        // JELENTKEZÉSEK ÁGAZATONKÉNT
        // ============================================

        private async Task LoadBranchChartAsync()
        {
            var applications = _context.Applications
                .AsNoTracking()
                .Where(a =>
                    a.Status == ApplicationStatus.Active);

            if (SelectedEventId.HasValue)
            {
                int eventId = SelectedEventId.Value;

                applications = applications.Where(a =>
                    a.PlanSession.TimeSlot.EventId == eventId);
            }

            var result = await applications
                .GroupBy(a => new
                {
                    a.User.BranchId,
                    BranchName = a.User.Branch != null
                        ? a.User.Branch.BranchName
                        : "Nincs ágazat"
                })
                .Select(g => new
                {
                    Name = g.Key.BranchName,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            BranchChartLabels = result
                .Select(x => x.Name)
                .ToList();

            BranchChartValues = result
                .Select(x => x.Count)
                .ToList();
        }


        // ============================================
        // PROGRAMALKALMAK KIHASZNÁLTSÁGA
        // ============================================

        private async Task LoadCapacityChartAsync()
        {
            var sessions = _context.PlanSessions
                .AsNoTracking()
                .AsQueryable();

            if (SelectedEventId.HasValue)
            {
                int eventId = SelectedEventId.Value;

                sessions = sessions.Where(s =>
                    s.TimeSlot.EventId == eventId);
            }

            var result = await sessions
                .Select(s => new
                {
                    s.Capacity,

                    ApplicationCount = s.Applications
                        .Count(a =>
                            a.Status == ApplicationStatus.Active)
                })
                .ToListAsync();

            int full = 0;
            int partial = 0;
            int empty = 0;

            foreach (var session in result)
            {
                if (session.ApplicationCount == 0)
                {
                    empty++;
                }
                else if (session.Capacity <= 0 ||
                         session.ApplicationCount >= session.Capacity)
                {
                    full++;
                }
                else
                {
                    partial++;
                }
            }

            CapacityChartValues = new List<int>
            {
                full,
                partial,
                empty
            };
        }


        // ============================================
        // JELENTKEZÉSEK IDŐBELI ALAKULÁSA
        // ============================================

        private async Task LoadApplicationTrendAsync()
        {
            var applications = _context.Applications
                .AsNoTracking()
                .Where(a =>
                    a.Status == ApplicationStatus.Active);

            if (SelectedEventId.HasValue)
            {
                int eventId = SelectedEventId.Value;

                applications = applications.Where(a =>
                    a.PlanSession.TimeSlot.EventId == eventId);
            }

            var dates = await applications
                .Select(a => a.ApplicationDate)
                .ToListAsync();

            var result = dates
                .GroupBy(d => d.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            TrendChartLabels = result
                .Select(x => x.Date.ToString("yyyy.MM.dd"))
                .ToList();

            TrendChartValues = result
                .Select(x => x.Count)
                .ToList();
        }


        // ============================================
        // LEGNÉPSZERŰBB PROGRAMALKALMAK
        // ============================================

        private async Task LoadPopularProgramsAsync()
        {
            var sessions = _context.PlanSessions
                .AsNoTracking()
                .AsQueryable();

            if (SelectedEventId.HasValue)
            {
                int eventId = SelectedEventId.Value;

                sessions = sessions.Where(s =>
                    s.TimeSlot.EventId == eventId);
            }

            var result = await sessions
                .Select(s => new
                {
                    Name = s.Plan.PlanName,
                    s.Capacity,

                    ApplicationCount = s.Applications
                        .Count(a =>
                            a.Status == ApplicationStatus.Active)
                })
                .OrderByDescending(s => s.ApplicationCount)
                .ThenBy(s => s.Name)
                .Take(10)
                .ToListAsync();

            PopularPrograms = result
                .Select(s => new PopularProgramDto
                {
                    Name = s.Name,

                    Capacity = s.Capacity,

                    ApplicationCount = s.ApplicationCount,

                    OccupancyPercent = s.Capacity > 0
                        ? (double)s.ApplicationCount / s.Capacity * 100
                        : 0
                })
                .ToList();
        }


        // ============================================
        // OSZTÁLYOK AKTIVITÁSA
        // ============================================

        private async Task LoadGradeStatisticsAsync()
        {
            var students = await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.Role == RoleChoice.Student &&
                    u.GradeId != null)
                .Select(u => new
                {
                    u.UserId,
                    u.GradeId,
                    GradeName = u.Grade!.GradeName
                })
                .ToListAsync();

            var applications = _context.Applications
                .AsNoTracking()
                .Where(a =>
                    a.Status == ApplicationStatus.Active &&
                    a.User.Role == RoleChoice.Student);

            if (SelectedEventId.HasValue)
            {
                int eventId = SelectedEventId.Value;

                applications = applications.Where(a =>
                    a.PlanSession.TimeSlot.EventId == eventId);
            }

            var activeStudentIds = await applications
                .Select(a => a.UserId)
                .Distinct()
                .ToListAsync();

            var activeStudentSet = activeStudentIds.ToHashSet();

            GradeStatistics = students
                .GroupBy(s => new
                {
                    s.GradeId,
                    s.GradeName
                })
                .Select(g =>
                {
                    int studentCount = g.Count();

                    int activeCount = g.Count(s =>
                        activeStudentSet.Contains(s.UserId));

                    return new GradeStatisticDto
                    {
                        GradeName = g.Key.GradeName,

                        StudentCount = studentCount,

                        ActiveStudentCount = activeCount,

                        ActivityPercent = studentCount > 0
                            ? (double)activeCount / studentCount * 100
                            : 0
                    };
                })
                .OrderBy(g => g.GradeName)
                .ToList();
        }


        // ============================================
        // DTO OSZTÁLYOK
        // ============================================

        public class EventDto
        {
            public int EventId { get; set; }

            public string EventName { get; set; } = string.Empty;
        }


        public class PopularProgramDto
        {
            public string Name { get; set; } = string.Empty;

            public int ApplicationCount { get; set; }

            public int Capacity { get; set; }

            public double OccupancyPercent { get; set; }
        }


        public class GradeStatisticDto
        {
            public string GradeName { get; set; } = string.Empty;

            public int StudentCount { get; set; }

            public int ActiveStudentCount { get; set; }

            public double ActivityPercent { get; set; }
        }
    }
}