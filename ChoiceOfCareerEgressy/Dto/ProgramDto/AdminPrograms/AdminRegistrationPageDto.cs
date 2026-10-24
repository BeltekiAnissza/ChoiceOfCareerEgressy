using ChoiceOfCareerEgressy.Dto.Homeroom;

namespace ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms
{
    public class AdminRegistrationPageDto
    {
        public int EventId { get; set; }

        public List<HomeroomEventDto> Events { get; set; } = new();

        public List<AdminGradeSummaryDto> Grades { get; set; } = new();

        public List<AdminProgramSummaryDto> Programs { get; set; } = new();

        public HomeroomGradeDto? SelectedGrade { get; set; }

        public AdminProgramDetailDto? SelectedProgram { get; set; }
    }
}
