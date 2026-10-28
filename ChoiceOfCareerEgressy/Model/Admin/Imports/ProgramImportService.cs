using ChoiceOfCareerEgressy.Dto.ProgramDto.ImportPrograms;
using ChoiceOfCareerEgressy.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChoiceOfCareerEgressy.Model.Admin.Imports
{
    public class ProgramImportService
    {
        private readonly ChoiceOfCareerPersistence _context;

        public ProgramImportService(ChoiceOfCareerPersistence context)
        {
            _context = context;
        }

        private static string Normalize(string value)
        {
            return Regex.Replace(value.Trim(), @"\s+", " ")
                .ToUpperInvariant();
        }

        private static DateTimeOffset ParseDate(string value)
        {
            var formats = new[]
            {
                "yyyy.MM.dd HH:mm",
                "yyyy.MM.dd H:mm",
                "yyyy-MM-dd HH:mm",
                "yyyy-MM-dd H:mm",
                "yyyy-MM-ddTHH:mm"
            };

            if (!DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var localDate))
            {
                throw new FormatException(
                    $"Hibás dátum: \"{value}\". " +
                    "Elvárt formátum például: 2026.11.20 09:00.");
            }

            TimeZoneInfo timeZone;

            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                    "Europe/Budapest");
            }
            catch (TimeZoneNotFoundException)
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                    "Central Europe Standard Time");
            }

            localDate = DateTime.SpecifyKind(
                localDate,
                DateTimeKind.Unspecified);

            if (timeZone.IsInvalidTime(localDate)
                || timeZone.IsAmbiguousTime(localDate))
            {
                throw new FormatException(
                    $"A megadott időpont nem létezik vagy " +
                    $"nem egyértelmű: \"{value}\".");
            }

            return new DateTimeOffset(
                localDate,
                timeZone.GetUtcOffset(localDate));
        }

        public async Task<ProgramImportResultDto> ImportAsync(
            IFormFile file,
            int eventId)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidDataException(
                    "Nem választottál ki fájlt, vagy a fájl üres.");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                throw new InvalidDataException(
                    "A fájl mérete maximum 5 MB lehet.");
            }

            if (!Path.GetExtension(file.FileName)
                .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Csak .xlsx formátumú Excel-fájl tölthető fel.");
            }

            var selectedEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (selectedEvent == null)
            {
                throw new InvalidDataException(
                    "A kiválasztott esemény nem található.");
            }

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var sheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidDataException(
                    "Az Excel nem tartalmaz munkalapot.");

            var headers = new Dictionary<string, int>();

            foreach (var cell in sheet.Row(1).CellsUsed())
            {
                string header = Normalize(cell.GetString());

                if (!headers.TryAdd(
                    header,
                    cell.Address.ColumnNumber))
                {
                    throw new InvalidDataException(
                        $"Az Excelben ismétlődő oszlop található: " +
                        $"\"{header}\".");
                }
            }

            string[] requiredHeaders =
            {
                "nev",
                "leiras",
                "agazat",
                "nem",
                "terem",
                "kapacitas",
                "kezdodatum",
                "vegdatum",
                "felugyelok"
            };

            foreach (string header in requiredHeaders)
            {
                if (!headers.ContainsKey(Normalize(header)))
                {
                    throw new InvalidDataException(
                        $"Hiányzik az Excelből a(z) " +
                        $"\"{header}\" oszlop.");
                }
            }

            string ReadCell(int row, string column)
            {
                return sheet.Cell(
                    row,
                    headers[Normalize(column)])
                    .GetFormattedString()
                    .Trim();
            }

            var result = new ProgramImportResultDto();

            void AddError(int row, string field, string message)
            {
                result.Errors.Add(new ProgramImportErrorDto
                {
                    RowNumber = row,
                    Message = $"{field}: {message}"
                });

                result.SkippedCount++;
            }

            var branches = await _context.Branches
                .ToListAsync();

            var sexes = await _context.Sexes
                .ToListAsync();

            var locations = await _context.Locations
                .ToListAsync();

            var plans = await _context.Plans
                .ToListAsync();

            var slots = await _context.TimeSlots
                .Where(t => t.EventId == eventId)
                .ToListAsync();

            var sessions = await _context.PlanSessions
                .Include(s => s.TimeSlot)
                .Include(s => s.Plan)
                .Include(s => s.Location)
                .Where(s => s.TimeSlot.EventId == eventId)
                .ToListAsync();

            var validRows = new List<(
                int Row,
                ProgramImportDto Data,
                Branch? Branch,
                Sex? Sex)>();

            int lastRow =
                sheet.LastRowUsed()?.RowNumber() ?? 1;

            // ELSŐ LÉPÉS:
            // Az Excel összes sorának ellenőrzése.
            // Ebben a szakaszban nem mentünk adatot.

            for (int row = 2; row <= lastRow; row++)
            {
                string name = ReadCell(row, "nev");
                string description = ReadCell(row, "leiras");
                string branchName = ReadCell(row, "agazat");
                string sexName = ReadCell(row, "nem");
                string locationName = ReadCell(row, "terem");
                string capacityText = ReadCell(row, "kapacitas");
                string beginText = ReadCell(row, "kezdodatum");
                string endText = ReadCell(row, "vegdatum");
                string supervisorsText = ReadCell(row, "felugyelok");

                if (new[]
                {
                    name,
                    description,
                    branchName,
                    sexName,
                    locationName,
                    capacityText,
                    beginText,
                    endText,
                    supervisorsText
                }.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                int errorCountBefore = result.Errors.Count;

                // KÖTELEZŐ SZÖVEGES MEZŐK

                if (string.IsNullOrWhiteSpace(name))
                {
                    AddError(
                        row,
                        "nev",
                        "A program neve nem lehet üres.");
                }

                if (string.IsNullOrWhiteSpace(description))
                {
                    AddError(
                        row,
                        "leiras",
                        "A program leírása nem lehet üres.");
                }

                if (string.IsNullOrWhiteSpace(locationName))
                {
                    AddError(
                        row,
                        "terem",
                        "A terem megadása kötelező.");
                }

                // KAPACITÁS

                bool validCapacity = int.TryParse(
                    capacityText,
                    out int capacity) && capacity > 0;

                if (!validCapacity)
                {
                    AddError(
                        row,
                        "kapacitas",
                        $"A megadott érték: \"{capacityText}\". " +
                        "A kapacitásnak 0-nál nagyobb " +
                        "egész számnak kell lennie.");
                }

                // FELÜGYELŐK SZÁMA

                bool validSupervisors = int.TryParse(
                    supervisorsText,
                    out int supervisors) && supervisors >= 0;

                if (!validSupervisors)
                {
                    AddError(
                        row,
                        "felugyelok",
                        $"A megadott érték: \"{supervisorsText}\". " +
                        "A felügyelők számának " +
                        "0 vagy annál nagyobb egész számnak kell lennie.");
                }

                // KEZDŐDÁTUM

                DateTimeOffset begin = default;
                bool validBegin = true;

                try
                {
                    begin = ParseDate(beginText);
                }
                catch (FormatException ex)
                {
                    validBegin = false;

                    AddError(
                        row,
                        "kezdodatum",
                        ex.Message);
                }

                // VÉGDÁTUM

                DateTimeOffset end = default;
                bool validEnd = true;

                try
                {
                    end = ParseDate(endText);
                }
                catch (FormatException ex)
                {
                    validEnd = false;

                    AddError(
                        row,
                        "vegdatum",
                        ex.Message);
                }

                // DÁTUMOK ELLENŐRZÉSE

                if (validBegin && validEnd && end <= begin)
                {
                    AddError(
                        row,
                        "vegdatum",
                        "A program befejezési időpontjának " +
                        "későbbinek kell lennie, mint a kezdési időpont.");
                }

                if (validBegin &&
                    begin.Date != selectedEvent.EventDate.Date)
                {
                    AddError(
                        row,
                        "kezdodatum",
                        $"A program dátuma ({begin:yyyy.MM.dd}) " +
                        $"nem egyezik a kiválasztott esemény " +
                        $"dátumával ({selectedEvent.EventDate:yyyy.MM.dd}).");
                }

                // ÁGAZAT
                // Üres érték = minden ágazat.

                Branch? branch = null;

                if (!string.IsNullOrWhiteSpace(branchName))
                {
                    branch = branches.FirstOrDefault(b =>
                        Normalize(b.BranchName) ==
                        Normalize(branchName));

                    if (branch == null)
                    {
                        string acceptedBranches = string.Join(
                            ", ",
                            branches.Select(b => b.BranchName));

                        AddError(
                            row,
                            "agazat",
                            $"Ismeretlen ágazat: \"{branchName}\". " +
                            $"Elfogadott értékek: {acceptedBranches}. " +
                            "A mező üresen is hagyható.");
                    }
                }

                // NEM
                // Üres vagy UNISEX = mindenki jelentkezhet.

                Sex? sex = null;

                if (!string.IsNullOrWhiteSpace(sexName)
                    && Normalize(sexName) != "UNISEX")
                {
                    sex = sexes.FirstOrDefault(s =>
                        Normalize(s.SexName) ==
                        Normalize(sexName));

                    if (sex == null)
                    {
                        string acceptedSexes = string.Join(
                            ", ",
                            sexes.Select(s => s.SexName));

                        AddError(
                            row,
                            "nem",
                            $"Ismeretlen érték: \"{sexName}\". " +
                            $"Elfogadott értékek: {acceptedSexes}, Unisex. " +
                            "A mező üresen is hagyható.");
                    }
                }

                // Csak hibamentes sor kerülhet a listába.

                if (result.Errors.Count > errorCountBefore)
                {
                    continue;
                }

                validRows.Add((
                    row,
                    new ProgramImportDto
                    {
                        Name = name,
                        Description = description,
                        BranchName = branchName,
                        SexName = sexName,
                        LocationName = locationName,
                        Capacity = capacity,
                        BeginDate = begin,
                        EndDate = end,
                        RequiredSupervisors = supervisors
                    },
                    branch,
                    sex
                ));
            }

            // Ha bármelyik sor hibás, az egész import sikertelen.

            if (result.Errors.Count > 0)
            {
                result.ImportedCount = 0;
                return result;
            }

            if (validRows.Count == 0)
            {
                result.Errors.Add(new ProgramImportErrorDto
                {
                    RowNumber = 0,
                    Message = "Az Excel nem tartalmaz importálható programokat."
                });

                result.SkippedCount++;
                return result;
            }

            // MÁSODIK LÉPÉS:
            // Ütközések ellenőrzése mentés nélkül.

            var checkedSessions = new List<(
                int Row,
                string PlanName,
                int? BranchId,
                int? SexId,
                string LocationName,
                DateTimeOffset Begin,
                DateTimeOffset End)>();

            foreach (var item in validRows)
            {
                var data = item.Data;

                bool duplicatePlan = sessions.Any(s =>
                    Normalize(s.Plan.PlanName) ==
                        Normalize(data.Name)
                    && s.Plan.BranchId == item.Branch?.BranchId
                    && s.Plan.SexId == item.Sex?.SexId
                    && s.TimeSlot.BeginDate < data.EndDate
                    && data.BeginDate < s.TimeSlot.EndDate);

                duplicatePlan = duplicatePlan ||
                    checkedSessions.Any(s =>
                        Normalize(s.PlanName) ==
                            Normalize(data.Name)
                        && s.BranchId == item.Branch?.BranchId
                        && s.SexId == item.Sex?.SexId
                        && s.Begin < data.EndDate
                        && data.BeginDate < s.End);

                bool locationConflict = sessions.Any(s =>
                    Normalize(s.Location.LocationName) ==
                        Normalize(data.LocationName)
                    && s.TimeSlot.BeginDate < data.EndDate
                    && data.BeginDate < s.TimeSlot.EndDate);

                locationConflict = locationConflict ||
                    checkedSessions.Any(s =>
                        Normalize(s.LocationName) ==
                            Normalize(data.LocationName)
                        && s.Begin < data.EndDate
                        && data.BeginDate < s.End);

                if (duplicatePlan)
                {
                    AddError(
                        item.Row,
                        "nev",
                        $"A(z) \"{data.Name}\" program " +
                        "már szerepel ebben az időszakban.");
                }

                if (locationConflict)
                {
                    AddError(
                        item.Row,
                        "terem",
                        $"A(z) \"{data.LocationName}\" terem " +
                        "már foglalt a megadott időszakban.");
                }

                checkedSessions.Add((
                    item.Row,
                    data.Name,
                    item.Branch?.BranchId,
                    item.Sex?.SexId,
                    data.LocationName,
                    data.BeginDate,
                    data.EndDate
                ));
            }

            // Ütközés esetén semmit nem mentünk.

            if (result.Errors.Count > 0)
            {
                result.ImportedCount = 0;
                return result;
            }

            // HARMADIK LÉPÉS:
            // Csak teljesen hibamentes Excel esetén mentünk.

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            foreach (var item in validRows)
            {
                var data = item.Data;

                // PROGRAM KERESÉSE VAGY LÉTREHOZÁSA

                var plan = plans.FirstOrDefault(p =>
                    Normalize(p.PlanName) ==
                        Normalize(data.Name)
                    && p.BranchId == item.Branch?.BranchId
                    && p.SexId == item.Sex?.SexId);

                if (plan == null)
                {
                    plan = new Plan
                    {
                        PlanName = data.Name,
                        PlanDescription = data.Description,
                        Branch = item.Branch,
                        Sex = item.Sex
                    };

                    _context.Plans.Add(plan);
                    plans.Add(plan);
                }

                // IDŐSÁV KERESÉSE VAGY LÉTREHOZÁSA

                var slot = slots.FirstOrDefault(t =>
                    t.BeginDate.ToUniversalTime() ==
                        data.BeginDate.ToUniversalTime()
                    && t.EndDate.ToUniversalTime() ==
                        data.EndDate.ToUniversalTime());

                if (slot == null)
                {
                    slot = new TimeSlot
                    {
                        TimeSlotName =
                            $"{data.BeginDate:HH:mm} - " +
                            $"{data.EndDate:HH:mm}",
                        BeginDate = data.BeginDate,
                        EndDate = data.EndDate,
                        Event = selectedEvent
                    };

                    _context.TimeSlots.Add(slot);
                    slots.Add(slot);
                }

                // TEREM KERESÉSE VAGY LÉTREHOZÁSA

                var location = locations.FirstOrDefault(l =>
                    Normalize(l.LocationName) ==
                    Normalize(data.LocationName));

                if (location == null)
                {
                    location = new Location
                    {
                        LocationName = data.LocationName
                    };

                    _context.Locations.Add(location);
                    locations.Add(location);
                }

                // PROGRAMALKALOM LÉTREHOZÁSA

                var session = new PlanSession
                {
                    Plan = plan,
                    TimeSlot = slot,
                    Location = location,
                    Capacity = data.Capacity,
                    RequiredSupervisors =
                        data.RequiredSupervisors
                };

                _context.PlanSessions.Add(session);

                result.ImportedCount++;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return result;
        }

        // =========================================
        // IMPORTÁLÁSHOZ VÁLASZTHATÓ ESEMÉNYEK
        // =========================================

        public async Task<List<EventOptionDto>> GetAvailableEventsAsync()
        {
            return await _context.Events
                .AsNoTracking()
                .OrderByDescending(e => e.EventId)
                .Select(e => new EventOptionDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName
                })
                .ToListAsync();
        }
    }
}