
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

        // =========================================
        // SEGÉDFÜGGVÉNYEK
        // =========================================

        private static bool IsBranchAllowed(
            string? allowedBranches,
            string? studentBranch)
        {
            // Üres ágazat = minden diák számára elérhető.
            if (string.IsNullOrWhiteSpace(allowedBranches))
                return true;

            // Ha a program ágazathoz kötött,
            // de a diáknak nincs ágazata, nem jogosult.
            if (string.IsNullOrWhiteSpace(studentBranch))
                return false;

            string normalizedStudentBranch = studentBranch.Trim();

            return allowedBranches
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Any(branch => string.Equals(
                    branch,
                    normalizedStudentBranch,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSexAllowed(
            string? programSex,
            string? studentSex)
        {
            // A nem nélküli programot általánosnak tekintjük.
            if (string.IsNullOrWhiteSpace(programSex))
                return true;

            string requiredSex = programSex.Trim();

            // Unisex = minden diák.
            if (string.Equals(
                requiredSex,
                "Unisex",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Nő vagy férfi = egyeznie kell.
            return string.Equals(
                requiredSex,
                studentSex?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProgramAllowed(
            Plan plan,
            string? studentBranch,
            string? studentSex)
        {
            string? allowedBranches = plan.Branch?.BranchName;
            string? requiredSex = plan.Sex?.SexName;

            // Ha van BranchId, de a kapcsolat nem található,
            // ne tekintsük korlátozás nélkülinek.
            if (plan.BranchId != null && plan.Branch == null)
                return false;

            if (plan.SexId != null && plan.Sex == null)
                return false;

            return IsBranchAllowed(
                       allowedBranches,
                       studentBranch)
                   &&
                   IsSexAllowed(
                       requiredSex,
                       studentSex);
        }

        // =========================================
        // ESEMÉNYEK ÉS PROGRAMOK LEKÉRÉSE
        // =========================================

        public async Task<List<StudentEventDto>> GetEventsAsync(
            int userId)
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

            var activeApplications = await _context.Applications
                .AsNoTracking()
                .Where(a =>
                    a.Status == ApplicationStatus.Active)
                .Select(a => new
                {
                    a.PlanSessionId
                })
                .ToListAsync();

            var myApplications = await _context.Applications
                .AsNoTracking()
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.TimeSlot)
                .Where(a =>
                    a.UserId == userId &&
                    a.Status == ApplicationStatus.Active)
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;

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

                    bool registrationOpen =
                        e.IsRegistrationEnabled &&
                        now >= e.RegistrationStart &&
                        now <= e.RegistrationEnd &&
                        slots.Count == 2;

                    return new StudentEventDto
                    {
                        EventId = e.EventId,
                        EventName = e.EventName,
                        EventDate = e.EventDate,
                        RegistrationStart = e.RegistrationStart,
                        RegistrationEnd = e.RegistrationEnd,

                        CanRegister = registrationOpen,

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
                                            studentSex))
                                    .OrderBy(s => s.Plan.PlanName)
                                    .Select(s =>
                                    {
                                        int occupied =
                                            activeApplications.Count(a =>
                                                a.PlanSessionId ==
                                                s.PlanSessionId);

                                        return new StudentProgramDto
                                        {
                                            PlanSessionId =
                                                s.PlanSessionId,

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
                                                s.Capacity - occupied)
                                        };
                                    })
                                    .ToList()
                            })
                            .ToList()
                    };
                })
                .ToList();
        }

        // =========================================
        // ÚJ JELENTKEZÉS
        // =========================================

        public async Task RegisterAsync(
            int userId,
            StudentRegistrationDto dto)
        {
            await SaveRegistrationAsync(
                userId,
                dto,
                false);
        }

        // =========================================
        // JELENTKEZÉS MÓDOSÍTÁSA
        // =========================================

        public async Task UpdateRegistrationAsync(
            int userId,
            StudentRegistrationDto dto)
        {
            await SaveRegistrationAsync(
                userId,
                dto,
                true);
        }

        // =========================================
        // JELENTKEZÉS MENTÉSE
        // =========================================

        private async Task SaveRegistrationAsync(
            int userId,
            StudentRegistrationDto dto,
            bool isUpdate)
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
            {
                throw new InvalidOperationException(
                    "Csak diák jelentkezhet.");
            }

            string? studentBranch = student.Branch?.BranchName;
            string? studentSex = student.Sex?.SexName;

            var selectedEvent = await _context.Events
                .Include(e => e.TimeSlots)
                .FirstOrDefaultAsync(e =>
                    e.EventId == dto.EventId);

            if (selectedEvent == null)
            {
                throw new InvalidOperationException(
                    "Az esemény nem található.");
            }

            var now = DateTimeOffset.UtcNow;

            if (!selectedEvent.IsRegistrationEnabled ||
                now < selectedEvent.RegistrationStart ||
                now > selectedEvent.RegistrationEnd)
            {
                throw new InvalidOperationException(
                    "A jelentkezés jelenleg nem engedélyezett.");
            }

            var slots = selectedEvent.TimeSlots
                .OrderBy(t => t.BeginDate)
                .ToList();

            if (slots.Count != 2)
            {
                throw new InvalidOperationException(
                    "Az eseménynek pontosan két idősávval kell rendelkeznie.");
            }

            if (dto.FirstPlanSessionId ==
                dto.SecondPlanSessionId)
            {
                throw new InvalidOperationException(
                    "Két különböző programot kell választani.");
            }

            var selectedSessions = await _context.PlanSessions
                .Include(s => s.Plan)
                    .ThenInclude(p => p.Branch)
                .Include(s => s.Plan)
                    .ThenInclude(p => p.Sex)
                .Include(s => s.TimeSlot)
                .Where(s =>
                    s.PlanSessionId == dto.FirstPlanSessionId ||
                    s.PlanSessionId == dto.SecondPlanSessionId)
                .ToListAsync();

            if (selectedSessions.Count != 2)
            {
                throw new InvalidOperationException(
                    "Az egyik kiválasztott program nem található.");
            }

            var first = selectedSessions.SingleOrDefault(s =>
                s.PlanSessionId == dto.FirstPlanSessionId);

            var second = selectedSessions.SingleOrDefault(s =>
                s.PlanSessionId == dto.SecondPlanSessionId);

            if (first == null ||
                second == null ||
                first.TimeSlotId != slots[0].TimeSlotId ||
                second.TimeSlotId != slots[1].TimeSlotId)
            {
                throw new InvalidOperationException(
                    "Mindkét idősávból egy-egy programot kell választani.");
            }

            // =========================================
            // DUPLIKÁLT PROGRAM ELLENŐRZÉSE
            // =========================================

            if (first.PlanId == second.PlanId ||
                string.Equals(
                    first.Plan.PlanName.Trim(),
                    second.Plan.PlanName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Ugyanarra a programra nem lehet kétszer jelentkezni.");
            }

            // =========================================
            // NEM ÉS ÁGAZAT ELLENŐRZÉSE
            // =========================================

            foreach (var session in selectedSessions)
            {
                if (!IsProgramAllowed(
                    session.Plan,
                    studentBranch,
                    studentSex))
                {
                    throw new InvalidOperationException(
                        $"A(z) {session.Plan.PlanName} programra " +
                        "nem vagy jogosult a nemed vagy az ágazatod alapján.");
                }
            }

            // =========================================
            // MEGLÉVŐ JELENTKEZÉSEK
            // =========================================

            var existingApplications = await _context.Applications
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.TimeSlot)
                .Where(a =>
                    a.UserId == userId &&
                    a.Status == ApplicationStatus.Active &&
                    a.PlanSession.TimeSlot.EventId == dto.EventId)
                .ToListAsync();

            if (!isUpdate && existingApplications.Count > 0)
            {
                throw new InvalidOperationException(
                    "Erre az eseményre már jelentkeztél.");
            }

            if (isUpdate)
            {
                if (existingApplications.Count != 2 ||
                    existingApplications
                        .Select(a => a.PlanSession.TimeSlotId)
                        .Distinct()
                        .Count() != 2 ||
                    existingApplications.Any(a =>
                        !slots.Any(t =>
                            t.TimeSlotId ==
                            a.PlanSession.TimeSlotId)))
                {
                    throw new InvalidOperationException(
                        "A meglévő jelentkezés hiányos vagy hibás. " +
                        "Kérd az osztályfőnök segítségét.");
                }
            }

            // =========================================
            // KAPACITÁS ELLENŐRZÉSE
            // =========================================

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

            // =========================================
            // MÓDOSÍTÁS
            // =========================================

            if (isUpdate)
            {
                foreach (var session in selectedSessions)
                {
                    var application =
                        existingApplications.Single(a =>
                            a.PlanSession.TimeSlotId ==
                            session.TimeSlotId);

                    if (application.PlanSessionId !=
                        session.PlanSessionId)
                    {
                        application.PlanSessionId =
                            session.PlanSessionId;

                        application.UpdateDate = now;
                    }
                }
            }
            else
            {
                // =========================================
                // ÚJ JELENTKEZÉS
                // =========================================

                foreach (var session in selectedSessions)
                {
                    _context.Applications.Add(
                        new Application
                        {
                            UserId = userId,
                            PlanSessionId = session.PlanSessionId,
                            ApplicationDate = now,
                            Status = ApplicationStatus.Active
                        });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
    }
}
