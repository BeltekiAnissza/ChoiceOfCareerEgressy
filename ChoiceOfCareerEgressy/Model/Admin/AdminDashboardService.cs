
using ChoiceOfCareerEgressy.Dto.UserDto.Admin.AdminDashboard;
using ChoiceOfCareerEgressy.Model;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model.Admin
{
    public class AdminDashboardService
    {
        private readonly ChoiceOfCareerPersistence _context;

        public AdminDashboardService(ChoiceOfCareerPersistence context)
        {
            _context = context;
        }

        // =========================================
        // 1. ESEMÉNYEK LEKÉRDEZÉSE
        // =========================================

        public async Task<List<DashboardEventDto>> GetAvailableEventsAsync()
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

            return events
                .OrderByDescending(e => e.EventDate)
                .Select(e => new DashboardEventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName
                })
                .ToList();
        }

        // =========================================
        // 2. STATISZTIKAI KÁRTYÁK
        // =========================================

        public async Task<DashboardStatisticsDto> GetStatisticsAsync(  int? eventId)
        {
            var result = new DashboardStatisticsDto();

            // Diákok száma

            result.StudentCount = await _context.Users
                .CountAsync(u => u.Role == RoleChoice.Student);

            // Tanárok száma

            result.TeacherCount = await _context.Users
                .CountAsync(u => u.Role == RoleChoice.Teacher);

            // Események száma

            result.EventCount = await _context.Events.CountAsync();

            // Programok száma

            var sessions = _context.PlanSessions
                .AsNoTracking()
                .AsQueryable();

            if (eventId.HasValue)
            {
                sessions = sessions.Where(s =>
                    s.TimeSlot.EventId == eventId.Value);

                result.ProgramCount = await sessions
                    .Select(s => s.PlanId)
                    .Distinct()
                    .CountAsync();
            }
            else
            {
                result.ProgramCount = await _context.Plans.CountAsync();
            }

            // Jelentkezések száma

            var applications = _context.Applications
                .AsNoTracking()
                .Where(a => a.Status == ApplicationStatus.Active);

            if (eventId.HasValue)
            {
                applications = applications.Where(a =>
                    a.PlanSession.TimeSlot.EventId == eventId.Value);
            }

            result.ApplicationCount = await applications.CountAsync();

            // Tanári felügyeletek száma

            var supervisions = _context.Supervisions
                .AsNoTracking()
                .AsQueryable();

            if (eventId.HasValue)
            {
                supervisions = supervisions.Where(s =>
                    (s.PlanSessionId != null &&
                     s.PlanSession!.TimeSlot.EventId == eventId.Value)
                    ||
                    (s.SupervisionPlaceId != null &&
                     s.SupervisionPlace!.TimeSlot.EventId == eventId.Value));
            }

            result.SupervisionCount = await supervisions.CountAsync();

            return result;
        }

        // =========================================
        // 3. ÁGAZATI DIAGRAM
        // =========================================

        public async Task<BranchChartDto> GetBranchChartAsync(   int? eventId)
        {
            var applications = _context.Applications
                .AsNoTracking()
                .Where(a => a.Status == ApplicationStatus.Active);

            if (eventId.HasValue)
            {
                applications = applications.Where(a =>
                    a.PlanSession.TimeSlot.EventId == eventId.Value);
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

            return new BranchChartDto
            {
                Labels = result
                    .Select(x => x.Name)
                    .ToList(),

                Values = result
                    .Select(x => x.Count)
                    .ToList()
            };
        }

        // =========================================
        // 4. KIHASZNÁLTSÁGI DIAGRAM
        // =========================================

        public async Task<CapacityChartDto> GetCapacityChartAsync(  int? eventId)
        {
            var sessions = _context.PlanSessions
                .AsNoTracking()
                .AsQueryable();

            if (eventId.HasValue)
            {
                sessions = sessions.Where(s =>
                    s.TimeSlot.EventId == eventId.Value);
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

            var chart = new CapacityChartDto();

            foreach (var session in result)
            {
                if (session.ApplicationCount == 0)
                {
                    chart.Empty++;
                }
                else if (session.Capacity <= 0 ||
                         session.ApplicationCount >= session.Capacity)
                {
                    chart.Full++;
                }
                else
                {
                    chart.Partial++;
                }
            }

            return chart;
        }

        // =========================================
        // 5. JELENTKEZÉSEK IDŐBELI ALAKULÁSA
        // =========================================

        public async Task<ApplicationTrendChartDto> GetApplicationTrendAsync( int? eventId)
        {
            var applications = _context.Applications
                .AsNoTracking()
                .Where(a => a.Status == ApplicationStatus.Active);

            if (eventId.HasValue)
            {
                applications = applications.Where(a =>
                    a.PlanSession.TimeSlot.EventId == eventId.Value);
            }

            // SQLite esetén a DateTimeOffset dátumokat
            // először betöltjük, majd memóriában csoportosítjuk.

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

            return new ApplicationTrendChartDto
            {
                Labels = result
                    .Select(x => x.Date.ToString("yyyy.MM.dd"))
                    .ToList(),

                Values = result
                    .Select(x => x.Count)
                    .ToList()
            };
        }

        // =========================================
        // 6. LEGNÉPSZERŰBB PROGRAMOK
        // =========================================

        public async Task<PopularProgramsChartDto> GetPopularProgramsAsync( int? eventId)
        {
            var sessions = _context.PlanSessions
                .AsNoTracking()
                .AsQueryable();

            if (eventId.HasValue)
            {
                sessions = sessions.Where(s =>
                    s.TimeSlot.EventId == eventId.Value);
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

            return new PopularProgramsChartDto
            {
                Programs = result
                    .Select(s => new PopularProgramDto
                    {
                        Name = s.Name,

                        Capacity = s.Capacity,

                        ApplicationCount = s.ApplicationCount,

                        OccupancyPercent = s.Capacity > 0
                            ? (double)s.ApplicationCount / s.Capacity * 100
                            : 0
                    })
                    .ToList()
            };
        }

        // =========================================
        // 7. OSZTÁLYOK AKTIVITÁSA
        // =========================================

        public async Task<GradeActivityChartDto> GetGradeActivityAsync( int? eventId)
        {
            // Diákok lekérdezése

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

            // Aktív jelentkezések lekérdezése

            var applications = _context.Applications
                .AsNoTracking()
                .Where(a =>
                    a.Status == ApplicationStatus.Active &&
                    a.User.Role == RoleChoice.Student);

            if (eventId.HasValue)
            {
                applications = applications.Where(a =>
                    a.PlanSession.TimeSlot.EventId == eventId.Value);
            }

            var activeStudentIds = await applications
                .Select(a => a.UserId)
                .Distinct()
                .ToListAsync();

            var activeStudentSet = activeStudentIds.ToHashSet();

            // Osztályonkénti statisztika

            var result = students
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

            return new GradeActivityChartDto
            {
                Grades = result
            };
        }
    }
}