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

    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public IndexModel(ChoiceOfCareerPersistence db, ExcelExportService export)
    {
        _db = db;
        _export = export;
    }

    public List<EventOption> Events { get; private set; } = new();
    public List<GradeOption> Grades { get; private set; } = new();
    public List<PlanOption> Plans { get; private set; } = new();
    public List<TimeSlotOption> TimeSlots { get; private set; } = new();

    public string? ErrorMessage { get; private set; }

    public record EventOption(int Id, string Name);
    public record GradeOption(int Id, string Name);
    public record PlanOption(int Id, string Name);

    public record TimeSlotOption(int Id, int EventId, int PlanId, string Name, int Number);

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    private async Task LoadOptionsAsync()
    {
        // =====================================================
        // ESEMÉNYEK
        // =====================================================

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

        // =====================================================
        // OSZTÁLYOK
        // =====================================================

        Grades = await _db.Grades
            .AsNoTracking()
            .OrderBy(g => g.GradeName)
            .Select(g => new GradeOption(
                g.GradeId,
                g.GradeName))
            .ToListAsync();

        // =====================================================
        // PROGRAMOK
        // =====================================================

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

        // =====================================================
        // PROGRAMOKHOZ TARTOZÓ IDŐSÁVOK
        // =====================================================

        var sessions = await _db.PlanSessions
            .AsNoTracking()
            .Select(s => new
            {
                s.PlanId,
                s.TimeSlotId,
                s.TimeSlot.EventId,
                s.TimeSlot.TimeSlotName,
                s.TimeSlot.BeginDate
            })
            .Distinct()
            .ToListAsync();

        // Eseményenként számozzuk az idősávokat.
        // SQLite DateTimeOffset rendezési probléma elkerülése.

        var slotNumbers = sessions
            .GroupBy(s => s.EventId)
            .SelectMany(group => group
                .GroupBy(s => s.TimeSlotId)
                .Select(g => g.First())
                .OrderBy(s => s.BeginDate)
                .ThenBy(s => s.TimeSlotId)
                .Select((s, index) => new
                {
                    s.EventId,
                    s.TimeSlotId,
                    Number = index + 1
                }))
            .ToList();

        TimeSlots = sessions
            .Select(s => new TimeSlotOption(
                s.TimeSlotId,
                s.EventId,
                s.PlanId,
                s.TimeSlotName,
                slotNumbers.First(n =>
                    n.EventId == s.EventId &&
                    n.TimeSlotId == s.TimeSlotId).Number))
            .OrderBy(s => s.EventId)
            .ThenBy(s => s.PlanId)
            .ThenBy(s => s.Number)
            .ToList();
    }

    // =====================================================
    // OSZTÁLY EXPORT
    // =====================================================

    public async Task<IActionResult> OnGetGradeAsync(int eventId, int gradeId)
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

            var fileName =
                await _export.GetGradeExportFileNameAsync(
                    eventId,
                    gradeId);

            return File(
                bytes,
                ExcelContentType,
                fileName);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;

            await LoadOptionsAsync();

            return Page();
        }
    }

    // =====================================================
    // PROGRAM EXPORT
    // =====================================================

    public async Task<IActionResult> OnGetPlanAsync(int eventId, int planId, int timeSlotId, bool includeSupervisors = false)
    {
        if (eventId <= 0 || planId <= 0 || timeSlotId <= 0)
        {
            return BadRequest(
                "Válassz ki egy eseményt, programot és idősávot.");
        }

        try
        {
            var bytes = await _export.ExportPlanAsync(
                eventId,
                planId,
                timeSlotId,
                includeSupervisors);

            var fileName =
                await _export.GetPlanExportFileNameAsync(
                    eventId,
                    planId,
                    timeSlotId);

            return File(
                bytes,
                ExcelContentType,
                fileName);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;

            await LoadOptionsAsync();

            return Page();
        }
    }

    // =====================================================
    // TANÁRI FELÜGYELETI JELENTKEZÉSEK EXPORTÁLÁSA
    // =====================================================

    public async Task<IActionResult> OnGetTeachersAsync(int eventId)
    {
        if (eventId <= 0)
        {
            return BadRequest(
                "Válassz ki egy eseményt.");
        }

        try
        {
            // Excel elkészítése

            var bytes = await _export.ExportTeachersAsync(eventId);

            // Fájlnév elkészítése

            var fileName =
                await _export.GetTeachersExportFileNameAsync(eventId);

            // Excel letöltése

            return File(
                bytes,
                ExcelContentType,
                fileName);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;

            await LoadOptionsAsync();

            return Page();
        }
    }
}