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

        public async Task<bool> IsHomeroomTeacherAsync(int teacherId)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.UserId == teacherId &&
                    u.Role == RoleChoice.Teacher &&
                    u.GradeId != null);
        }

        public async Task<HomeroomPageDto> GetPageAsync( int teacherId, int? eventId)
        {
            await CheckTeacherAsync(teacherId);

            var events = await _context.Events
                .AsNoTracking()
                .ToListAsync();

            events = events
                .OrderByDescending(e => e.EventDate)
                .ToList();

            var selectedEvent = events.FirstOrDefault(e =>
                e.EventId == eventId) ?? events.FirstOrDefault();

            var result = new HomeroomPageDto
            {
                EventId = selectedEvent?.EventId ?? 0,
                Events = events.Select(e => new HomeroomEventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDate = e.EventDate
                }).ToList()
            };

            if (selectedEvent == null)
                return result;

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
                    "Ehhez a tanárhoz nincs osztály rendelve.");
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
                .Where(t => t.EventId == selectedEvent.EventId)
                .ToListAsync();

            slots = slots.OrderBy(t => t.BeginDate).ToList();

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
                        selectedEvent.EventId)
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

                        Applications = myApplications.Select(a =>
                            new HomeroomApplicationDto
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
                            }).ToList()
                    };

                    foreach (var slot in slots)
                    {
                        var current = myApplications
                            .FirstOrDefault(a =>
                                a.PlanSession.TimeSlotId ==
                                    slot.TimeSlotId);

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
                                (s.Plan.BranchId == null ||
                                 s.Plan.BranchId == student.BranchId) &&
                                (s.Plan.SexId == null ||
                                 s.Plan.SexId == student.SexId))
                            .OrderBy(s => s.Plan.PlanName))
                        {
                            int occupied = applications.Count(a =>
                                a.PlanSessionId == session.PlanSessionId);

                            int available = Math.Max(
                                0, session.Capacity - occupied);

                            // A saját jelenlegi helye akkor is
                            // kiválasztható, ha a program betelt.
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

        public async Task ChangeApplicationAsync(
            int teacherId,
            HomeroomChangeApplicationDto dto)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var student = await GetAuthorizedStudentAsync( teacherId, dto.StudentId);

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
                throw new InvalidOperationException(
                    "A diáknak több jelentkezése van ebben az idősávban.");

            var current = existing.SingleOrDefault();

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

            var occupied = await _context.Applications
                .CountAsync(a =>
                    a.PlanSessionId == session.PlanSessionId &&
                    a.Status == ApplicationStatus.Active);

            if (occupied >= session.Capacity)
                throw new InvalidOperationException(
                    "A kiválasztott program betelt.");

            if (current != null)
            {
                current.Status = ApplicationStatus.Cancelled;
                current.UpdateDate = DateTimeOffset.UtcNow;
            }

            _context.Applications.Add(new Application
            {
                UserId = student.UserId,
                PlanSessionId = session.PlanSessionId,
                ApplicationDate = DateTimeOffset.UtcNow,
                Status = ApplicationStatus.Active
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task DeleteApplicationAsync(
            int teacherId,
            int studentId,
            int applicationId,
            int eventId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            await GetAuthorizedStudentAsync(teacherId, studentId);

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

        private async Task CheckTeacherAsync(int teacherId)
        {
            bool isHomeroomTeacher = await _context.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.UserId == teacherId &&
                    u.Role == RoleChoice.Teacher &&
                    u.GradeId != null);

            if (!isHomeroomTeacher)
            {
                throw new UnauthorizedAccessException(
                    "A felhasználó nem osztályfőnök, " +
                    "vagy nincs hozzá osztály rendelve.");
            }
        }

        private async Task<User> GetAuthorizedStudentAsync( int teacherId, int studentId)
        {
            await CheckTeacherAsync(teacherId);

            var teacherGradeId = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == teacherId)
                .Select(u => u.GradeId)
                .FirstOrDefaultAsync();

            if (!teacherGradeId.HasValue)
            {
                throw new UnauthorizedAccessException(
                    "Nem vagy osztályfőnök.");
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
