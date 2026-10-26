using ChoiceOfCareerEgressy.Dto.Homeroom;
using ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model.Admin
{
    public class AdminRegistrationStudentService
    {
        private readonly ChoiceOfCareerPersistence _db;

        public AdminRegistrationStudentService(ChoiceOfCareerPersistence db)
        {
            _db = db;
        }

        public async Task<AdminRegistrationPageDto> GetPageAsync(int? eventId, int? gradeId, int? planSessionId)
        {
            var events = await _db.Events.AsNoTracking().ToListAsync();
            events = events.OrderByDescending(e => e.EventDate).ToList();

            var selectedEvent = events.FirstOrDefault(e => e.EventId == eventId)
                                ?? events.FirstOrDefault();

            var result = new AdminRegistrationPageDto
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

            var grades = await _db.Grades
                .AsNoTracking()
                .Include(g => g.Users)
                .ToListAsync();

            var slots = await _db.TimeSlots
                .AsNoTracking()
                .Include(t => t.PlanSessions)
                    .ThenInclude(s => s.Plan)
                .Include(t => t.PlanSessions)
                    .ThenInclude(s => s.Location)
                .Where(t => t.EventId == selectedEvent.EventId)
                .ToListAsync();

            slots = slots.OrderBy(t => t.BeginDate).ToList();

            var applications = await _db.Applications
                .AsNoTracking()
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.Plan)
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.Location)
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.TimeSlot)
                .Where(a =>
                    a.Status == ApplicationStatus.Active &&
                    a.PlanSession.TimeSlot.EventId == selectedEvent.EventId)
                .ToListAsync();

            foreach (var grade in grades.OrderBy(g => g.GradeName))
            {
                var students = grade.Users
                    .Where(u => u.Role == RoleChoice.Student)
                    .ToList();

                int CountFor(int userId) =>
                    applications.Count(a => a.UserId == userId);

                result.Grades.Add(new AdminGradeSummaryDto
                {
                    GradeId = grade.GradeId,
                    GradeName = grade.GradeName,
                    StudentCount = students.Count,
                    StartedCount = students.Count(s => CountFor(s.UserId) > 0),
                    CompletedCount = students.Count(s =>
                        slots.Count == 2 &&
                        slots.All(t => applications.Any(a =>
                            a.UserId == s.UserId &&
                            a.PlanSession.TimeSlotId == t.TimeSlotId)))
                });
            }

            foreach (var slot in slots)
            {
                foreach (var session in slot.PlanSessions
                    .OrderBy(s => s.Plan.PlanName))
                {
                    result.Programs.Add(new AdminProgramSummaryDto
                    {
                        PlanSessionId = session.PlanSessionId,
                        PlanName = session.Plan.PlanName,
                        TimeSlotName = slot.TimeSlotName,
                        LocationName = session.Location.LocationName,
                        Capacity = session.Capacity,
                        RegisteredCount = applications.Count(a =>
                            a.PlanSessionId == session.PlanSessionId)
                    });
                }
            }

            // Osztály szerinti részletes nézet
            var selectedGrade = grades.FirstOrDefault(g =>
                g.GradeId == gradeId);

            if (selectedGrade != null)
            {
                var gradeDto = new HomeroomGradeDto
                {
                    GradeId = selectedGrade.GradeId,
                    GradeName = selectedGrade.GradeName
                };

                foreach (var student in selectedGrade.Users
                    .Where(u => u.Role == RoleChoice.Student)
                    .OrderBy(u => u.UserName))
                {
                    var mine = applications
                        .Where(a => a.UserId == student.UserId)
                        .ToList();

                    var studentDto = new HomeroomStudentDto
                    {
                        UserId = student.UserId,
                        UserName = student.UserName,
                        EmailAddress = student.EmailAddress,
                        RegistrationCount = mine.Count,
                        Applications = mine.Select(a =>
                            new HomeroomApplicationDto
                            {
                                ApplicationId = a.ApplicationId,
                                PlanSessionId = a.PlanSessionId,
                                PlanName = a.PlanSession.Plan.PlanName,
                                LocationName =
                                    a.PlanSession.Location.LocationName,
                                TimeSlotName =
                                    a.PlanSession.TimeSlot.TimeSlotName,
                                BeginDate = a.PlanSession.TimeSlot.BeginDate,
                                EndDate = a.PlanSession.TimeSlot.EndDate
                            }).ToList()
                    };

                    foreach (var slot in slots)
                    {
                        var current = mine.FirstOrDefault(a =>
                            a.PlanSession.TimeSlotId == slot.TimeSlotId);

                        studentDto.TimeSlots.Add(new HomeroomStudentSlotDto
                        {
                            TimeSlotId = slot.TimeSlotId,
                            TimeSlotName = slot.TimeSlotName,
                            ApplicationId = current?.ApplicationId,
                            SelectedPlanSessionId = current?.PlanSessionId,
                            Programs = GetOptions(
                                slot.TimeSlotId,
                                student,
                                slots,
                                applications,
                                current?.PlanSessionId)
                        });
                    }

                    gradeDto.Students.Add(studentDto);
                }

                result.SelectedGrade = gradeDto;
            }

            // Program szerinti részletes nézet
            var selectedSession = slots
                .SelectMany(t => t.PlanSessions)
                .FirstOrDefault(s => s.PlanSessionId == planSessionId);

            if (selectedSession != null)
            {
                var selectedSlot = slots.Single(t =>
                    t.TimeSlotId == selectedSession.TimeSlotId);

                var detail = new AdminProgramDetailDto
                {
                    PlanSessionId = selectedSession.PlanSessionId,
                    PlanName = selectedSession.Plan.PlanName,
                    TimeSlotName = selectedSlot.TimeSlotName,
                    LocationName = selectedSession.Location.LocationName,
                    Capacity = selectedSession.Capacity,
                    TimeSlotId = selectedSlot.TimeSlotId,
                };

                var studentIds = applications
                    .Where(a => a.PlanSessionId == selectedSession.PlanSessionId)
                    .Select(a => a.UserId)
                    .ToHashSet();

                var enrolledStudents = await _db.Users
                    .AsNoTracking()
                    .Include(u => u.Grade)
                    .Where(u => studentIds.Contains(u.UserId) &&
                                u.Role == RoleChoice.Student)
                    .ToListAsync();

                foreach (var student in enrolledStudents.OrderBy(u => u.UserName))
                {
                    var application = applications.First(a =>
                        a.UserId == student.UserId &&
                        a.PlanSessionId == selectedSession.PlanSessionId);

                    detail.Students.Add(new AdminProgramStudentDto
                    {
                        StudentId = student.UserId,
                        StudentName = student.UserName,
                        GradeName = student.Grade?.GradeName ?? "Nincs osztály",
                        ApplicationId = application.ApplicationId,
                        AvailablePrograms = GetOptions(
                            selectedSlot.TimeSlotId,
                            student,
                            slots,
                            applications,
                            selectedSession.PlanSessionId)
                    });
                }

                result.SelectedProgram = detail;
            }

            return result;
        }

        private static List<HomeroomProgramOptionDto> GetOptions(int timeSlotId, User student, List<TimeSlot> slots, List<Application> applications, int? currentSessionId)
        {
            var slot = slots.First(t => t.TimeSlotId == timeSlotId);

            // Azoknak a programoknak az azonosítói,
            // amelyekre a diák már jelentkezett egy másik időpontban.
            var registeredPlanIds = applications
                .Where(a =>
                    a.UserId == student.UserId &&
                    a.PlanSession.TimeSlotId != timeSlotId)
                .Select(a => a.PlanSession.PlanId)
                .ToHashSet();

            return slot.PlanSessions
                .Where(s =>

                    // Ágazat ellenőrzése
                    (s.Plan.BranchId == null ||
                     s.Plan.BranchId == student.BranchId) &&

                    // Nem ellenőrzése
                    (s.Plan.SexId == null ||
                     s.Plan.SexId == student.SexId) &&

                    // Ne jelenjen meg az a program,
                    // amelyre már jelentkezett egy másik időpontban.
                    !registeredPlanIds.Contains(s.PlanId)
                )
                .Select(s =>
                {
                    int occupied = applications.Count(a =>
                        a.PlanSessionId == s.PlanSessionId);

                    return new
                    {
                        Session = s,
                        Available = Math.Max(0, s.Capacity - occupied)
                    };
                })
                .Where(x =>
                    x.Available > 0 ||
                    x.Session.PlanSessionId == currentSessionId)
                .OrderBy(x => x.Session.Plan.PlanName)
                .Select(x => new HomeroomProgramOptionDto
                {
                    PlanSessionId = x.Session.PlanSessionId,
                    PlanName = x.Session.Plan.PlanName,
                    LocationName = x.Session.Location.LocationName,
                    TimeSlotName = slot.TimeSlotName,
                    AvailablePlaces = x.Available
                })
                .ToList();
        }

        public async Task ChangeAsync(AdminChangeApplicationDto dto)
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            var student = await _db.Users.FirstOrDefaultAsync(u =>
                u.UserId == dto.StudentId &&
                u.Role == RoleChoice.Student);

            if (student == null)
                throw new InvalidOperationException("A diák nem található.");

            var session = await _db.PlanSessions
                .Include(s => s.Plan)
                .Include(s => s.TimeSlot)
                .FirstOrDefaultAsync(s =>
                    s.PlanSessionId == dto.PlanSessionId);

            if (session == null ||
                session.TimeSlotId != dto.TimeSlotId ||
                session.TimeSlot.EventId != dto.EventId)
            {
                throw new InvalidOperationException(
                    "A program nem tartozik a megadott eseményhez és idősávhoz.");
            }

            if (session.Plan.BranchId != null &&
                 session.Plan.BranchId != student.BranchId ||
                session.Plan.SexId != null &&
                 session.Plan.SexId != student.SexId)
            {
                throw new InvalidOperationException(
                    "A diák nem jogosult erre a programra.");
            }

            var currentApplications = await _db.Applications
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.TimeSlot)
                .Where(a =>
                    a.UserId == dto.StudentId &&
                    a.Status == ApplicationStatus.Active &&
                    a.PlanSession.TimeSlot.EventId == dto.EventId)
                .ToListAsync();

            var inSlot = currentApplications
                .Where(a => a.PlanSession.TimeSlotId == dto.TimeSlotId)
                .ToList();

            if (inSlot.Count > 1)
                throw new InvalidOperationException(
                    "A diáknak több aktív jelentkezése van ebben az idősávban.");

            var current = inSlot.SingleOrDefault();

            if (current?.PlanSessionId == session.PlanSessionId)
            {
                await transaction.CommitAsync();
                return;
            }

            if (currentApplications.Any(a =>
                a.PlanSession.TimeSlotId != dto.TimeSlotId &&
                a.PlanSession.PlanId == session.PlanId))
            {
                throw new InvalidOperationException(
                    "Ugyanazt a programot nem választhatja kétszer.");
            }

            var occupied = await _db.Applications.CountAsync(a =>
                a.PlanSessionId == session.PlanSessionId &&
                a.Status == ApplicationStatus.Active);

            if (occupied >= session.Capacity)
                throw new InvalidOperationException("A program betelt.");

            if (current != null)
            {
                current.Status = ApplicationStatus.Cancelled;
                current.UpdateDate = DateTimeOffset.UtcNow;
            }

            _db.Applications.Add(new Application
            {
                UserId = student.UserId,
                PlanSessionId = session.PlanSessionId,
                ApplicationDate = DateTimeOffset.UtcNow,
                Status = ApplicationStatus.Active
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task DeleteAsync(
            int studentId, int applicationId, int eventId)
        {
            var application = await _db.Applications
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

            await _db.SaveChangesAsync();
        }
    }
}
