namespace ChoiceOfCareerEgressy.Dto.UserDto.Student
{
    public class StudentTimeSlotDto
    {
        public int TimeSlotId { get; set; }
        public string TimeSlotName { get; set; } = "";

        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public int? SelectedPlanSessionId { get; set; }

        public List<StudentProgramDto> Programs { get; set; } = new();
    }
}
