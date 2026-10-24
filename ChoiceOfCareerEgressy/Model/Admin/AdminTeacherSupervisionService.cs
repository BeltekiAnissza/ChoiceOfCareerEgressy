using ChoiceOfCareerEgressy.Dto.Homeroom;
using ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model.Admin
{
    public class AdminTeacherSupervisionService
    {
        private readonly ChoiceOfCareerPersistence _db;

        public AdminTeacherSupervisionService(
            ChoiceOfCareerPersistence db)
        {
            _db = db;
        }

        public async Task<AdminTeacherSupervisionPageDto> GetPageAsync(
            int? eventId)
        {
            var events = await _db.Events
                .AsNoTracking()
                .ToListAsync();

            events = events
                .OrderByDescending(e => e.EventDate)
                .ToList();

            var selectedEvent = events
                .FirstOrDefault(e => e.EventId == eventId)
                ?? events.FirstOrDefault();

            var result = new AdminTeacherSupervisionPageDto
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

            var teachers = await _db.Users
                .AsNoTracking()
                .Where(u => u.Role == RoleChoice.Teacher)
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var slots = await _db.TimeSlots
                .AsNoTracking()
                .Include(t => t.PlanSessions)
                    .ThenInclude(s => s.Plan)
                .Include(t => t.PlanSessions)
                    .ThenInclude(s => s.Location)
                .Where(t => t.EventId == selectedEvent.EventId)
                .ToListAsync();

            slots = slots
                .OrderBy(t => t.BeginDate)
                .ToList();

            var slotIds = slots
                .Select(t => t.TimeSlotId)
                .ToList();

            var supervisions = await _db.Supervisions
                .AsNoTracking()
                .Include(s => s.PlanSession)
                    .ThenInclude(ps => ps!.Plan)
                .Include(s => s.PlanSession)
                    .ThenInclude(ps => ps!.Location)
                .Include(s => s.SupervisionPlace)
                .Where(s =>
                    (s.PlanSessionId != null &&
                     slotIds.Contains(s.PlanSession!.TimeSlotId)) ||
                    (s.SupervisionPlaceId != null &&
                     slotIds.Contains(s.SupervisionPlace!.TimeSlotId)))
                .ToListAsync();

            foreach (var teacher in teachers)
            {
                var mine = supervisions
                    .Where(s => s.UserId == teacher.UserId)
                    .ToList();

                var teacherDto = new AdminTeacherDto
                {
                    UserId = teacher.UserId,
                    UserName = teacher.UserName,
                    EmailAddress = teacher.EmailAddress,
                    SupervisionCount = mine.Count
                };

                foreach (var slot in slots)
                {
                    var current = mine.FirstOrDefault(s =>
                        s.PlanSession?.TimeSlotId == slot.TimeSlotId ||
                        s.SupervisionPlace?.TimeSlotId == slot.TimeSlotId);

                    var slotDto = new AdminTeacherSlotDto
                    {
                        TimeSlotId = slot.TimeSlotId,
                        TimeSlotName = slot.TimeSlotName,
                        SupervisionId = current?.SupervisionId,
                        SelectedPlanSessionId = current?.PlanSessionId,
                        CurrentProgramName =
                            current?.PlanSession?.Plan.PlanName ??
                            current?.SupervisionPlace?.Name,
                        CurrentLocationName =
                            current?.PlanSession?.Location.LocationName ??
                            "Általános felügyeleti hely"
                    };

                    foreach (var session in slot.PlanSessions
                        .OrderBy(s => s.Plan.PlanName))
                    {
                        int registered = supervisions.Count(s =>
                            s.PlanSessionId == session.PlanSessionId);

                        // A már kiválasztott program betelt
                        // állapotban is maradjon látható.
                        if (registered >= session.RequiredSupervisors &&
                            current?.PlanSessionId != session.PlanSessionId)
                        {
                            continue;
                        }

                        slotDto.Programs.Add(
                            new AdminSupervisionOptionDto
                            {
                                PlanSessionId = session.PlanSessionId,
                                PlanName = session.Plan.PlanName,
                                LocationName =
                                    session.Location.LocationName,
                                RequiredSupervisors =
                                    session.RequiredSupervisors,
                                RegisteredSupervisors = registered
                            });
                    }

                    teacherDto.TimeSlots.Add(slotDto);
                }

                result.Teachers.Add(teacherDto);
            }

            return result;
        }

        public async Task SetAsync(
            AdminSetTeacherSupervisionDto dto)
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            bool teacherExists = await _db.Users.AnyAsync(u =>
                u.UserId == dto.TeacherId &&
                u.Role == RoleChoice.Teacher);

            if (!teacherExists)
                throw new InvalidOperationException(
                    "A kiválasztott tanár nem található.");

            var session = await _db.PlanSessions
                .Include(s => s.TimeSlot)
                .FirstOrDefaultAsync(s =>
                    s.PlanSessionId == dto.PlanSessionId);

            if (session == null ||
                session.TimeSlotId != dto.TimeSlotId ||
                session.TimeSlot.EventId != dto.EventId)
            {
                throw new InvalidOperationException(
                    "A program nem tartozik a megadott idősávhoz.");
            }

            // A programhoz és az általános felügyeleti
            // helyekhez tartozó jelentkezéseket is vizsgáljuk.
            var existing = await _db.Supervisions
                .Include(s => s.PlanSession)
                .Include(s => s.SupervisionPlace)
                .Where(s =>
                    s.UserId == dto.TeacherId &&
                    (
                        (s.PlanSessionId != null &&
                         s.PlanSession!.TimeSlotId == dto.TimeSlotId) ||
                        (s.SupervisionPlaceId != null &&
                         s.SupervisionPlace!.TimeSlotId == dto.TimeSlotId)
                    ))
                .ToListAsync();

            if (existing.Count > 1)
                throw new InvalidOperationException(
                    "A tanárhoz több felügyelet tartozik ebben az idősávban.");

            var current = existing.SingleOrDefault();

            if (current?.PlanSessionId == dto.PlanSessionId)
            {
                await transaction.CommitAsync();
                return;
            }

            int registered = await _db.Supervisions.CountAsync(s =>
                s.PlanSessionId == dto.PlanSessionId);

            if (registered >= session.RequiredSupervisors)
            {
                throw new InvalidOperationException(
                    "Ehhez a programhoz már megvan a szükséges felügyelő.");
            }

            if (current != null)
            {
                // Módosítás: a meglévő felügyeletet helyezzük át.
                current.PlanSessionId = dto.PlanSessionId;
                current.SupervisionPlaceId = null;
                current.UpdateDate = DateTimeOffset.UtcNow;
            }
            else
            {
                // Új felügyelet.
                _db.Supervisions.Add(new Supervision
                {
                    UserId = dto.TeacherId,
                    PlanSessionId = dto.PlanSessionId,
                    ApplicationDate = DateTimeOffset.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task DeleteAsync(
            int teacherId, int supervisionId, int eventId)
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            var supervision = await _db.Supervisions
                .Include(s => s.PlanSession)
                    .ThenInclude(ps => ps!.TimeSlot)
                .Include(s => s.SupervisionPlace)
                    .ThenInclude(sp => sp!.TimeSlot)
                .FirstOrDefaultAsync(s =>
                    s.SupervisionId == supervisionId &&
                    s.UserId == teacherId);

            if (supervision == null)
                throw new InvalidOperationException(
                    "A felügyeleti jelentkezés nem található.");

            int? actualEventId =
                supervision.PlanSession?.TimeSlot.EventId ??
                supervision.SupervisionPlace?.TimeSlot.EventId;

            if (actualEventId != eventId)
                throw new InvalidOperationException(
                    "A felügyelet nem tartozik a kiválasztott eseményhez.");

            _db.Supervisions.Remove(supervision);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
    }
}
