using ChoiceOfCareerEgressy.Dto.Homeroom;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model
{
    public class HomeroomService
    {
        private readonly ChoiceOfCareerPersistence _context;

        public HomeroomService(ChoiceOfCareerPersistence context)
        {
            _context = context;
        }

        // =====================================================
        // OSZTÁLYFŐNÖKI JOGOSULTSÁG
        // =====================================================

        public async Task<bool> IsHomeroomTeacherAsync(int teacherId)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.UserId == teacherId &&
                    u.Role == RoleChoice.Teacher &&
                    u.GradeId != null);
        }

        private async Task CheckTeacherAsync(int teacherId)
        {
            if (!await IsHomeroomTeacherAsync(teacherId))
            {
                throw new UnauthorizedAccessException(
                    "Nem vagy osztályfőnök, vagy nincs hozzád osztály rendelve.");
            }
        }

        // =====================================================
        // AKTUÁLIS ESEMÉNY
        // =====================================================

        private async Task<Event?> GetCurrentEventAsync()
        {
            var now = DateTimeOffset.UtcNow;

            // SQLite esetén a DateTimeOffset összehasonlítást
            // memóriában végezzük.
            var events = await _context.Events
                .AsNoTracking()
                .Include(e => e.TimeSlots)
                .ToListAsync();

            return events
                .Where(e =>
                    e.RegistrationStart <= now &&
                    GetEventStart(e) > now)
                .OrderBy(e => GetEventStart(e))
                .FirstOrDefault();
        }

        private static DateTimeOffset GetEventStart(Event ev)
        {
            if (ev.TimeSlots.Count > 0)
            {
                return ev.TimeSlots
                    .Min(t => t.BeginDate);
            }

            return ev.EventDate;
        }

        private async Task<Event> GetEditableEventAsync(int eventId)
        {
            var currentEvent = await GetCurrentEventAsync();

            if (currentEvent == null ||
                currentEvent.EventId != eventId)
            {
                throw new InvalidOperationException(
                    "Ez az esemény jelenleg nem módosítható.");
            }

            return currentEvent;
        }

        // =====================================================
        // OLDAL ADATAINAK LEKÉRÉSE
        // =====================================================

        public async Task<HomeroomPageDto> GetPageAsync(int teacherId, int? eventId)
        {
            await CheckTeacherAsync(teacherId);

            var currentEvent = await GetCurrentEventAsync();

            var result = new HomeroomPageDto();

            if (currentEvent == null)
                return result;

            // Csak az aktuális esemény szerepelhet a listában.
            result.EventId = currentEvent.EventId;

            result.Events.Add(new HomeroomEventDto
            {
                EventId = currentEvent.EventId,
                EventName = currentEvent.EventName,
                EventDate = currentEvent.EventDate
            });

            // A felhasználó nem választhat másik eseményt.
            if (eventId.HasValue &&
                eventId.Value != currentEvent.EventId)
            {
                throw new InvalidOperationException(
                    "A kiválasztott esemény nem aktuális.");
            }

            // A tanárhoz rendelt osztályok.
            var teacherGradeId = await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.UserId == teacherId &&
                        u.Role == RoleChoice.Teacher)
                    .Select(u => u.GradeId)
                    .FirstOrDefaultAsync();

            if (!teacherGradeId.HasValue)
            {
                throw new UnauthorizedAccessException(
                    "Nincs hozzád osztály rendelve.");
            }

            var grades = await _context.Grades
                .AsNoTracking()
                .Include(g => g.Users)
                .Where(g => g.GradeId == teacherGradeId.Value)
                .ToListAsync();

            var slots = await _context.TimeSlots
                .AsNoTracking()
                .Include(t => t.PlanSessions)
                    .ThenInclude(s => s.Plan)
                .Include(t => t.PlanSessions)
                    .ThenInclude(s => s.Location)
                .Where(t => t.EventId == currentEvent.EventId)
                .ToListAsync();

            slots = slots
                .OrderBy(t => t.BeginDate)
                .ToList();

            var applications = await _context.Applications
                .AsNoTracking()
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.Plan)
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.Location)
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.TimeSlot)
                .Where(a =>
                    a.Status == ApplicationStatus.Active &&
                    a.PlanSession.TimeSlot.EventId ==
                        currentEvent.EventId)
                .ToListAsync();

            foreach (var grade in grades.OrderBy(g => g.GradeName))
            {
                var gradeDto = new HomeroomGradeDto
                {
                    GradeId = grade.GradeId,
                    GradeName = grade.GradeName
                };

                foreach (var student in grade.Users
                    .Where(u => u.Role == RoleChoice.Student)
                    .OrderBy(u => u.UserName))
                {
                    var myApplications = applications
                        .Where(a => a.UserId == student.UserId)
                        .ToList();

                    var studentDto = new HomeroomStudentDto
                    {
                        UserId = student.UserId,
                        UserName = student.UserName,
                        EmailAddress = student.EmailAddress,
                        RegistrationCount = myApplications.Count,

                        Applications = myApplications
                            .Select(a => new HomeroomApplicationDto
                            {
                                ApplicationId = a.ApplicationId,
                                PlanSessionId = a.PlanSessionId,
                                PlanName = a.PlanSession.Plan.PlanName,
                                LocationName =
                                    a.PlanSession.Location.LocationName,
                                TimeSlotName =
                                    a.PlanSession.TimeSlot.TimeSlotName,
                                BeginDate =
                                    a.PlanSession.TimeSlot.BeginDate,
                                EndDate =
                                    a.PlanSession.TimeSlot.EndDate
                            })
                            .ToList()
                    };

                    foreach (var slot in slots)
                    {
                        var current = myApplications
                            .FirstOrDefault(a =>
                                a.PlanSession.TimeSlotId ==
                                    slot.TimeSlotId);

                        var registeredPlanIds = myApplications
                            .Where(a =>
                                a.PlanSession.TimeSlotId != slot.TimeSlotId)
                            .Select(a => a.PlanSession.PlanId)
                            .ToHashSet();

                        var slotDto = new HomeroomStudentSlotDto
                        {
                            TimeSlotId = slot.TimeSlotId,
                            TimeSlotName = slot.TimeSlotName,
                            ApplicationId = current?.ApplicationId,
                            SelectedPlanSessionId =
                                current?.PlanSessionId
                        };

                        foreach (var session in slot.PlanSessions
                            .Where(s =>
                                // Ágazat ellenőrzése
                                (s.Plan.BranchId == null ||
                                 s.Plan.BranchId == student.BranchId) &&

                                // Nem ellenőrzése
                                (s.Plan.SexId == null ||
                                 s.Plan.SexId == student.SexId) &&

                                // A másik idősávban már kiválasztott
                                // program ne jelenjen meg.
                                !registeredPlanIds.Contains(s.PlanId))
                            .OrderBy(s => s.Plan.PlanName))
                        {
                            int occupied = applications.Count(a =>
                                a.PlanSessionId == session.PlanSessionId);

                            int available = Math.Max(
                                0,
                                session.Capacity - occupied);

                            // A saját programját akkor is láthatja,
                            // ha időközben betelt.
                            if (available == 0 &&
                                current?.PlanSessionId !=
                                    session.PlanSessionId)
                            {
                                continue;
                            }

                            slotDto.Programs.Add(
                                new HomeroomProgramOptionDto
                                {
                                    PlanSessionId =
                                        session.PlanSessionId,
                                    PlanName = session.Plan.PlanName,
                                    LocationName =
                                        session.Location.LocationName,
                                    TimeSlotName = slot.TimeSlotName,
                                    AvailablePlaces = available
                                });
                        }

                        studentDto.TimeSlots.Add(slotDto);
                    }

                    gradeDto.Students.Add(studentDto);
                }

                result.Grades.Add(gradeDto);
            }

            return result;
        }

        // =====================================================
        // JELENTKEZÉS MÓDOSÍTÁSA
        // =====================================================

        public async Task ChangeApplicationAsync(int teacherId, HomeroomChangeApplicationDto dto)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var student = await GetAuthorizedStudentAsync(teacherId, dto.StudentId);

            await GetEditableEventAsync(dto.EventId);

            var session = await _context.PlanSessions
                .Include(s => s.Plan)
                .Include(s => s.TimeSlot)
                .FirstOrDefaultAsync(s =>
                    s.PlanSessionId == dto.PlanSessionId);

            if (session == null ||
                session.TimeSlotId != dto.TimeSlotId ||
                session.TimeSlot.EventId != dto.EventId)
            {
                throw new InvalidOperationException(
                    "A kiválasztott program nem tartozik az idősávhoz.");
            }

            if ((session.Plan.BranchId != null &&
                 session.Plan.BranchId != student.BranchId) ||
                (session.Plan.SexId != null &&
                 session.Plan.SexId != student.SexId))
            {
                throw new InvalidOperationException(
                    "A diák nem jogosult erre a programra.");
            }

            var existing = await _context.Applications
                .Include(a => a.PlanSession)
                .Where(a =>
                    a.UserId == student.UserId &&
                    a.Status == ApplicationStatus.Active &&
                    a.PlanSession.TimeSlotId == dto.TimeSlotId)
                .ToListAsync();

            if (existing.Count > 1)
            {
                throw new InvalidOperationException(
                    "A diáknak több jelentkezése van ebben az idősávban.");
            }

            var current = existing.SingleOrDefault();

            // Ha már erre a programra jelentkezett,
            // nincs szükség módosításra.
            if (current?.PlanSessionId == session.PlanSessionId)
                return;

            var otherApplications = await _context.Applications
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.TimeSlot)
                .Where(a =>
                    a.UserId == student.UserId &&
                    a.Status == ApplicationStatus.Active &&
                    a.PlanSession.TimeSlot.EventId == dto.EventId &&
                    a.PlanSession.TimeSlotId != dto.TimeSlotId)
                .ToListAsync();

            if (otherApplications.Any(a =>
                a.PlanSession.PlanId == session.PlanId))
            {
                throw new InvalidOperationException(
                    "A diák ugyanazt a programot nem választhatja kétszer.");
            }

            int occupied = await _context.Applications
                .CountAsync(a =>
                    a.PlanSessionId == session.PlanSessionId &&
                    a.Status == ApplicationStatus.Active);

            if (occupied >= session.Capacity)
            {
                throw new InvalidOperationException(
                    "A kiválasztott program betelt.");
            }

            var now = DateTimeOffset.UtcNow;

            // Korábban már létrehozott jelentkezés keresése.
            // A UserId + PlanSessionId egyedi index miatt
            // nem hozhatunk létre ugyanilyen rekordot újra.
            var previousApplication = await _context.Applications
                .FirstOrDefaultAsync(a =>
                    a.UserId == student.UserId &&
                    a.PlanSessionId == session.PlanSessionId);

            if (current != null)
            {
                current.Status = ApplicationStatus.Cancelled;
                current.UpdateDate = now;
            }

            if (previousApplication != null)
            {
                // Korábban lemondott jelentkezés újraaktiválása.
                previousApplication.Status = ApplicationStatus.Active;
                previousApplication.UpdateDate = now;
            }
            else
            {
                _context.Applications.Add(new Application
                {
                    UserId = student.UserId,
                    PlanSessionId = session.PlanSessionId,
                    ApplicationDate = now,
                    Status = ApplicationStatus.Active
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // =====================================================
        // JELENTKEZÉS TÖRLÉSE
        // =====================================================

        public async Task DeleteApplicationAsync(int teacherId, int studentId, int applicationId, int eventId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            await GetAuthorizedStudentAsync(
                teacherId,
                studentId);

            await GetEditableEventAsync(eventId);

            var application = await _context.Applications
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.TimeSlot)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == applicationId &&
                    a.UserId == studentId &&
                    a.Status == ApplicationStatus.Active);

            if (application == null ||
                application.PlanSession.TimeSlot.EventId != eventId)
            {
                throw new InvalidOperationException(
                    "A jelentkezés nem található.");
            }

            application.Status = ApplicationStatus.Cancelled;
            application.UpdateDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // =====================================================
        // SAJÁT OSZTÁLY DIÁKJÁNAK ELLENŐRZÉSE
        // =====================================================

        private async Task<User> GetAuthorizedStudentAsync(int teacherId, int studentId)
        {
            await CheckTeacherAsync(teacherId);

            var teacherGradeId = await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.UserId == teacherId &&
                    u.Role == RoleChoice.Teacher)
                .Select(u => u.GradeId)
                .FirstOrDefaultAsync();

            if (!teacherGradeId.HasValue)
            {
                throw new UnauthorizedAccessException(
                    "Nincs hozzád osztály rendelve.");
            }

            var student = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == studentId &&
                    u.Role == RoleChoice.Student &&
                    u.GradeId == teacherGradeId.Value);

            if (student == null)
            {
                throw new UnauthorizedAccessException(
                    "Csak a saját osztályod diákjait módosíthatod.");
            }

            return student;
        }
    }
}