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

}
