namespace ChoiceOfCareerEgressy.Dto.UserDto.Student
{
    public class StudentEventDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = "";

        public DateTimeOffset EventDate { get; set; }
        public DateTimeOffset RegistrationStart { get; set; }
        public DateTimeOffset RegistrationEnd { get; set; }

        public bool CanRegister { get; set; }
        public bool HasRegistration { get; set; }

        public List<StudentTimeSlotDto> TimeSlots { get; set; } = new();
    }

    public class StudentRegistrationDto
    {
        public int EventId { get; set; }
        public int FirstPlanSessionId { get; set; }
        public int SecondPlanSessionId { get; set; }
    }
    public class StudentProgramDto
    {
        public int PlanSessionId { get; set; }
        public int PlanId { get; set; }
        public int TimeSlotId { get; set; }

        public string PlanName { get; set; } = "";
        public string Description { get; set; } = "";
        public string LocationName { get; set; } = "";

        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public int Capacity { get; set; }
        public int AvailablePlaces { get; set; }
    }

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
