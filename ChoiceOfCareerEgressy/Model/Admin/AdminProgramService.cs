
using ChoiceOfCareerEgressy.Dto.ProgramDto;
using ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChoiceOfCareerEgressy.Model.Admin.Programs
{
    public class AdminProgramService
    {
        private readonly ChoiceOfCareerPersistence _context;

        public AdminProgramService(
            ChoiceOfCareerPersistence context)
        {
            _context = context;
        }

        // =====================================================
        // PROGRAMOK LISTÁZÁSA
        // =====================================================

        public async Task<List<AdminProgramListDto>> GetProgramsAsync(
            int? eventId = null,
            string? search = null)
        {
            var query = _context.PlanSessions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.Plan)
                    .ThenInclude(p => p.Branch)
                .Include(s => s.Plan)
                    .ThenInclude(p => p.Sex)
                .Include(s => s.TimeSlot)
                    .ThenInclude(t => t.Event)
                .Include(s => s.Location)
                .Include(s => s.Applications)
                .Include(s => s.Supervisions)
                .AsQueryable();

            if (eventId.HasValue)
            {
                query = query.Where(s =>
                    s.TimeSlot.EventId == eventId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string text = search.Trim();

                query = query.Where(s =>
                    s.Plan.PlanName.Contains(text) ||
                    s.Plan.PlanDescription.Contains(text) ||
                    s.Location.LocationName.Contains(text));
            }

            var sessions = await query.ToListAsync();

            return sessions
                .OrderBy(s => s.TimeSlot.Event.EventDate)
                .ThenBy(s => s.TimeSlot.BeginDate)
                .ThenBy(s => s.Plan.PlanName)
                .Select(s => new AdminProgramListDto
                {
                    PlanSessionId = s.PlanSessionId,
                    PlanId = s.PlanId,

                    PlanName = s.Plan.PlanName,
                    PlanDescription = s.Plan.PlanDescription,

                    EventId = s.TimeSlot.EventId,
                    EventName = s.TimeSlot.Event.EventName,

                    TimeSlotId = s.TimeSlotId,
                    TimeSlotName = s.TimeSlot.TimeSlotName,

                    BeginDate = s.TimeSlot.BeginDate,
                    EndDate = s.TimeSlot.EndDate,

                    LocationName = s.Location.LocationName,

                    BranchName = s.Plan.Branch?.BranchName
                        ?? "Minden ágazat",

                    SexName = s.Plan.Sex?.SexName
                        ?? "Unisex",

                    Capacity = s.Capacity,
                    RequiredSupervisors = s.RequiredSupervisors,

                    ApplicationCount = s.Applications.Count(a =>
                        a.Status == ApplicationStatus.Active),

                    SupervisionCount = s.Supervisions.Count
                })
                .ToList();
        }

        // =====================================================
        // EGY PROGRAM ADATAINAK LEKÉRDEZÉSE
        // =====================================================

        public async Task<AdminProgramDto?> GetProgramAsync(
            int planSessionId)
        {
            var session = await _context.PlanSessions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Include(s => s.TimeSlot)
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s =>
                    s.PlanSessionId == planSessionId);

            if (session == null)
                return null;

            return new AdminProgramDto
            {
                PlanSessionId = session.PlanSessionId,
                PlanId = session.PlanId,

                PlanName = session.Plan.PlanName,
                PlanDescription = session.Plan.PlanDescription,

                BranchId = session.Plan.BranchId,
                SexId = session.Plan.SexId,

                EventId = session.TimeSlot.EventId,
                TimeSlotId = session.TimeSlotId,
                LocationId = session.LocationId,

                Capacity = session.Capacity,
                RequiredSupervisors =
                    session.RequiredSupervisors
            };
        }

        // =====================================================
        // LEGÖRDÜLŐ LISTÁK ADATAI
        // =====================================================

        public async Task<AdminProgramOptionsDto> GetOptionsAsync()
        {
            var events = (await _context.Events
                .AsNoTracking()
                .ToListAsync())
                .OrderByDescending(e => e.EventDate)
                .ToList();

            var slots = (await _context.TimeSlots
                .AsNoTracking()
                .ToListAsync())
                .OrderBy(t => t.BeginDate)
                .ToList();

            var locations = await _context.Locations
                .AsNoTracking()
                .OrderBy(l => l.LocationName)
                .ToListAsync();

            var branches = await _context.Branches
                .AsNoTracking()
                .OrderBy(b => b.BranchName)
                .ToListAsync();

            var sexes = await _context.Sexes
                .AsNoTracking()
                .OrderBy(s => s.SexName)
                .ToListAsync();

            return new AdminProgramOptionsDto
            {
                Events = events.Select(e =>
                    new AdminProgramOptionDto
                    {
                        Id = e.EventId,
                        Name = e.EventName
                    }).ToList(),

                TimeSlots = slots.Select(t =>
                    new AdminTimeSlotOptionDto
                    {
                        TimeSlotId = t.TimeSlotId,
                        EventId = t.EventId,
                        TimeSlotName = t.TimeSlotName,
                        BeginDate = t.BeginDate,
                        EndDate = t.EndDate
                    }).ToList(),

                Locations = locations.Select(l =>
                    new AdminProgramOptionDto
                    {
                        Id = l.LocationId,
                        Name = l.LocationName
                    }).ToList(),

                Branches = branches.Select(b =>
                    new AdminProgramOptionDto
                    {
                        Id = b.BranchId,
                        Name = b.BranchName
                    }).ToList(),

                Sexes = sexes.Select(s =>
                    new AdminProgramOptionDto
                    {
                        Id = s.SexId,
                        Name = s.SexName
                    }).ToList()
            };
        }

        // =====================================================
        // SEGÉDFÜGGVÉNYEK
        // =====================================================

        private static string Normalize(string value)
        {
            return string.Join(
                " ",
                value.Trim()
                    .Split(' ',
                        StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
        }

        private static bool IntervalsOverlap(
            DateTimeOffset firstStart,
            DateTimeOffset firstEnd,
            DateTimeOffset secondStart,
            DateTimeOffset secondEnd)
        {
            return firstStart < secondEnd &&
                   secondStart < firstEnd;
        }

        private async Task<TimeSlot> ValidateTimeSlotAsync(
            AdminProgramDto dto)
        {
            var slot = await _context.TimeSlots
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t =>
                    t.TimeSlotId == dto.TimeSlotId);

            if (slot == null)
                throw new InvalidOperationException(
                    "A kiválasztott idősáv nem található.");

            if (slot.EventId != dto.EventId)
                throw new InvalidOperationException(
                    "Az idősáv nem a kiválasztott eseményhez tartozik.");

            if (slot.BeginDate >= slot.EndDate)
                throw new InvalidOperationException(
                    "Az idősáv időpontjai hibásak.");

            return slot;
        }

        private async Task<Location> ResolveLocationAsync(
            AdminProgramDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.NewLocationName))
            {
                string name = dto.NewLocationName.Trim();

                var locations = await _context.Locations
                    .ToListAsync();

                var existing = locations.FirstOrDefault(l =>
                    Normalize(l.LocationName) == Normalize(name));

                if (existing != null)
                    return existing;

                var newLocation = new Location
                {
                    LocationName = name
                };

                _context.Locations.Add(newLocation);
                return newLocation;
            }

            if (!dto.LocationId.HasValue)
                throw new InvalidOperationException(
                    "Válassz termet, vagy adj meg új teremnevet.");

            var location = await _context.Locations
                .FirstOrDefaultAsync(l =>
                    l.LocationId == dto.LocationId.Value);

            return location ?? throw new InvalidOperationException(
                "A kiválasztott terem nem található.");
        }

        private async Task ValidateReferencesAsync(
            AdminProgramDto dto)
        {
            if (dto.BranchId.HasValue &&
                !await _context.Branches.AnyAsync(b =>
                    b.BranchId == dto.BranchId.Value))
            {
                throw new InvalidOperationException(
                    "A kiválasztott ágazat nem található.");
            }

            if (dto.SexId.HasValue &&
                !await _context.Sexes.AnyAsync(s =>
                    s.SexId == dto.SexId.Value))
            {
                throw new InvalidOperationException(
                    "A kiválasztott nem nem található.");
            }
        }

        private async Task ValidateRoomAvailabilityAsync(
            int locationId,
            TimeSlot slot,
            int? excludedPlanSessionId = null)
        {
            var otherSessions = await _context.PlanSessions
                .AsNoTracking()
                .Include(s => s.TimeSlot)
                .Where(s =>
                    s.LocationId == locationId &&
                    (!excludedPlanSessionId.HasValue ||
                     s.PlanSessionId != excludedPlanSessionId.Value))
                .ToListAsync();

            bool occupied = otherSessions.Any(s =>
                IntervalsOverlap(
                    slot.BeginDate,
                    slot.EndDate,
                    s.TimeSlot.BeginDate,
                    s.TimeSlot.EndDate));

            if (occupied)
                throw new InvalidOperationException(
                    "A kiválasztott terem ebben az időszakban már foglalt.");
        }

        private async Task ValidateDuplicateProgramAsync(
            int planId,
            int timeSlotId,
            int? excludedPlanSessionId = null)
        {
            bool duplicate = await _context.PlanSessions
                .AnyAsync(s =>
                    s.PlanId == planId &&
                    s.TimeSlotId == timeSlotId &&
                    (!excludedPlanSessionId.HasValue ||
                     s.PlanSessionId != excludedPlanSessionId.Value));

            if (duplicate)
                throw new InvalidOperationException(
                    "Ez a program már szerepel a kiválasztott idősávban.");
        }

        private async Task AddAuditLogAsync(
            string action,
            string entityName,
            int entityId,
            object? oldValue,
            object? newValue,
            int? adminUserId)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminUserId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValue = oldValue == null
                    ? null
                    : JsonSerializer.Serialize(oldValue),
                NewValue = newValue == null
                    ? null
                    : JsonSerializer.Serialize(newValue),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        // =====================================================
        // ÚJ PROGRAM LÉTREHOZÁSA
        // =====================================================

        public async Task<int> CreateProgramAsync(
            AdminProgramDto dto,
            int? adminUserId = null)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            if (string.IsNullOrWhiteSpace(dto.PlanName) ||
                string.IsNullOrWhiteSpace(dto.PlanDescription))
            {
                throw new InvalidOperationException(
                    "A program neve és leírása kötelező.");
            }

            if (dto.Capacity <= 0 ||
                dto.RequiredSupervisors < 0)
            {
                throw new InvalidOperationException(
                    "Hibás kapacitás vagy felügyelőszám.");
            }

            await ValidateReferencesAsync(dto);

            var slot = await ValidateTimeSlotAsync(dto);
            var location = await ResolveLocationAsync(dto);

            // Az Excel-importtal megegyezően
            // újrahasználjuk az azonos alapadatú programot.
            var plans = await _context.Plans.ToListAsync();

            var plan = plans.FirstOrDefault(p =>
                Normalize(p.PlanName) ==
                    Normalize(dto.PlanName) &&
                p.BranchId == dto.BranchId &&
                p.SexId == dto.SexId);

            if (plan == null)
            {
                plan = new Plan
                {
                    PlanName = dto.PlanName.Trim(),
                    PlanDescription = dto.PlanDescription.Trim(),
                    BranchId = dto.BranchId,
                    SexId = dto.SexId
                };

                _context.Plans.Add(plan);
            }
            else
            {
                await ValidateDuplicateProgramAsync(
                    plan.PlanId,
                    slot.TimeSlotId);
            }

            if (location.LocationId != 0)
            {
                await ValidateRoomAvailabilityAsync(
                    location.LocationId,
                    slot);
            }

            var session = new PlanSession
            {
                Plan = plan,
                TimeSlot = slot,
                Location = location,
                Capacity = dto.Capacity,
                RequiredSupervisors = dto.RequiredSupervisors
            };

            _context.PlanSessions.Add(session);

            await _context.SaveChangesAsync();

            await AddAuditLogAsync(
                "CREATE",
                "PlanSession",
                session.PlanSessionId,
                null,
                new
                {
                    session.PlanSessionId,
                    session.PlanId,
                    session.TimeSlotId,
                    session.LocationId,
                    session.Capacity,
                    session.RequiredSupervisors,
                    plan.PlanName
                },
                adminUserId);

            await transaction.CommitAsync();

            return session.PlanSessionId;
        }

        // =====================================================
        // PROGRAM MÓDOSÍTÁSA
        // =====================================================

        public async Task UpdateProgramAsync(
            AdminProgramDto dto,
            int? adminUserId = null)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var session = await _context.PlanSessions
                .Include(s => s.Plan)
                .Include(s => s.TimeSlot)
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s =>
                    s.PlanSessionId == dto.PlanSessionId);

            if (session == null)
                throw new InvalidOperationException(
                    "A módosítandó program nem található.");

            if (string.IsNullOrWhiteSpace(dto.PlanName) ||
                string.IsNullOrWhiteSpace(dto.PlanDescription))
            {
                throw new InvalidOperationException(
                    "A program neve és leírása kötelező.");
            }

            if (dto.Capacity <= 0 ||
                dto.RequiredSupervisors < 0)
            {
                throw new InvalidOperationException(
                    "Hibás kapacitás vagy felügyelőszám.");
            }

            await ValidateReferencesAsync(dto);

            var newSlot = await ValidateTimeSlotAsync(dto);
            var newLocation = await ResolveLocationAsync(dto);

            int activeCount = await _context.Applications
                .CountAsync(a =>
                    a.PlanSessionId == session.PlanSessionId &&
                    a.Status == ApplicationStatus.Active);

            if (dto.Capacity < activeCount)
            {
                throw new InvalidOperationException(
                    $"A programra már {activeCount} diák jelentkezett. " +
                    "A kapacitás nem lehet ennél kisebb.");
            }

            // A program neve, leírása és jogosultságai
            // más alkalmaknál is megváltozhatnak,
            // ha ugyanazt a Plan rekordot használják.
            // Ezért a közös Plan módosítása helyett
            // szükség esetén külön Plan rekordot készítünk.

            var oldPlan = session.Plan;

            bool hasOtherSessions = await _context.PlanSessions
                .AnyAsync(s =>
                    s.PlanId == oldPlan.PlanId &&
                    s.PlanSessionId != session.PlanSessionId);

            bool planDataChanged =
                oldPlan.PlanName != dto.PlanName.Trim() ||
                oldPlan.PlanDescription != dto.PlanDescription.Trim() ||
                oldPlan.BranchId != dto.BranchId ||
                oldPlan.SexId != dto.SexId;

            Plan targetPlan = oldPlan;

            if (planDataChanged && hasOtherSessions)
            {
                targetPlan = new Plan
                {
                    PlanName = dto.PlanName.Trim(),
                    PlanDescription = dto.PlanDescription.Trim(),
                    BranchId = dto.BranchId,
                    SexId = dto.SexId
                };

                _context.Plans.Add(targetPlan);
            }
            else if (planDataChanged)
            {
                oldPlan.PlanName = dto.PlanName.Trim();
                oldPlan.PlanDescription =
                    dto.PlanDescription.Trim();
                oldPlan.BranchId = dto.BranchId;
                oldPlan.SexId = dto.SexId;
            }

            // Ellenőrizzük, hogy az új idősávban
            // nincs-e ugyanilyen program.
            if (targetPlan.PlanId != 0)
            {
                await ValidateDuplicateProgramAsync(
                    targetPlan.PlanId,
                    newSlot.TimeSlotId,
                    session.PlanSessionId);
            }

            if (newLocation.LocationId != 0)
            {
                await ValidateRoomAvailabilityAsync(
                    newLocation.LocationId,
                    newSlot,
                    session.PlanSessionId);
            }

            // Ellenőrizzük az összes jelentkező
            // többi programjával való időbeli ütközést.
            var affectedApplications = await _context.Applications
                .AsNoTracking()
                .Where(a =>
                    a.PlanSessionId == session.PlanSessionId &&
                    a.Status == ApplicationStatus.Active)
                .Select(a => new
                {
                    a.UserId,
                    a.User.UserName
                })
                .ToListAsync();

            var registeredStudents = await _context.Applications
                .AsNoTracking()
                .Include(a => a.User)
                .Where(a =>
                    a.PlanSessionId == session.PlanSessionId &&
                    a.Status == ApplicationStatus.Active)
                .Select(a => a.User)
                .ToListAsync();

            var ineligibleStudents = registeredStudents
                .Where(student =>
                    (dto.BranchId.HasValue &&
                     student.BranchId != dto.BranchId.Value) ||
                    (dto.SexId.HasValue &&
                     student.SexId != dto.SexId.Value))
                .Select(student => student.UserName)
                .Distinct()
                .ToList();

            if (ineligibleStudents.Count > 0)
            {
                throw new InvalidOperationException(
                    "A módosított jelentkezési feltételek kizárnák " +
                    "a következő már jelentkezett diákokat: " +
                    string.Join(", ", ineligibleStudents));
            }


            var affectedUserIds = affectedApplications
                .Select(a => a.UserId)
                .ToList();

            var otherApplications = await _context.Applications
                .AsNoTracking()
                .Include(a => a.PlanSession)
                    .ThenInclude(s => s.TimeSlot)
                .Where(a =>
                    affectedUserIds.Contains(a.UserId) &&
                    a.PlanSessionId != session.PlanSessionId &&
                    a.Status == ApplicationStatus.Active)
                .ToListAsync();

            var conflictingStudents = affectedApplications
                .Where(student =>
                    otherApplications.Any(a =>
                        a.UserId == student.UserId &&
                        IntervalsOverlap(
                            newSlot.BeginDate,
                            newSlot.EndDate,
                            a.PlanSession.TimeSlot.BeginDate,
                            a.PlanSession.TimeSlot.EndDate)))
                .Select(s => s.UserName)
                .Distinct()
                .ToList();

            if (conflictingStudents.Count > 0)
            {
                throw new InvalidOperationException(
                    "Az új időpont ütközne a következő diákok " +
                    "másik programjával: " +
                    string.Join(", ", conflictingStudents));
            }

            // Tanári felügyeletek ütközésének ellenőrzése.
            var assignedTeachers = await _context.Supervisions
                .AsNoTracking()
                .Where(s =>
                    s.PlanSessionId == session.PlanSessionId)
                .Select(s => new
                {
                    s.UserId,
                    s.User.UserName
                })
                .ToListAsync();

            var teacherIds = assignedTeachers
                .Select(s => s.UserId)
                .ToList();

            var otherSupervisions = await _context.Supervisions
                .AsNoTracking()
                .Include(s => s.PlanSession)
                    .ThenInclude(p => p!.TimeSlot)
                .Include(s => s.SupervisionPlace)
                    .ThenInclude(p => p!.TimeSlot)
                .Where(s =>
                    teacherIds.Contains(s.UserId) &&
                    s.PlanSessionId != session.PlanSessionId)
                .ToListAsync();

            var conflictingTeachers = assignedTeachers
                .Where(teacher =>
                    otherSupervisions.Any(s =>
                    {
                        if (s.UserId != teacher.UserId)
                            return false;

                        var otherSlot =
                            s.PlanSession?.TimeSlot ??
                            s.SupervisionPlace?.TimeSlot;

                        return otherSlot != null &&
                            IntervalsOverlap(
                                newSlot.BeginDate,
                                newSlot.EndDate,
                                otherSlot.BeginDate,
                                otherSlot.EndDate);
                    }))
                .Select(t => t.UserName)
                .Distinct()
                .ToList();

            if (conflictingTeachers.Count > 0)
            {
                throw new InvalidOperationException(
                    "Az új időpont ütközne a következő " +
                    "tanárok felügyeletével: " +
                    string.Join(", ", conflictingTeachers));
            }

            var oldValue = new
            {
                session.PlanSessionId,
                session.PlanId,
                session.TimeSlotId,
                session.LocationId,
                session.Capacity,
                session.RequiredSupervisors,
                session.Plan.PlanName,
                session.Plan.PlanDescription,
                session.Plan.BranchId,
                session.Plan.SexId
            };

            session.Plan = targetPlan;
            session.TimeSlot = newSlot;
            session.Location = newLocation;
            session.Capacity = dto.Capacity;
            session.RequiredSupervisors =
                dto.RequiredSupervisors;

            await _context.SaveChangesAsync();

            // Ha a régi Plan már nem kapcsolódik
            // egyetlen meghirdetett alkalomhoz sem,
            // eltávolítható.
            if (targetPlan != oldPlan)
            {
                bool oldPlanUsed = await _context.PlanSessions
                    .AnyAsync(s => s.PlanId == oldPlan.PlanId);

                if (!oldPlanUsed)
                {
                    _context.Plans.Remove(oldPlan);
                    await _context.SaveChangesAsync();
                }
            }

            await AddAuditLogAsync(
                "UPDATE",
                "PlanSession",
                session.PlanSessionId,
                oldValue,
                new
                {
                    session.PlanSessionId,
                    session.PlanId,
                    session.TimeSlotId,
                    session.LocationId,
                    session.Capacity,
                    session.RequiredSupervisors,
                    session.Plan.PlanName,
                    session.Plan.PlanDescription,
                    session.Plan.BranchId,
                    session.Plan.SexId
                },
                adminUserId);

            await transaction.CommitAsync();
        }

        // =====================================================
        // TÖRLÉS ELŐTTI ÖSSZESÍTÉS
        // =====================================================

        public async Task<AdminProgramDeleteDto?> GetDeleteInfoAsync(int planSessionId)
        {
            var session = await _context.PlanSessions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Include(s => s.TimeSlot)
                    .ThenInclude(t => t.Event)
                .Include(s => s.Location)
                .Include(s => s.Applications)
                .Include(s => s.Supervisions)
                .FirstOrDefaultAsync(s =>
                    s.PlanSessionId == planSessionId);

            if (session == null)
                return null;

            bool hasOtherSessions = await _context.PlanSessions
                .AnyAsync(s =>
                    s.PlanId == session.PlanId &&
                    s.PlanSessionId != planSessionId);

            return new AdminProgramDeleteDto
            {
                PlanSessionId = session.PlanSessionId,
                PlanName = session.Plan.PlanName,
                EventName = session.TimeSlot.Event.EventName,
                TimeSlotName = session.TimeSlot.TimeSlotName,
                LocationName = session.Location.LocationName,
                BeginDate = session.TimeSlot.BeginDate,
                EndDate = session.TimeSlot.EndDate,

                TotalApplicationCount = session.Applications.Count,

                ActiveApplicationCount =
                    session.Applications.Count(a =>
                        a.Status == ApplicationStatus.Active),

                SupervisionCount = session.Supervisions.Count,
                HasOtherSessions = hasOtherSessions
            };
        }

        // =====================================================
        // PROGRAM TÖRLÉSE
        // =====================================================

        public async Task DeleteProgramAsync(int planSessionId, int? adminUserId = null)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var session = await _context.PlanSessions
                .Include(s => s.Plan)
                .Include(s => s.TimeSlot)
                .Include(s => s.Applications)
                .Include(s => s.Supervisions)
                .FirstOrDefaultAsync(s =>
                    s.PlanSessionId == planSessionId);

            if (session == null)
                throw new InvalidOperationException(
                    "A törlendő program nem található.");

            var oldValue = new
            {
                session.PlanSessionId,
                session.PlanId,
                session.TimeSlotId,
                session.LocationId,
                session.Capacity,
                session.RequiredSupervisors,
                session.Plan.PlanName,
                TotalApplications = session.Applications.Count,
                ActiveApplications = session.Applications.Count(a =>
                    a.Status == ApplicationStatus.Active),
                Supervisions = session.Supervisions.Count
            };

            var plan = session.Plan;

            // A diákok jelentkezéseit és a tanári
            // felügyeleteket is töröljük.
            _context.Applications.RemoveRange(
                session.Applications);

            _context.Supervisions.RemoveRange(
                session.Supervisions);

            await _context.SaveChangesAsync();

            _context.PlanSessions.Remove(session);
            await _context.SaveChangesAsync();

            // Csak akkor töröljük a Plan alaprekordot,
            // ha más alkalom már nem használja.
            bool hasOtherSessions = await _context.PlanSessions
                .AnyAsync(s => s.PlanId == plan.PlanId);

            if (!hasOtherSessions)
            {
                _context.Plans.Remove(plan);
                await _context.SaveChangesAsync();
            }

            await AddAuditLogAsync(
                "DELETE",
                "PlanSession",
                planSessionId,
                oldValue,
                null,
                adminUserId);

            await transaction.CommitAsync();
        }

        public async Task<int> DeleteAllProgramsAsync(int eventId, int? adminUserId = null)
        {
            bool eventExists = await _context.Events
                .AnyAsync(e => e.EventId == eventId);

            if (!eventExists)
            {
                throw new InvalidOperationException(
                    "A kiválasztott esemény nem található.");
            }

            var programIds = await _context.PlanSessions
                .AsNoTracking()
                .Where(s => s.TimeSlot.EventId == eventId)
                .Select(s => s.PlanSessionId)
                .ToListAsync();

            foreach (var programId in programIds)
            {
                await DeleteProgramAsync(programId, adminUserId);
            }

            return programIds.Count;
        }
    }
}
