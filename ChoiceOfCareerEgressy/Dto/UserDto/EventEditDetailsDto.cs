namespace ChoiceOfCareerEgressy.Dto.UserDto
{
    public class EventEditDetailsDto
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateTimeOffset EventDate { get; set; }

        public DateTimeOffset RegistrationStart { get; set; }

        public DateTimeOffset RegistrationEnd { get; set; }

        public bool IsRegistrationEnabled { get; set; }

        public List<EventEditTimeSlotDto> TimeSlots { get; set; }
            = new();
    }

    public class EventEditTimeSlotDto
    {
        public int TimeSlotId { get; set; }

        public string TimeSlotName { get; set; } = string.Empty;

        public DateTimeOffset BeginDate { get; set; }

        public DateTimeOffset EndDate { get; set; }

        public bool IsProtected { get; set; }
    }
}
