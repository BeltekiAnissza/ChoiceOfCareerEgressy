using ChoiceOfCareerEgressy.Dto.Homeroom;

namespace ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms
{
    public class AdminTeacherSupervisionPageDto
    {
        public int EventId { get; set; }

        public List<HomeroomEventDto> Events { get; set; } = new();

        public List<AdminTeacherDto> Teachers { get; set; } = new();

        public List<AdminTeacherProgramSummaryDto> Programs { get; set; } = new();

        public AdminTeacherProgramDetailDto? SelectedProgram { get; set; }
    }
}

