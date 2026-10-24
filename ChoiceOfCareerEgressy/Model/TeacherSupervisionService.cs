using ChoiceOfCareerEgressy.Dto.UserDto.Teacher;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model
{
    public class TeacherSupervisionService
    {
        private readonly ChoiceOfCareerPersistence _context;

        public TeacherSupervisionService(
            ChoiceOfCareerPersistence context)
        {
            _context = context;
        }

        public async Task<List<TeacherSupervisionEventDto>> GetEventsAsync(
            int userId)
        {
            await CheckTeacherAsync(userId);

            var events = await _context.Events
                .AsNoTracking()
                .Include(e => e.TimeSlots)
                    .ThenInclude(t => t.PlanSessions)
                        .ThenInclude(s => s.Plan)
                .Include(e => e.TimeSlots)
                    .ThenInclude(t => t.PlanSessions)
                        .ThenInclude(s => s.Location)
                .Include(e => e.TimeSlots)
                    .ThenInclude(t => t.PlanSessions)
                        .ThenInclude(s => s.Supervisions)
                .ToListAsync();

            return events
                .OrderBy(e => e.EventDate)
                .Select(e => new TeacherSupervisionEventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDate = e.EventDate,

                    TimeSlots = e.TimeSlots
                        .OrderBy(t => t.BeginDate)
                        .Select(t => new TeacherSupervisionSlotDto
                        {
                            TimeSlotId = t.TimeSlotId,
                            TimeSlotName = t.TimeSlotName,
                            BeginDate = t.BeginDate,
                            EndDate = t.EndDate,

                            Programs = t.PlanSessions
                                .OrderBy(s => s.Plan.PlanName)
                                .Select(s => new TeacherSupervisionProgramDto
                                {
                                    PlanSessionId = s.PlanSessionId,
                                    PlanName = s.Plan.PlanName,
                                    LocationName = s.Location.LocationName,
                                    RequiredSupervisors = s.RequiredSupervisors,
                                    RegisteredSupervisors =
                                        s.Supervisions.Count,
                                    IsMySupervision = s.Supervisions
                                        .Any(x => x.UserId == userId)
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList();
        }

        public async Task RegisterAsync(
            int userId,
            int planSessionId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            await CheckTeacherAsync(userId);

            var session = await _context.PlanSessions
                .Include(s => s.TimeSlot)
                .Include(s => s.Supervisions)
                .FirstOrDefaultAsync(s =>
                    s.PlanSessionId == planSessionId);

            if (session == null)
                throw new InvalidOperationException(
                    "A kiválasztott program nem található.");

            var now = DateTimeOffset.UtcNow;

            if (session.TimeSlot.EndDate <= now)
                throw new InvalidOperationException(
                    "Lezárult programra nem jelentkezhetsz.");

            // A tanár már vállalt felügyeletet ebben az idősávban?
            bool alreadyAssigned = await _context.Supervisions
                .AnyAsync(s =>
                    s.UserId == userId &&
                    (
                        (s.PlanSession != null &&
                         s.PlanSession.TimeSlotId ==
                            session.TimeSlotId)
                        ||
                        (s.SupervisionPlace != null &&
                         s.SupervisionPlace.TimeSlotId ==
                            session.TimeSlotId)
                    ));

            if (alreadyAssigned)
                throw new InvalidOperationException(
                    "Ebben az idősávban már vállaltál felügyeletet.");

            if (session.Supervisions.Count >=
                session.RequiredSupervisors)
            {
                throw new InvalidOperationException(
                    "Erre a programra már elegendő tanár jelentkezett.");
            }

            _context.Supervisions.Add(new Supervision
            {
                UserId = userId,
                PlanSessionId = session.PlanSessionId,
                ApplicationDate = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task CancelAsync(
            int userId,
            int planSessionId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            await CheckTeacherAsync(userId);

            var supervision = await _context.Supervisions
                .Include(s => s.PlanSession)
                    .ThenInclude(s => s!.TimeSlot)
                .FirstOrDefaultAsync(s =>
                    s.UserId == userId &&
                    s.PlanSessionId == planSessionId);

            if (supervision == null)
                throw new InvalidOperationException(
                    "Nincs ilyen felügyeleti jelentkezésed.");

            if (supervision.PlanSession!.TimeSlot.BeginDate
                <= DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException(
                    "A már elkezdődött felügyeletet nem mondhatod le.");
            }

            _context.Supervisions.Remove(supervision);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private async Task CheckTeacherAsync(int userId)
        {
            bool isTeacher = await _context.Users
                .AnyAsync(u =>
                    u.UserId == userId &&
                    u.Role == RoleChoice.Teacher);

            if (!isTeacher)
                throw new UnauthorizedAccessException(
                    "Csak tanár vállalhat felügyeletet.");
        }
    }
}

