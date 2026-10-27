using System.ComponentModel.DataAnnotations;

namespace ChoiceOfCareerEgressy.Dto.ProgramDto
{
    public class CreateTimeSlotDto
    {
        [Required(ErrorMessage = "Az idősáv neve kötelező.")]
        public string TimeSlotName { get; set; } = string.Empty;

        public DateTime BeginDate { get; set; }

        public DateTime EndDate { get; set; }
    }

    public class CreateEventDto
    {
        [Required(ErrorMessage = "Add meg az esemény nevét!")]
        public string EventName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        [Required]
        public DateTime RegistrationStart { get; set; }

        [Required]
        public DateTime RegistrationEnd { get; set; }

        public bool IsRegistrationEnabled { get; set; }

        public List<CreateTimeSlotDto> TimeSlots { get; set; } = new();
    }

    public class CreatedEventDto
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateTimeOffset EventDate { get; set; }
    }
}