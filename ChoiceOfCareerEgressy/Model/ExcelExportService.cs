using ClosedXML.Excel;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model;

public class ExcelExportService
{
    private readonly ChoiceOfCareerPersistence _db;
    public ExcelExportService(ChoiceOfCareerPersistence db) => _db = db;

    public async Task<int> GetOwnGradeIdAsync(int userId)
    {
        var gradeId = await _db.Users.AsNoTracking()
            .Where(u => u.UserId == userId && u.Role == RoleChoice.Teacher)
            .Select(u => u.GradeId).FirstOrDefaultAsync();
        return gradeId ?? throw new UnauthorizedAccessException("Nem vagy osztályfőnök.");
    }

    public async Task<byte[]> ExportGradeAsync(int eventId, int gradeId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId)
            ?? throw new InvalidOperationException("Az esemény nem található.");
        var grade = await _db.Grades.AsNoTracking().FirstOrDefaultAsync(g => g.GradeId == gradeId)
            ?? throw new InvalidOperationException("Az osztály nem található.");
        var students = await _db.Users.AsNoTracking()
            .Where(u => u.Role == RoleChoice.Student && u.GradeId == gradeId)
            .OrderBy(u => u.UserName).Select(u => new { u.UserId, u.UserName }).ToListAsync();
        var ids = students.Select(s => s.UserId).ToList();
        var applications = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.UserId) && a.PlanSession.TimeSlot.EventId == eventId)
            .Select(a => new {
                a.UserId, a.PlanSession.Plan.PlanName, a.PlanSession.TimeSlot.TimeSlotName,
                a.PlanSession.TimeSlot.BeginDate, a.PlanSession.TimeSlot.EndDate,
                a.PlanSession.Location.LocationName, a.Status
            }).ToListAsync();
        var rows = new List<string[]>();
        foreach (var student in students)
        {
            var own = applications.Where(a => a.UserId == student.UserId)
                .OrderBy(a => a.BeginDate).ThenBy(a => a.PlanName).ToList();
            if (own.Count == 0)
                rows.Add(new[] { student.UserName, grade.GradeName, "Nincs jelentkezés", "", "", "", "" });
            foreach (var a in own)
                rows.Add(new[] { student.UserName, grade.GradeName, a.PlanName,
                    a.TimeSlotName, $"{a.BeginDate:yyyy.MM.dd HH:mm} – {a.EndDate:HH:mm}",
                    a.LocationName, a.Status.ToString() });
        }
        return BuildWorkbook($"{grade.GradeName} – {ev.EventName}",
            new[] { "Diák neve", "Osztály", "Program", "Idősáv", "Időpont", "Helyszín", "Jelentkezés állapota" }, rows);
    }


    public async Task<byte[]> ExportPlanAsync(int eventId, int planId)
    {
        // Esemény lekérdezése
        var ev = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId)
            ?? throw new InvalidOperationException(
                "Az esemény nem található.");

        // Program lekérdezése
        var plan = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanId == planId)
            ?? throw new InvalidOperationException(
                "A program nem található.");

        // Jelentkezések lekérdezése az adatbázisból
        var data = await _db.Applications
            .AsNoTracking()
            .Where(a =>
                a.PlanSession.PlanId == planId &&
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

        // Rendezés memóriában (SQLite DateTimeOffset hiba elkerülése)
        var sortedData = data
            .OrderBy(a => a.BeginDate)
            .ThenBy(a => a.UserName)
            .ToList();

        // Excel sorok összeállítása
        var rows = sortedData
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

        // Excel fájl létrehozása
        return BuildWorkbook(
            $"{plan.PlanName} – {ev.EventName}",
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


    private static byte[] BuildWorkbook(string title, string[] headers, List<string[]> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Jelentkezések");
        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, headers.Length).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        for (int c = 0; c < headers.Length; c++) ws.Cell(3, c + 1).Value = headers[c];
        for (int r = 0; r < rows.Count; r++)
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(r + 4, c + 1).Value = rows[r][c];
        var header = ws.Range(3, 1, 3, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9E5F7");
        ws.Range(3, 1, Math.Max(3, rows.Count + 3), headers.Length).SetAutoFilter();
        ws.SheetView.FreezeRows(3);
        ws.Columns().AdjustToContents();
        foreach (var column in ws.ColumnsUsed()) if (column.Width > 55) column.Width = 55;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
