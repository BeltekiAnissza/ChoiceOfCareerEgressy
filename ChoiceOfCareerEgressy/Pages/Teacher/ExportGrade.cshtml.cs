using System.Security.Claims;
using ChoiceOfCareerEgressy.Model;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Pages.Teacher;

[Authorize(Roles = "Teacher")]
public class ExportGradeModel : PageModel
{
    private readonly ChoiceOfCareerPersistence _db;
    private readonly ExcelExportService _export;
    public ExportGradeModel(ChoiceOfCareerPersistence db, ExcelExportService export)
    { _db = db; _export = export; }
    public List<EventOption> Events { get; private set; } = new();
    public record EventOption(int Id, string Name);

    public async Task<IActionResult> OnGetAsync()
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int actorId))
        {
            return Challenge();
        }

        try
        {
            await _export.GetOwnGradeIdAsync(actorId);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        // Események lekérdezése az adatbázisból
        var events = await _db.Events
            .AsNoTracking()
            .Select(e => new
            {
                e.EventId,
                e.EventName,
                e.EventDate
            })
            .ToListAsync();

        // Rendezés memóriában, mert a SQLite nem támogatja
        // a DateTimeOffset típus szerinti rendezést.
        Events = events
            .OrderByDescending(e => e.EventDate)
            .Select(e => new EventOption(
                e.EventId,
                e.EventName))
            .ToList();

        return Page();
    }


    public async Task<IActionResult> OnGetDownloadAsync(int eventId)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int actorId))
        {
            return Challenge();
        }

        if (eventId <= 0)
        {
            return BadRequest("Válassz ki egy eseményt.");
        }

        int gradeId;

        try
        {
            gradeId = await _export.GetOwnGradeIdAsync(actorId);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        try
        {
            var bytes = await _export.ExportGradeAsync(
                eventId,
                gradeId);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"osztaly_{gradeId}_esemeny_{eventId}.xlsx");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

}
