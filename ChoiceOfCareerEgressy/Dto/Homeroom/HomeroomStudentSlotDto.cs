namespace ChoiceOfCareerEgressy.Dto.Homeroom
{
    public class HomeroomStudentSlotDto
    {
        public int TimeSlotId { get; set; }
        public string TimeSlotName { get; set; } = "";

        public int? ApplicationId { get; set; }
        public int? SelectedPlanSessionId { get; set; }

        public List<HomeroomProgramOptionDto> Programs { get; set; }
            = new();
    }
}
