using ChoiceOfCareerEgressy.Dto.Homeroom;

namespace ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms
{
    public class AdminProgramStudentDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string GradeName { get; set; } = "";

        public int ApplicationId { get; set; }

        public List<HomeroomProgramOptionDto> AvailablePrograms { get; set; }
            = new();
    }
}
