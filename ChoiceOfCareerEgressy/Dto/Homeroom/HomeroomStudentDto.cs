namespace ChoiceOfCareerEgressy.Dto.Homeroom
{
    public class HomeroomStudentDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string EmailAddress { get; set; } = "";

        public int RegistrationCount { get; set; }

        public List<HomeroomApplicationDto> Applications { get; set; }
            = new();
        public List<HomeroomStudentSlotDto> TimeSlots { get; set; } = new();
    }
}

