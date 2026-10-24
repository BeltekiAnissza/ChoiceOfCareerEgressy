using System.ComponentModel.DataAnnotations;

namespace ChoiceOfCareerEgressy.Dto.ProgramDto
{
    public class CreateEventDto
    {
        [Required(ErrorMessage = "Add meg az esemény nevét!")]
        public string EventName { get; set; } = "";

        [Required]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        [Required]
        public DateTime RegistrationStart { get; set; }

        [Required]
        public DateTime RegistrationEnd { get; set; }

        public bool IsRegistrationEnabled { get; set; }

        [Required]
        public DateTime FirstSlotStart { get; set; }

        [Required]
        public DateTime FirstSlotEnd { get; set; }

        [Required]
        public DateTime SecondSlotStart { get; set; }

        [Required]
        public DateTime SecondSlotEnd { get; set; }
    }

    public class CreatedEventDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = "";
        public DateTimeOffset EventDate { get; set; }
    }
}
