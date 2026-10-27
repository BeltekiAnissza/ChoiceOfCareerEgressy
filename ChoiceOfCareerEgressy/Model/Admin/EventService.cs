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

        // =========================================
        // ESEMÉNY LÉTREHOZÁSA
        // =========================================

        public async Task<CreatedEventDto> CreateAsync(CreateEventDto dto)
        {
            // =====================================
            // ESEMÉNY ADATAINAK ELLENŐRZÉSE
            // =====================================

            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrWhiteSpace(dto.EventName))
            {
                throw new ArgumentException(
                    "Az esemény neve kötelező.");
            }

            if (dto.TimeSlots == null || dto.TimeSlots.Count < 2)
            {
                throw new ArgumentException(
                    "Egy eseményhez legalább 2 idősávot kell megadni.");
            }

            if (dto.RegistrationStart >= dto.RegistrationEnd)
            {
                throw new ArgumentException(
                    "A jelentkezés vége későbbi kell legyen, mint a kezdete.");
            }

            // =====================================
            // ESEMÉNY DÁTUMA
            // =====================================

            var eventDate = dto.EventDate.Date;

            // =====================================
            // MAGYAR IDŐZÓNA
            // =====================================

            var registrationStart =
                ToHungarianTime(dto.RegistrationStart);

            var registrationEnd =
                ToHungarianTime(dto.RegistrationEnd);

            var eventStart =
                ToHungarianTime(eventDate);

            // =====================================
            // IDŐSÁVOK FELDOLGOZÁSA
            // =====================================

            var timeSlots = new List<TimeSlot>();

            foreach (var slot in dto.TimeSlots)
            {
                // IDŐSÁV NEVE

                if (string.IsNullOrWhiteSpace(slot.TimeSlotName))
                {
                    throw new ArgumentException(
                        "Minden idősávnak nevet kell adni.");
                }

                // KEZDÉS ÉS BEFEJEZÉS

                if (slot.BeginDate >= slot.EndDate)
                {
                    throw new ArgumentException(
                        $"A(z) {slot.TimeSlotName} kezdete " +
                        "korábbi kell legyen, mint a vége.");
                }

                // AZ IDŐSÁV AZ ESEMÉNY NAPJÁRA ESSEN

                if (slot.BeginDate.Date != eventDate ||
                    slot.EndDate.Date != eventDate)
                {
                    throw new ArgumentException(
                        $"A(z) {slot.TimeSlotName} idősávnak " +
                        "az esemény napjára kell esnie.");
                }

                // MAGYAR IDŐZÓNA SZERINTI ÁTALAKÍTÁS

                var beginDate =
                    ToHungarianTime(slot.BeginDate);

                var endDate =
                    ToHungarianTime(slot.EndDate);

                timeSlots.Add(new TimeSlot
                {
                    TimeSlotName = slot.TimeSlotName.Trim(),
                    BeginDate = beginDate,
                    EndDate = endDate
                });
            }

            // =====================================
            // IDŐSÁVOK RENDEZÉSE
            // =====================================

            timeSlots = timeSlots
                .OrderBy(s => s.BeginDate)
                .ToList();

            // =====================================
            // IDŐSÁVOK ÁTFEDÉSÉNEK ELLENŐRZÉSE
            // =====================================

            for (int i = 1; i < timeSlots.Count; i++)
            {
                var previousSlot = timeSlots[i - 1];
                var currentSlot = timeSlots[i];

                if (currentSlot.BeginDate < previousSlot.EndDate)
                {
                    throw new ArgumentException(
                        $"A(z) {previousSlot.TimeSlotName} és " +
                        $"{currentSlot.TimeSlotName} idősávok " +
                        "nem fedhetik egymást.");
                }
            }

            // =====================================
            // JELENTKEZÉSI IDŐSZAK ELLENŐRZÉSE
            // =====================================

            var firstSlotStart = timeSlots.First().BeginDate;

            if (registrationEnd > firstSlotStart)
            {
                throw new ArgumentException(
                    "A jelentkezési időszaknak az esemény " +
                    "első idősávja előtt le kell zárulnia.");
            }

            // =====================================
            // ESEMÉNY LÉTREHOZÁSA
            // =====================================

            var newEvent = new Event
            {
                EventName = dto.EventName.Trim(),
                EventDate = eventStart,
                RegistrationStart = registrationStart,
                RegistrationEnd = registrationEnd,
                IsRegistrationEnabled = dto.IsRegistrationEnabled,
                TimeSlots = timeSlots
            };

            // =====================================
            // ADATBÁZIS-MENTÉS
            // =====================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            _context.Events.Add(newEvent);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // =====================================
            // EREDMÉNY VISSZAADÁSA
            // =====================================

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

        public async Task SetRegistrationEnabledAsync(int eventId, bool isEnabled)
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

        // =========================================
        // ESEMÉNY SZERKESZTÉSE
        // =========================================

        public async Task UpdateAsync(int eventId, CreateEventDto dto, Dictionary<int, int> existingSlotIds)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrWhiteSpace(dto.EventName))
            {
                throw new ArgumentException(
                    "Az esemény neve kötelező.");
            }

            if (dto.TimeSlots == null || dto.TimeSlots.Count < 2)
            {
                throw new ArgumentException(
                    "Legalább két idősávot meg kell adni.");
            }

            var existingEvent = await _context.Events
                .Include(e => e.TimeSlots)
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (existingEvent == null)
            {
                throw new KeyNotFoundException(
                    "A megadott esemény nem található.");
            }

            // =====================================
            // IDŐPONTOK ELLENŐRZÉSE
            // =====================================

            if (dto.RegistrationStart >= dto.RegistrationEnd)
            {
                throw new ArgumentException(
                    "A jelentkezés kezdete korábbi kell legyen, mint a vége.");
            }

            var eventDate = dto.EventDate.Date;

            var newEventDate = ToHungarianTime(eventDate);

            var registrationStart =
                ToHungarianTime(dto.RegistrationStart);

            var registrationEnd =
                ToHungarianTime(dto.RegistrationEnd);

            var updatedSlots = new List<(
                int? ExistingId,
                string Name,
                DateTimeOffset Begin,
                DateTimeOffset End)>();

            // =====================================
            // IDŐSÁVOK FELDOLGOZÁSA
            // =====================================

            for (int i = 0; i < dto.TimeSlots.Count; i++)
            {
                var slot = dto.TimeSlots[i];

                if (slot == null ||
                    string.IsNullOrWhiteSpace(slot.TimeSlotName))
                {
                    throw new ArgumentException(
                        $"A(z) {i + 1}. idősáv neve kötelező.");
                }

                if (slot.BeginDate >= slot.EndDate)
                {
                    throw new ArgumentException(
                        $"A(z) {slot.TimeSlotName} kezdete " +
                        "korábbi kell legyen, mint a vége.");
                }

                if (slot.BeginDate.Date != eventDate ||
                    slot.EndDate.Date != eventDate)
                {
                    throw new ArgumentException(
                        $"A(z) {slot.TimeSlotName} idősávnak " +
                        "az esemény napjára kell esnie.");
                }

                int? existingId = null;

                if (existingSlotIds.TryGetValue(i, out int slotId) &&
                    slotId > 0)
                {
                    existingId = slotId;
                }

                updatedSlots.Add((
                    existingId,
                    slot.TimeSlotName.Trim(),
                    ToHungarianTime(slot.BeginDate),
                    ToHungarianTime(slot.EndDate)
                ));
            }

            // =====================================
            // ÁTFEDÉSEK ELLENŐRZÉSE
            // =====================================

            updatedSlots = updatedSlots
                .OrderBy(s => s.Begin)
                .ToList();

            for (int i = 1; i < updatedSlots.Count; i++)
            {
                if (updatedSlots[i].Begin < updatedSlots[i - 1].End)
                {
                    throw new ArgumentException(
                        "Az idősávok nem fedhetik egymást.");
                }
            }

            if (registrationEnd > updatedSlots[0].Begin)
            {
                throw new ArgumentException(
                    "A jelentkezésnek az első idősáv kezdete előtt le kell zárulnia.");
            }

            // =====================================
            // MEGLÉVŐ IDŐSÁVOK AZONOSÍTÓI
            // =====================================

            var existingIds = existingEvent.TimeSlots
                .Select(s => s.TimeSlotId)
                .ToHashSet();

            var submittedIds = updatedSlots
                .Where(s => s.ExistingId.HasValue)
                .Select(s => s.ExistingId!.Value)
                .ToList();

            if (submittedIds.Count != submittedIds.Distinct().Count())
            {
                throw new ArgumentException(
                    "Egy idősáv nem szerepelhet többször.");
            }

            if (submittedIds.Any(id => !existingIds.Contains(id)))
            {
                throw new ArgumentException(
                    "Az egyik idősáv nem tartozik ehhez az eseményhez.");
            }

            // =====================================
            // HASZNÁLT IDŐSÁVOK ELLENŐRZÉSE
            // =====================================

            var usedSlotIds = await _context.PlanSessions
                .Where(ps => existingIds.Contains(ps.TimeSlotId))
                .Select(ps => ps.TimeSlotId)
                .Distinct()
                .ToListAsync();

            var supervisionSlotIds = await _context.SupervisionPlaces
                .Where(sp => existingIds.Contains(sp.TimeSlotId))
                .Select(sp => sp.TimeSlotId)
                .Distinct()
                .ToListAsync();

            var protectedSlotIds = usedSlotIds
                .Concat(supervisionSlotIds)
                .ToHashSet();

            // =====================================
            // ESEMÉNY DÁTUMÁNAK VÉDELME
            // =====================================

            if (existingEvent.EventDate.Date != newEventDate.Date &&
                protectedSlotIds.Count > 0)
            {
                throw new ArgumentException(
                    "Az esemény dátuma nem módosítható, " +
                    "mert már tartoznak hozzá programok vagy felügyeleti helyek.");
            }

            // =====================================
            // TÖRLENDŐ IDŐSÁVOK
            // =====================================

            var slotsToDelete = existingEvent.TimeSlots
                .Where(s => !submittedIds.Contains(s.TimeSlotId))
                .ToList();

            foreach (var slot in slotsToDelete)
            {
                if (protectedSlotIds.Contains(slot.TimeSlotId))
                {
                    throw new ArgumentException(
                        $"A(z) {slot.TimeSlotName} nem törölhető, " +
                        "mert már használatban van.");
                }
            }

            // =====================================
            // MEGLÉVŐ IDŐSÁVOK MÓDOSÍTÁSA
            // =====================================

            foreach (var submittedSlot in updatedSlots)
            {
                if (!submittedSlot.ExistingId.HasValue)
                {
                    continue;
                }

                var existingSlot = existingEvent.TimeSlots
                    .First(s => s.TimeSlotId == submittedSlot.ExistingId.Value);

                bool timeChanged =
                    existingSlot.BeginDate != submittedSlot.Begin ||
                    existingSlot.EndDate != submittedSlot.End;

                if (timeChanged &&
                    protectedSlotIds.Contains(existingSlot.TimeSlotId))
                {
                    throw new ArgumentException(
                        $"A(z) {existingSlot.TimeSlotName} időpontja " +
                        "nem módosítható, mert már használatban van.");
                }
            }

            // =====================================
            // ADATBÁZIS-MÓDOSÍTÁS
            // =====================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // ESEMÉNY ADATAI

            existingEvent.EventName = dto.EventName.Trim();
            existingEvent.EventDate = newEventDate;
            existingEvent.RegistrationStart = registrationStart;
            existingEvent.RegistrationEnd = registrationEnd;

            // TÖRLÉS

            foreach (var slot in slotsToDelete)
            {
                _context.TimeSlots.Remove(slot);
            }

            // MÓDOSÍTÁS ÉS HOZZÁADÁS

            foreach (var submittedSlot in updatedSlots)
            {
                if (submittedSlot.ExistingId.HasValue)
                {
                    var existingSlot = existingEvent.TimeSlots
                        .First(s =>
                            s.TimeSlotId == submittedSlot.ExistingId.Value);

                    existingSlot.TimeSlotName = submittedSlot.Name;
                    existingSlot.BeginDate = submittedSlot.Begin;
                    existingSlot.EndDate = submittedSlot.End;
                }
                else
                {
                    _context.TimeSlots.Add(new TimeSlot
                    {
                        EventId = eventId,
                        TimeSlotName = submittedSlot.Name,
                        BeginDate = submittedSlot.Begin,
                        EndDate = submittedSlot.End
                    });
                }
            }

            // NAPLÓZÁS

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "EventUpdated",
                EntityName = "Event",
                EntityId = eventId,
                OldValue = null,
                NewValue = existingEvent.EventName
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
    }
}