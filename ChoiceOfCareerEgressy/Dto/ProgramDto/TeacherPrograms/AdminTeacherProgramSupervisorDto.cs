namespace ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms
{
    public class AdminTeacherProgramSupervisorDto
    {
        public int TeacherId { get; set; }

        public string TeacherName { get; set; } = "";

        public string EmailAddress { get; set; } = "";

        public int SupervisionId { get; set; }

        public List<AdminSupervisionOptionDto> AvailablePrograms { get; set; } = new();
    }
}
