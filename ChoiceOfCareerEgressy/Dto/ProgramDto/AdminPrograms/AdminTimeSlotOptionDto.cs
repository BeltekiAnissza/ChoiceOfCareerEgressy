namespace ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms
{
    public class AdminTimeSlotOptionDto
    {
        public int TimeSlotId { get; set; }
        public int EventId { get; set; }

        public string TimeSlotName { get; set; } = "";

        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public string DisplayName =>
            $"{TimeSlotName} " +
            $"({BeginDate:HH:mm} - {EndDate:HH:mm})";
    }
}
