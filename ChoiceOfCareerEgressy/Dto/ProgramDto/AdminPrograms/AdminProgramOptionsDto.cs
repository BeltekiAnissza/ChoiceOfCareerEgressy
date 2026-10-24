namespace ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms
{
    public class AdminProgramOptionsDto
    {
        public List<AdminProgramOptionDto> Events { get; set; }
           = new();

        public List<AdminTimeSlotOptionDto> TimeSlots { get; set; }
            = new();

        public List<AdminProgramOptionDto> Locations { get; set; }
            = new();

        public List<AdminProgramOptionDto> Branches { get; set; }
            = new();

        public List<AdminProgramOptionDto> Sexes { get; set; }
            = new();
    }
}
