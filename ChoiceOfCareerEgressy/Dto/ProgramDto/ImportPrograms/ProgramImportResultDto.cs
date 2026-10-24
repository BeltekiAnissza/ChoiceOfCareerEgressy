namespace ChoiceOfCareerEgressy.Dto.ProgramDto.ImportPrograms
{
    public class ProgramImportResultDto
    {
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<ProgramImportErrorDto> Errors { get; set; } = new();
    }
}
