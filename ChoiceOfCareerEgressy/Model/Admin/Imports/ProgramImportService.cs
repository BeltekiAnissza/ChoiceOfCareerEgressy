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
                    "Hibás dátum. Példa: 2026.11.20 09:00");
            }

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                "Europe/Budapest");

            localDate = DateTime.SpecifyKind(
                localDate,
                DateTimeKind.Unspecified);

            if (timeZone.IsInvalidTime(localDate)
                || timeZone.IsAmbiguousTime(localDate))
            {
                throw new FormatException(
                    "Nem egyértelmű vagy nem létező helyi időpont.");
            }

            return new DateTimeOffset(
                localDate,
                timeZone.GetUtcOffset(localDate));
        }

        public async Task<ProgramImportResultDto> ImportAsync(
            IFormFile file,
            int eventId)
        {
            if (file.Length == 0 || file.Length > 5 * 1024 * 1024)
                throw new InvalidDataException(
                    "A fájl mérete maximum 5 MB lehet.");

            if (!Path.GetExtension(file.FileName)
                .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Csak .xlsx fájl tölthető fel.");
            }

            var selectedEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (selectedEvent == null)
                throw new InvalidDataException(
                    "A kiválasztott esemény nem található.");

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var sheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidDataException(
                    "Az Excel nem tartalmaz munkalapot.");

            var headers = new Dictionary<string, int>();

            foreach (var cell in sheet.Row(1).CellsUsed())
            {
                var header = Normalize(cell.GetString());

                if (!headers.TryAdd(header, cell.Address.ColumnNumber))
                    throw new InvalidDataException(
                        $"Ismétlődő oszlop: {header}");
            }

            string[] requiredHeaders =
            {
            "nev", "leiras", "agazat", "nem", "terem",
            "kapacitas", "kezdodatum", "vegdatum", "felugyelok"
        };

            foreach (var header in requiredHeaders)
            {
                if (!headers.ContainsKey(Normalize(header)))
                    throw new InvalidDataException(
                        $"Hiányzó oszlop: {header}");
            }

            string ReadCell(int row, string column)
            {
                return sheet.Cell(row, headers[Normalize(column)])
                    .GetFormattedString()
                    .Trim();
            }

            var result = new ProgramImportResultDto();

            var branches = await _context.Branches.ToListAsync();
            var sexes = await _context.Sexes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var plans = await _context.Plans.ToListAsync();

            var slots = await _context.TimeSlots
                .Where(t => t.EventId == eventId)
                .ToListAsync();

            var sessions = await _context.PlanSessions
                .Include(s => s.TimeSlot)
                .Include(s => s.Plan)
                .Include(s => s.Location)
                .Where(s => s.TimeSlot.EventId == eventId)
                .ToListAsync();

            // Az import először minden sort ellenőriz.
            // Hibás sor esetén nem mentünk részleges adatokat.
            var validRows = new List<(
                int Row,
                ProgramImportDto Data,
                Branch? Branch,
                Sex? Sex)>();

            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                var name = ReadCell(row, "nev");
                var description = ReadCell(row, "leiras");
                var branchName = ReadCell(row, "agazat");
                var sexName = ReadCell(row, "nem");
                var locationName = ReadCell(row, "terem");
                var capacityText = ReadCell(row, "kapacitas");
                var beginText = ReadCell(row, "kezdodatum");
                var endText = ReadCell(row, "vegdatum");
                var supervisorsText = ReadCell(row, "felugyelok");

                if (new[]
                {
                name, description, branchName, sexName,
                locationName, capacityText, beginText,
                endText, supervisorsText
            }.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(name)
                        || string.IsNullOrWhiteSpace(description)
                        || string.IsNullOrWhiteSpace(sexName)
                        || string.IsNullOrWhiteSpace(locationName))
                    {
                        throw new FormatException(
                            "Hiányzó kötelező szöveges adat.");
                    }

                    if (!int.TryParse(capacityText, out int capacity)
                        || capacity <= 0)
                    {
                        throw new FormatException(
                            "A kapacitásnak pozitív egész számnak kell lennie.");
                    }

                    if (!int.TryParse(supervisorsText, out int supervisors)
                        || supervisors < 0)
                    {
                        throw new FormatException(
                            "A felügyelők száma nem lehet negatív.");
                    }

                    var begin = ParseDate(beginText);
                    var end = ParseDate(endText);

                    if (end <= begin)
                        throw new FormatException(
                            "A végdátumnak későbbinek kell lennie.");

                    if (begin.Date != selectedEvent.EventDate.Date)
                        throw new FormatException(
                            "A program dátuma nem egyezik a kiválasztott esemény napjával.");

                    Branch? branch = null;

                    if (!string.IsNullOrWhiteSpace(branchName))
                    {
                        branch = branches.FirstOrDefault(b =>
                            Normalize(b.BranchName) == Normalize(branchName));

                        if (branch == null)
                            throw new FormatException(
                                $"Ismeretlen ágazat: {branchName}");
                    }

                    Sex? sex = null;

                    if (Normalize(sexName) != "UNISEX")
                    {
                        sex = sexes.FirstOrDefault(s =>
                            Normalize(s.SexName) == Normalize(sexName));

                        if (sex == null)
                            throw new FormatException(
                                $"Ismeretlen nem: {sexName}");
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
                catch (FormatException ex)
                {
                    result.SkippedCount++;
                    result.Errors.Add(new ProgramImportErrorDto
                    {
                        RowNumber = row,
                        Message = ex.Message
                    });
                }
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            foreach (var item in validRows)
            {
                var data = item.Data;

                // Azonos nevű programot újrahasználunk.
                var plan = plans.FirstOrDefault(p =>
                    Normalize(p.PlanName) == Normalize(data.Name)
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

                // Idősáv keresése
                var slot = slots.FirstOrDefault(t =>
                    t.BeginDate.ToUniversalTime()
                        == data.BeginDate.ToUniversalTime()
                    && t.EndDate.ToUniversalTime()
                        == data.EndDate.ToUniversalTime());

                if (slot == null)
                {
                    slot = new TimeSlot
                    {
                        TimeSlotName =
                            $"{data.BeginDate:HH:mm} - {data.EndDate:HH:mm}",
                        BeginDate = data.BeginDate,
                        EndDate = data.EndDate,
                        Event = selectedEvent
                    };

                    _context.TimeSlots.Add(slot);
                    slots.Add(slot);
                }

                // Terem keresése vagy létrehozása
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

                // Azonos program és idősáv ismétlődése
                bool duplicatePlan = sessions.Any(s =>
                    (ReferenceEquals(s.Plan, plan)
                        || s.PlanId != 0 && s.PlanId == plan.PlanId)
                    && (ReferenceEquals(s.TimeSlot, slot)
                        || s.TimeSlotId != 0
                            && s.TimeSlotId == slot.TimeSlotId));

                // A terem nem lehet egyszerre két program helyszíne
                bool locationConflict = sessions.Any(s =>
                    (ReferenceEquals(s.Location, location)
                        || s.LocationId != 0
                            && s.LocationId == location.LocationId)
                    && s.TimeSlot.BeginDate < data.EndDate
                    && data.BeginDate < s.TimeSlot.EndDate);

                if (duplicatePlan || locationConflict)
                {
                    result.SkippedCount++;
                    result.Errors.Add(new ProgramImportErrorDto
                    {
                        RowNumber = item.Row,
                        Message = duplicatePlan
                            ? "A program már szerepel ebben az idősávban."
                            : "A terem ebben az időpontban foglalt."
                    });
                    continue;
                }

                var session = new PlanSession
                {
                    Plan = plan,
                    TimeSlot = slot,
                    Location = location,
                    Capacity = data.Capacity,
                    RequiredSupervisors = data.RequiredSupervisors
                };

                _context.PlanSessions.Add(session);
                sessions.Add(session);

                result.ImportedCount++;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return result;
        }
    }
}
