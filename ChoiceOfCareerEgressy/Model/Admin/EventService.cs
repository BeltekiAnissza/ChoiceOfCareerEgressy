using ChoiceOfCareerEgressy.Dto.ProgramDto;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model.Admin
{
    public class EventService
    {
        private readonly ChoiceOfCareerPersistence _context;

        public EventService(ChoiceOfCareerPersistence context)
        {
            _context = context;
        }

        private static DateTimeOffset ToHungarianTime(DateTime value)
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(
                "Europe/Budapest");

            var local = DateTime.SpecifyKind(
                value, DateTimeKind.Unspecified);

            if (zone.IsInvalidTime(local) ||
                zone.IsAmbiguousTime(local))
            {
                throw new ArgumentException(
                    "Az egyik időpont nem létezik, vagy nem egyértelmű.");
            }

            return new DateTimeOffset(
                local, zone.GetUtcOffset(local));
        }

        // =========================================
        // ESEMÉNY LÉTREHOZÁSA
        // =========================================

        public async Task<CreatedEventDto> CreateAsync(CreateEventDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EventName))
                throw new ArgumentException(
                    "Az esemény neve kötelező.");

            if (dto.RegistrationStart >= dto.RegistrationEnd)
                throw new ArgumentException(
                    "A jelentkezés vége későbbi kell legyen, mint a kezdete.");

            if (dto.FirstSlotStart >= dto.FirstSlotEnd ||
                dto.SecondSlotStart >= dto.SecondSlotEnd)
            {
                throw new ArgumentException(
                    "Az idősávok kezdete korábbi kell legyen, mint a vége.");
            }

            var eventDate = dto.EventDate.Date;

            if (dto.FirstSlotStart.Date != eventDate ||
                dto.FirstSlotEnd.Date != eventDate ||
                dto.SecondSlotStart.Date != eventDate ||
                dto.SecondSlotEnd.Date != eventDate)
            {
                throw new ArgumentException(
                    "Mindkét idősávnak az esemény napjára kell esnie.");
            }

            if (dto.FirstSlotStart < dto.SecondSlotEnd &&
                dto.SecondSlotStart < dto.FirstSlotEnd)
            {
                throw new ArgumentException(
                    "A két idősáv nem fedheti egymást.");
            }

            var registrationStart =
                ToHungarianTime(dto.RegistrationStart);

            var registrationEnd =
                ToHungarianTime(dto.RegistrationEnd);

            var firstStart =
                ToHungarianTime(dto.FirstSlotStart);

            var firstEnd =
                ToHungarianTime(dto.FirstSlotEnd);

            var secondStart =
                ToHungarianTime(dto.SecondSlotStart);

            var secondEnd =
                ToHungarianTime(dto.SecondSlotEnd);

            if (registrationEnd > firstStart ||
                registrationEnd > secondStart)
            {
                throw new ArgumentException(
                    "A jelentkezési időszaknak az esemény előtt le kell zárulnia.");
            }

            var newEvent = new Event
            {
                EventName = dto.EventName.Trim(),
                EventDate = ToHungarianTime(eventDate),
                RegistrationStart = registrationStart,
                RegistrationEnd = registrationEnd,
                IsRegistrationEnabled = dto.IsRegistrationEnabled
            };

            newEvent.TimeSlots.Add(new TimeSlot
            {
                TimeSlotName = "1. idősáv",
                BeginDate = firstStart,
                EndDate = firstEnd
            });

            newEvent.TimeSlots.Add(new TimeSlot
            {
                TimeSlotName = "2. idősáv",
                BeginDate = secondStart,
                EndDate = secondEnd
            });

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            _context.Events.Add(newEvent);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new CreatedEventDto
            {
                EventId = newEvent.EventId,
                EventName = newEvent.EventName,
                EventDate = newEvent.EventDate
            };
        }

        // =========================================
        // ESEMÉNY ÁTNEVEZÉSE
        // =========================================

        public async Task RenameAsync(int eventId, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException(
                    "Az esemény neve nem lehet üres.");
            }

            newName = newName.Trim();

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (existingEvent == null)
            {
                throw new KeyNotFoundException(
                    "A megadott esemény nem található.");
            }

            if (existingEvent.EventName == newName)
            {
                return;
            }

            var oldName = existingEvent.EventName;

            existingEvent.EventName = newName;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "EventRenamed",
                EntityName = "Event",
                EntityId = eventId,
                OldValue = oldName,
                NewValue = newName
            });

            await _context.SaveChangesAsync();
        }

        // =========================================
        // ESEMÉNY TÖRLÉSE
        // =========================================

        public async Task DeleteAsync(int eventId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var existingEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (existingEvent == null)
            {
                throw new KeyNotFoundException(
                    "A megadott esemény nem található.");
            }

            // =====================================
            // 1. PROGRAMALKALMAK AZONOSÍTÓI
            // =====================================

            var planSessionIds = await _context.PlanSessions
                .Where(ps => ps.TimeSlot.EventId == eventId)
                .Select(ps => ps.PlanSessionId)
                .ToListAsync();

            // =====================================
            // 2. FELÜGYELETI HELYEK AZONOSÍTÓI
            // =====================================

            var supervisionPlaceIds =
                await _context.SupervisionPlaces
                    .Where(sp => sp.TimeSlot.EventId == eventId)
                    .Select(sp => sp.SupervisionPlaceId)
                    .ToListAsync();

            // =====================================
            // 3. DIÁKJELENTKEZÉSEK TÖRLÉSE
            // =====================================

            await _context.Applications
                .Where(a => planSessionIds.Contains(a.PlanSessionId))
                .ExecuteDeleteAsync();

            // =====================================
            // 4. TANÁRI FELÜGYELETEK TÖRLÉSE
            // =====================================

            await _context.Supervisions
                .Where(s =>
                    (s.PlanSessionId.HasValue &&
                     planSessionIds.Contains(s.PlanSessionId.Value))
                    ||
                    (s.SupervisionPlaceId.HasValue &&
                     supervisionPlaceIds.Contains(s.SupervisionPlaceId.Value)))
                .ExecuteDeleteAsync();

            // =====================================
            // 5. PROGRAMALKALMAK TÖRLÉSE
            // =====================================

            await _context.PlanSessions
                .Where(ps => ps.TimeSlot.EventId == eventId)
                .ExecuteDeleteAsync();

            // =====================================
            // 6. FELÜGYELETI HELYEK TÖRLÉSE
            // =====================================

            await _context.SupervisionPlaces
                .Where(sp => sp.TimeSlot.EventId == eventId)
                .ExecuteDeleteAsync();

            // =====================================
            // 7. IDŐSÁVOK TÖRLÉSE
            // =====================================

            await _context.TimeSlots
                .Where(ts => ts.EventId == eventId)
                .ExecuteDeleteAsync();

            // =====================================
            // 8. ESEMÉNY TÖRLÉSE
            // =====================================

            await _context.Events
                .Where(e => e.EventId == eventId)
                .ExecuteDeleteAsync();

            // =====================================
            // 9. NAPLÓZÁS
            // =====================================

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "EventDeleted",
                EntityName = "Event",
                EntityId = eventId,
                OldValue = existingEvent.EventName,
                NewValue = null
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        // =========================================
        // JELENTKEZÉS ENGEDÉLYEZÉSE / LETILTÁSA
        // =========================================

        public async Task SetRegistrationEnabledAsync(
            int eventId,
            bool isEnabled)
        {
            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (existingEvent == null)
            {
                throw new KeyNotFoundException(
                    "A megadott esemény nem található.");
            }

            if (existingEvent.IsRegistrationEnabled == isEnabled)
            {
                return;
            }

            if (isEnabled)
            {
                var now = DateTimeOffset.UtcNow;

                if (now < existingEvent.RegistrationStart ||
                    now >= existingEvent.RegistrationEnd)
                {
                    throw new InvalidOperationException(
                        "A jelentkezés csak a megadott jelentkezési időszakban indítható el.");
                }
            }

            bool previousValue = existingEvent.IsRegistrationEnabled;

            existingEvent.IsRegistrationEnabled = isEnabled;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = isEnabled
                    ? "EventRegistrationEnabled"
                    : "EventRegistrationDisabled",

                EntityName = "Event",
                EntityId = eventId,

                OldValue = previousValue.ToString(),
                NewValue = isEnabled.ToString()
            });

            await _context.SaveChangesAsync();
        }
    }
}