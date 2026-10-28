namespace ChoiceOfCareerEgressy.Dto.UserDto
{
    public class EventListItemDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTimeOffset EventDate { get; set; }
        public DateTimeOffset RegistrationStart { get; set; }
        public DateTimeOffset RegistrationEnd { get; set; }
        public bool IsRegistrationEnabled { get; set; }
    }

    public class EventDetailsDto : EventListItemDto
    {
        public List<EventTimeSlotDto> TimeSlots { get; set; } = new();
    }

    public class EventTimeSlotDto
    {
        public int TimeSlotId { get; set; }
        public string TimeSlotName { get; set; } = string.Empty;
        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }
}
