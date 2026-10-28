namespace ChoiceOfCareerEgressy.Dto.UserDto
{
    public record ExportEventOptionDto(int Id, string Name);
    public record ExportGradeOptionDto(int Id, string Name);
    public record ExportPlanOptionDto(int Id, string Name);

    public record ExportTimeSlotOptionDto(
        int Id,
        int EventId,
        int PlanId,
        string Name,
        int Number);

    public class ExcelExportOptionsDto
    {
        public List<ExportEventOptionDto> Events { get; set; } = new();
        public List<ExportGradeOptionDto> Grades { get; set; } = new();
        public List<ExportPlanOptionDto> Plans { get; set; } = new();
        public List<ExportTimeSlotOptionDto> TimeSlots { get; set; } = new();
    }
}
