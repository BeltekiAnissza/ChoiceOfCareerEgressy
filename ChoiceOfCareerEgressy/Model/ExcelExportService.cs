using ClosedXML.Excel;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ChoiceOfCareerEgressy.Model;

public class ExcelExportService
{
    private readonly ChoiceOfCareerPersistence _db;

    public ExcelExportService(ChoiceOfCareerPersistence db)
    {
        _db = db;
    }

    // =====================================================
    // OSZTÁLYFŐNÖK OSZTÁLYÁNAK LEKÉRDEZÉSE
    // =====================================================

    public async Task<int> GetOwnGradeIdAsync(int userId)
    {
        var gradeId = await _db.Users
            .AsNoTracking()
            .Where(u =>
                u.UserId == userId &&
                u.Role == RoleChoice.Teacher)
            .Select(u => u.GradeId)
            .FirstOrDefaultAsync();

        return gradeId
            ?? throw new UnauthorizedAccessException(
                "Nem vagy osztályfőnök.");
    }

    // =====================================================
    // OSZTÁLYONKÉNTI EXCEL EXPORTÁLÁS
    // =====================================================

    public async Task<byte[]> ExportGradeAsync(int eventId, int gradeId)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId)
            ?? throw new InvalidOperationException(
                "Az esemény nem található.");

        var grade = await _db.Grades
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GradeId == gradeId)
            ?? throw new InvalidOperationException(
                "Az osztály nem található.");

        var students = await _db.Users
            .AsNoTracking()
            .Where(u =>
                u.Role == RoleChoice.Student &&
                u.GradeId == gradeId)
            .OrderBy(u => u.UserName)
            .Select(u => new
            {
                u.UserId,
                u.UserName
            })
            .ToListAsync();

        var ids = students
            .Select(s => s.UserId)
            .ToList();

        var applications = await _db.Applications
            .AsNoTracking()
            .Where(a =>
                ids.Contains(a.UserId) &&
                a.PlanSession.TimeSlot.EventId == eventId)
            .Select(a => new
            {
                a.UserId,
                a.PlanSession.Plan.PlanName,
                a.PlanSession.TimeSlot.TimeSlotName,
                a.PlanSession.TimeSlot.BeginDate,
                a.PlanSession.TimeSlot.EndDate,
                a.PlanSession.Location.LocationName,
                a.Status
            })
            .ToListAsync();

        var rows = new List<string[]>();

        foreach (var student in students)
        {
            var own = applications
                .Where(a => a.UserId == student.UserId)
                .OrderBy(a => a.BeginDate)
                .ThenBy(a => a.PlanName)
                .ToList();

            if (own.Count == 0)
            {
                rows.Add(new[]
                {
                    student.UserName,
                    grade.GradeName,
                    "Nincs jelentkezés",
                    "",
                    "",
                    "",
                    ""
                });
            }

            foreach (var a in own)
            {
                rows.Add(new[]
                {
                    student.UserName,
                    grade.GradeName,
                    a.PlanName,
                    a.TimeSlotName,
                    $"{a.BeginDate:yyyy.MM.dd HH:mm} – {a.EndDate:HH:mm}",
                    a.LocationName,
                    a.Status.ToString()
                });
            }
        }

        return BuildWorkbook(
            $"{grade.GradeName} – {ev.EventName}",
            new[]
            {
                "Diák neve",
                "Osztály",
                "Program",
                "Idősáv",
                "Időpont",
                "Helyszín",
                "Jelentkezés állapota"
            },
            rows);
    }

    // =====================================================
    // PROGRAMONKÉNTI EXCEL EXPORTÁLÁS
    // =====================================================

    public async Task<byte[]> ExportPlanAsync(int eventId, int planId, int timeSlotId)
    {
        var ev = await _db.Events
         .AsNoTracking()
         .FirstOrDefaultAsync(e => e.EventId == eventId)
         ?? throw new InvalidOperationException(
             "Az esemény nem található.");

        var plan = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanId == planId)
            ?? throw new InvalidOperationException(
                "A program nem található.");

        var slot = await _db.TimeSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.TimeSlotId == timeSlotId &&
                t.EventId == eventId)
            ?? throw new InvalidOperationException(
                "Az idősáv nem található.");

        // Ellenőrizzük, hogy a program szerepel-e az idősávban.

        bool sessionExists = await _db.PlanSessions
            .AsNoTracking()
            .AnyAsync(s =>
                s.PlanId == planId &&
                s.TimeSlotId == timeSlotId);

        if (!sessionExists)
        {
            throw new InvalidOperationException(
                "A program nem érhető el a kiválasztott idősávban.");
        }

        // Csak a kiválasztott program és idősáv jelentkezései.

        var data = await _db.Applications
            .AsNoTracking()
            .Where(a =>
                a.PlanSession.PlanId == planId &&
                a.PlanSession.TimeSlotId == timeSlotId &&
                a.PlanSession.TimeSlot.EventId == eventId)
            .Select(a => new
            {
                a.User.UserName,

                GradeName = a.User.Grade != null
                    ? a.User.Grade.GradeName
                    : "–",

                a.PlanSession.TimeSlot.TimeSlotName,
                a.PlanSession.TimeSlot.BeginDate,
                a.PlanSession.TimeSlot.EndDate,
                a.PlanSession.Location.LocationName,
                a.Status
            })
            .ToListAsync();

        // Rendezés memóriában.

        var rows = data
            .OrderBy(a => a.UserName)
            .Select(a => new[]
            {
            a.UserName,
            a.GradeName,
            plan.PlanName,
            a.TimeSlotName,
            $"{a.BeginDate:yyyy.MM.dd HH:mm} – {a.EndDate:HH:mm}",
            a.LocationName,
            a.Status.ToString()
            })
            .ToList();

        return BuildWorkbook(
            $"{plan.PlanName} – {slot.TimeSlotName} – {ev.EventName}",
            new[]
            {
            "Diák neve",
            "Osztály",
            "Program",
            "Idősáv",
            "Időpont",
            "Helyszín",
            "Jelentkezés állapota"
            },
            rows);
    }
    

    // =====================================================
    // OSZTÁLYONKÉNTI EXPORT FÁJLNEVE
    //
    // Példa:
    // 2026_10i.xlsx
    // =====================================================

    public async Task<string> GetGradeExportFileNameAsync(int eventId, int gradeId)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId)
            ?? throw new InvalidOperationException(
                "Az esemény nem található.");

        var grade = await _db.Grades
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GradeId == gradeId)
            ?? throw new InvalidOperationException(
                "Az osztály nem található.");

        int year = ev.EventDate.Year;

        string gradeName = NormalizeGradeName(
            grade.GradeName);

        return $"{year}_{gradeName}.xlsx";
    }

    // =====================================================
    // PROGRAMONKÉNTI EXPORT FÁJLNEVE
    //
    // Példa:
    // 2026_noi_joga_1_idosav.xlsx
    // =====================================================

    public async Task<string> GetPlanExportFileNameAsync(int eventId, int planId, int timeSlotId)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId)
            ?? throw new InvalidOperationException(
                "Az esemény nem található.");

        var plan = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanId == planId)
            ?? throw new InvalidOperationException(
                "A program nem található.");

        // Idősávok lekérdezése

        var slots = await _db.TimeSlots
            .AsNoTracking()
            .Where(t => t.EventId == eventId)
            .Select(t => new
            {
                t.TimeSlotId,
                t.BeginDate
            })
            .ToListAsync();

        // Idősávok rendezése memóriában
        // SQLite DateTimeOffset hiba elkerülése

        var orderedSlots = slots
            .OrderBy(t => t.BeginDate)
            .ThenBy(t => t.TimeSlotId)
            .ToList();

        // Az idősáv sorszámának meghatározása

        int slotNumber = orderedSlots.FindIndex(
            t => t.TimeSlotId == timeSlotId) + 1;

        if (slotNumber == 0)
        {
            throw new InvalidOperationException(
                "A kiválasztott idősáv nem található az eseményben.");
        }

        int year = ev.EventDate.Year;

        string planName = NormalizeFileName(
            plan.PlanName);

        return $"{year}_{planName}_{slotNumber}_idosav.xlsx";
    }

    // =====================================================
    // OSZTÁLYNÉV NORMALIZÁLÁSA
    //
    // 10.I  -> 10i
    // 11.A  -> 11a
    // 12 B  -> 12b
    // =====================================================

    private static string NormalizeGradeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "ismeretlen";

        string normalized = RemoveAccents(value)
            .ToLowerInvariant();

        // Csak betűk és számok maradnak

        normalized = Regex.Replace(
            normalized,
            @"[^a-z0-9]",
            "");

        return string.IsNullOrWhiteSpace(normalized)
            ? "ismeretlen"
            : normalized;
    }

    // =====================================================
    // PROGRAMNÉV NORMALIZÁLÁSA
    //
    // Női jóga       -> noi_joga
    // Webfejlesztés  -> webfejlesztes
    // Robotika       -> robotika
    // =====================================================

    private static string NormalizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "ismeretlen";

        string normalized = RemoveAccents(value)
            .ToLowerInvariant();

        // Szóközök és speciális karakterek
        // alulvonásra cserélése

        normalized = Regex.Replace(
            normalized,
            @"[^a-z0-9]+",
            "_");

        normalized = normalized.Trim('_');

        return string.IsNullOrWhiteSpace(normalized)
            ? "ismeretlen"
            : normalized;
    }

    // =====================================================
    // ÉKEZETEK ELTÁVOLÍTÁSA
    // =====================================================

    private static string RemoveAccents(string value)
    {
        string normalized = value.Normalize(
            NormalizationForm.FormD);

        var characters = normalized
            .Where(c =>
                CharUnicodeInfo.GetUnicodeCategory(c)
                != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(characters)
            .Normalize(NormalizationForm.FormC);
    }

    // =====================================================
    // EXCEL MUNKAFÜZET LÉTREHOZÁSA
    // =====================================================

    private static byte[] BuildWorkbook(string title, string[] headers, List<string[]> rows)
    {
        using var workbook = new XLWorkbook();

        var ws = workbook.Worksheets.Add(
            "Jelentkezések");

        // Cím

        ws.Cell(1, 1).Value = title;

        ws.Range(
            1,
            1,
            1,
            headers.Length)
            .Merge();

        ws.Cell(1, 1).Style.Font.Bold = true;

        ws.Cell(1, 1).Style.Font.FontSize = 16;

        // Fejlécek

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(3, c + 1).Value = headers[c];
        }

        // Adatsorok

        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(r + 4, c + 1).Value = rows[r][c];
            }
        }

        // Fejléc formázása

        var header = ws.Range(
            3,
            1,
            3,
            headers.Length);

        header.Style.Font.Bold = true;

        header.Style.Fill.BackgroundColor =
            XLColor.FromHtml("#E9E5F7");

        // Szűrő

        ws.Range(
            3,
            1,
            Math.Max(3, rows.Count + 3),
            headers.Length)
            .SetAutoFilter();

        // Első három sor rögzítése

        ws.SheetView.FreezeRows(3);

        // Oszlopszélességek

        ws.Columns().AdjustToContents();

        foreach (var column in ws.ColumnsUsed())
        {
            if (column.Width > 55)
            {
                column.Width = 55;
            }
        }

        // Excel mentése memóriába

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}