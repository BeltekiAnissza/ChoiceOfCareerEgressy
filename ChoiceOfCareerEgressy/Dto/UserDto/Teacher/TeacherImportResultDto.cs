namespace ChoiceOfCareerEgressy.Dto.UserDto.Teacher
{
    public class TeacherImportResultDto
    {
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }

        public List<TeacherImportErrorDto> Errors { get; set; } = new();
    }
}
