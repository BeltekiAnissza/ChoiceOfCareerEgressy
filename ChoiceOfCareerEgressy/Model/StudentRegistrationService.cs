
using ChoiceOfCareerEgressy.Dto.UserDto.Student;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model
{
    public class StudentRegistrationService
    {
        private readonly ChoiceOfCareerPersistence _context;

        public StudentRegistrationService(
            ChoiceOfCareerPersistence context)
        {
            _context = context;
        }

        private static bool IsBranchAllowed(
            string? allowedBranches,
            string? studentBranch)
        {
            if (string.IsNullOrWhiteSpace(allowedBranches))
                return true;

            if (string.IsNullOrWhiteSpace(studentBranch))
                return false;

            return allowedBranches
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Any(branch => string.Equals(
                    branch,
                    studentBranch.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSexAllowed(
            string? programSex,
            string? studentSex)
        {
            if (string.IsNullOrWhiteSpace(programSex))
                return true;

            if (string.Equals(
                programSex.Trim(),
                "Unisex",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(
                programSex.Trim(),
                studentSex?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProgramAllowed(
            Plan plan,
            string? studentBranch,
            string? studentSex)
        {
            if (plan.BranchId != null && plan.Branch == null)
                return false;

            if (plan.SexId != null && plan.Sex == null)
                return false;

            return IsBranchAllowed(
                       plan.Branch?.BranchName,
                       studentBranch)
                   &&
                   IsSexAllowed(
                       plan.Sex?.SexName,
                       studentSex);
        }

        // A kiválasztott programok nem fedhetik át egymást.
        private static bool HasOverlappingPrograms(
            List<PlanSession> sessions)
        {
            var ordered = sessions
                .OrderBy(s => s.TimeSlot.BeginDate)
                .ToList();

            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].TimeSlot.BeginDate <
                    ordered[i - 1].TimeSlot.EndDate)
                {
                    return true;
                }
            }

            return false;
        }

        // Minden olyan időszakaszt le kell fedni,
        // amelyben az esemény valamely idősávja aktív.
        private static bool CoversAllTimeSlots(List<TimeSlot> slots, List<PlanSession> selectedSessions)
        {
            if (slots.Count == 0 || selectedSessions.Count == 0)
                return false;

            // Azok az idősávok, amelyek nem tartalmaznak
            // teljes egészében másik, rövidebb idősávot.
            // Ezek lesznek a kötelező alapidősávok.
            var requiredSlots = slots
                .Where(slot => !slots.Any(other =>
                    other.TimeSlotId != slot.TimeSlotId &&
                    other.BeginDate >= slot.BeginDate &&
                    other.EndDate <= slot.EndDate &&
                    (
                        other.BeginDate > slot.BeginDate ||
                        other.EndDate < slot.EndDate
                    )))
                .ToList();

            // Minden kötelező alapidősávot le kell fednie
            // valamelyik kiválasztott programnak.
            foreach (var requiredSlot in requiredSlots)
            {
                bool covered = selectedSessions.Any(session =>
                    session.TimeSlot.BeginDate <= requiredSlot.BeginDate &&
                    session.TimeSlot.EndDate >= requiredSlot.EndDate);

                if (!covered)
                    return false;
            }

            return true;
        }

        private static bool IsRegistrationOpen(
            Event ev,
            DateTimeOffset now)
        {
            return ev.IsRegistrationEnabled &&
                   now >= ev.RegistrationStart &&
                   now <= ev.RegistrationEnd &&
                   ev.TimeSlots.Count > 0;
        }

        public async Task<List<StudentEventDto>> GetEventsAsync(int userId)
        {
            var student = await _context.Users
                .AsNoTracking()
                .Include(u => u.Branch)
                .Include(u => u.Sex)
                .FirstOrDefaultAsync(u =>
                    u.UserId == userId &&
                    u.Role == RoleChoice.Student);

            if (student == null)
            {
                throw new UnauthorizedAccessException(
                    "A felhasználó nem diák.");
            }

            string? studentBranch = student.Branch?.BranchName;
            string? studentSex = student.Sex?.SexName;

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
                    g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var counts = applicationCounts.ToDictionary(
                x => x.Key,
                x => x.Count);

            var myApplications = await _context.Applications
                .AsNoTracking()
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.TimeSlot)
                .Where(a =>
                    a.UserId == userId &&
                    a.Status == ApplicationStatus.Active)
                .ToListAsync();

            DateTimeOffset now = DateTimeOffset.UtcNow;

            return events
                .OrderBy(e => e.EventDate)
                .Select(e =>
                {
                    var slots = e.TimeSlots
                        .OrderBy(t => t.BeginDate)
                        .ToList();

                    var mine = myApplications
                        .Where(a =>
                            a.PlanSession.TimeSlot.EventId ==
                            e.EventId)
                        .ToList();

                    var selectedIds = mine
                        .Select(a => a.PlanSessionId)
                        .ToHashSet();

                    return new StudentEventDto
                    {
                        EventId = e.EventId,
                        EventName = e.EventName,
                        EventDate = e.EventDate,
                        RegistrationStart = e.RegistrationStart,
                        RegistrationEnd = e.RegistrationEnd,

                        CanRegister = IsRegistrationOpen(e, now),
                        HasRegistration = mine.Count > 0,

                        TimeSlots = slots
                            .Select(t => new StudentTimeSlotDto
                            {
                                TimeSlotId = t.TimeSlotId,
                                TimeSlotName = t.TimeSlotName,
                                BeginDate = t.BeginDate,
                                EndDate = t.EndDate,

                                SelectedPlanSessionId = mine
                                    .FirstOrDefault(a =>
                                        a.PlanSession.TimeSlotId ==
                                        t.TimeSlotId)
                                    ?.PlanSessionId,

                                Programs = t.PlanSessions
                                    .Where(s =>
                                        IsProgramAllowed(
                                            s.Plan,
                                            studentBranch,
                                            studentSex)
                                        ||
                                        selectedIds.Contains(
                                            s.PlanSessionId))
                                    .OrderBy(s => s.Plan.PlanName)
                                    .Select(s =>
                                    {
                                        int occupied =
                                            counts.GetValueOrDefault(
                                                s.PlanSessionId, 0);

                                        return new StudentProgramDto
                                        {
                                            PlanSessionId = s.PlanSessionId,
                                            PlanId = s.PlanId,
                                            TimeSlotId = t.TimeSlotId,

                                            PlanName = s.Plan.PlanName,
                                            Description =
                                                s.Plan.PlanDescription,

                                            LocationName =
                                                s.Location.LocationName,

                                            BeginDate = t.BeginDate,
                                            EndDate = t.EndDate,

                                            Capacity = s.Capacity,
                                            AvailablePlaces = Math.Max(
                                                0,
                                                s.Capacity - occupied),

                                            IsSelected =
                                                selectedIds.Contains(
                                                    s.PlanSessionId)
                                        };
                                    })
                                    .ToList()
                            })
                            .ToList()
                    };
                })
                .ToList();
        }

        public async Task RegisterAsync(int userId, StudentRegistrationDto dto)
        {
            await SaveRegistrationAsync(userId, dto, false);
        }

        public async Task UpdateRegistrationAsync( int userId, StudentRegistrationDto dto)
        {
            await SaveRegistrationAsync(userId, dto, true);
        }

        private async Task SaveRegistrationAsync(int userId, StudentRegistrationDto dto, bool isUpdate)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var student = await _context.Users
                .Include(u => u.Branch)
                .Include(u => u.Sex)
                .FirstOrDefaultAsync(u =>
                    u.UserId == userId &&
                    u.Role == RoleChoice.Student);

            if (student == null)
                throw new InvalidOperationException(
                    "Csak diák jelentkezhet.");

            var selectedEvent = await _context.Events
                .Include(e => e.TimeSlots)
                .FirstOrDefaultAsync(e =>
                    e.EventId == dto.EventId);

            if (selectedEvent == null)
                throw new InvalidOperationException(
                    "Az esemény nem található.");

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (!IsRegistrationOpen(selectedEvent, now))
                throw new InvalidOperationException(
                    "A jelentkezés jelenleg nem engedélyezett.");

            var slots = selectedEvent.TimeSlots
                .OrderBy(t => t.BeginDate)
                .ToList();

            if (slots.Any(t => t.BeginDate >= t.EndDate))
                throw new InvalidOperationException(
                    "Az esemény egyik idősávjának időpontjai hibásak.");

            var ids = (dto.SelectedPlanSessionIds ?? new List<int>())
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                throw new InvalidOperationException(
                    "Legalább egy programot választanod kell.");

            if (ids.Count !=
                (dto.SelectedPlanSessionIds?.Count ?? 0))
            {
                throw new InvalidOperationException(
                    "Egy programot csak egyszer választhatsz.");
            }

            var selectedSessions = await _context.PlanSessions
                .Include(s => s.Plan)
                    .ThenInclude(p => p.Branch)
                .Include(s => s.Plan)
                    .ThenInclude(p => p.Sex)
                .Include(s => s.TimeSlot)
                .Where(s => ids.Contains(s.PlanSessionId))
                .ToListAsync();

            if (selectedSessions.Count != ids.Count)
                throw new InvalidOperationException(
                    "Az egyik kiválasztott program nem található.");

            if (selectedSessions.Any(s =>
                s.TimeSlot.EventId != dto.EventId))
            {
                throw new InvalidOperationException(
                    "A kiválasztott program nem ehhez az eseményhez tartozik.");
            }

            if (selectedSessions.Any(s =>
                s.TimeSlot.BeginDate >= s.TimeSlot.EndDate))
            {
                throw new InvalidOperationException(
                    "Az egyik program időpontja hibás.");
            }

            var duplicateNames = selectedSessions
                .GroupBy(
                    s => s.Plan.PlanName.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1);

            if (selectedSessions
                    .Select(s => s.PlanId)
                    .Distinct()
                    .Count() != selectedSessions.Count
                || duplicateNames)
            {
                throw new InvalidOperationException(
                    "Ugyanarra a programra nem lehet kétszer jelentkezni.");
            }

            foreach (var session in selectedSessions)
            {
                if (!IsProgramAllowed(
                    session.Plan,
                    student.Branch?.BranchName,
                    student.Sex?.SexName))
                {
                    throw new InvalidOperationException(
                        $"A(z) {session.Plan.PlanName} programra " +
                        "nem vagy jogosult a nemed vagy az ágazatod alapján.");
                }
            }

            if (HasOverlappingPrograms(selectedSessions))
                throw new InvalidOperationException(
                    "A kiválasztott programok időben ütköznek.");

            if (!CoversAllTimeSlots(slots, selectedSessions))
                throw new InvalidOperationException(
                    "A kiválasztott programok nem fedik le " +
                    "az esemény összes kötelező időszakát.");

            var existingApplications = await _context.Applications
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.TimeSlot)
                .Where(a =>
                    a.UserId == userId &&
                    a.Status == ApplicationStatus.Active &&
                    a.PlanSession.TimeSlot.EventId == dto.EventId)
                .ToListAsync();

            if (!isUpdate && existingApplications.Count > 0)
                throw new InvalidOperationException(
                    "Erre az eseményre már jelentkeztél.");

            if (isUpdate && existingApplications.Count == 0)
                throw new InvalidOperationException(
                    "Nincs módosítható jelentkezésed.");

            foreach (var session in selectedSessions)
            {
                int occupied = await _context.Applications
                    .CountAsync(a =>
                        a.PlanSessionId == session.PlanSessionId &&
                        a.Status == ApplicationStatus.Active);

                bool alreadyRegisteredHere =
                    existingApplications.Any(a =>
                        a.PlanSessionId == session.PlanSessionId);

                if (occupied >= session.Capacity &&
                    !alreadyRegisteredHere)
                {
                    throw new InvalidOperationException(
                        $"A(z) {session.Plan.PlanName} program betelt.");
                }
            }

            if (isUpdate)
            {
                // A változatlan jelentkezéseket megtartjuk.
                // A már nem kiválasztottakat lemondjuk.
                foreach (var application in existingApplications)
                {
                    if (!ids.Contains(application.PlanSessionId))
                    {
                        application.Status = ApplicationStatus.Cancelled;
                        application.UpdateDate = now;
                    }
                }
            }

            var existingSelectedIds = existingApplications
                .Select(a => a.PlanSessionId)
                .ToHashSet();

            foreach (var session in selectedSessions)
            {
                if (existingSelectedIds.Contains(session.PlanSessionId))
                    continue;

                _context.Applications.Add(new Application
                {
                    UserId = userId,
                    PlanSessionId = session.PlanSessionId,
                    ApplicationDate = now,
                    Status = ApplicationStatus.Active
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
    }
}
