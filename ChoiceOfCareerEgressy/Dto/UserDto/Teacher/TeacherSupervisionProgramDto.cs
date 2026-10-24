namespace ChoiceOfCareerEgressy.Dto.UserDto.Teacher
{
    public class TeacherSupervisionProgramDto
    {
        public int PlanSessionId { get; set; }
        public string PlanName { get; set; } = "";
        public string LocationName { get; set; } = "";

        public int RequiredSupervisors { get; set; }
        public int RegisteredSupervisors { get; set; }

        public int AvailablePlaces =>
            Math.Max(0, RequiredSupervisors - RegisteredSupervisors);

        public bool IsMySupervision { get; set; }
    }

    public class TeacherSupervisionSlotDto
    {
        public int TimeSlotId { get; set; }
        public string TimeSlotName { get; set; } = "";

        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public List<TeacherSupervisionProgramDto> Programs { get; set; }
            = new();
    }

    public class TeacherSupervisionEventDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = "";
        public DateTimeOffset EventDate { get; set; }

        public List<TeacherSupervisionSlotDto> TimeSlots { get; set; }
            = new();
    }

    public class TeacherSupervisionRequestDto
    {
        public int PlanSessionId { get; set; }
    }
}
