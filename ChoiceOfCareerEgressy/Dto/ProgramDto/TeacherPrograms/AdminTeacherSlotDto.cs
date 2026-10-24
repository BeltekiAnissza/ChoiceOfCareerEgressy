namespace ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms
{
    public class AdminTeacherSlotDto
    {
        public int TimeSlotId { get; set; }
        public string TimeSlotName { get; set; } = "";

        public int? SupervisionId { get; set; }
        public int? SelectedPlanSessionId { get; set; }

        public string? CurrentProgramName { get; set; }
        public string? CurrentLocationName { get; set; }

        public List<AdminSupervisionOptionDto> Programs { get; set; }
            = new();
    }
}
