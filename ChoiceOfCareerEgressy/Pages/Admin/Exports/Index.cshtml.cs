
using ChoiceOfCareerEgressy.Model;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Pages.Admin.Exports;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ChoiceOfCareerPersistence _db;
    private readonly ExcelExportService _export;

    public IndexModel( ChoiceOfCareerPersistence db, ExcelExportService export)
    {
        _db = db;
        _export = export;
    }

    public List<EventOption> Events { get; private set; } = new();
    public List<GradeOption> Grades { get; private set; } = new();
    public List<PlanOption> Plans { get; private set; } = new();

    public string? ErrorMessage { get; private set; }

    public record EventOption(int Id, string Name);
    public record GradeOption(int Id, string Name);
    public record PlanOption(int Id, string Name);

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    private async Task LoadOptionsAsync()
    {
        var events = await _db.Events
            .AsNoTracking()
            .Select(e => new
            {
                e.EventId,
                e.EventName,
                e.EventDate
            })
            .ToListAsync();

        Events = events
            .OrderByDescending(e => e.EventDate)
            .Select(e => new EventOption(
                e.EventId,
                e.EventName))
            .ToList();

        Grades = await _db.Grades
            .AsNoTracking()
            .OrderBy(g => g.GradeName)
            .Select(g => new GradeOption(
                g.GradeId,
                g.GradeName))
            .ToListAsync();

        Plans = await _db.PlanSessions
            .AsNoTracking()
            .Select(s => new
            {
                s.PlanId,
                s.Plan.PlanName
            })
            .Distinct()
            .OrderBy(s => s.PlanName)
            .Select(s => new PlanOption(
                s.PlanId,
                s.PlanName))
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetGradeAsync(   int eventId, int gradeId)
    {
        if (eventId <= 0 || gradeId <= 0)
        {
            return BadRequest(
                "Válassz ki egy eseményt és egy osztályt.");
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
            ErrorMessage = ex.Message;
            await LoadOptionsAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnGetPlanAsync( int eventId, int planId)
    {
        if (eventId <= 0 || planId <= 0)
        {
            return BadRequest(
                "Válassz ki egy eseményt és egy programot.");
        }

        try
        {
            var bytes = await _export.ExportPlanAsync(
                eventId,
                planId);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"program_{planId}_esemeny_{eventId}.xlsx");
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            await LoadOptionsAsync();
            return Page();
        }
    }
}
