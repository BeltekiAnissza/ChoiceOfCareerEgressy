namespace ChoiceOfCareerEgressy.Dto.UserDto.Student
{
    public class StudentImportResultDto
    {
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }

        public List<StudentImportErrorDto> Errors { get; set; } = new();
    }
}
