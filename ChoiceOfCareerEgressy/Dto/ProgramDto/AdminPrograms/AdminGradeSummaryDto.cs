namespace ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms
{
    public class AdminGradeSummaryDto
    {
        public int GradeId { get; set; }
        public string GradeName { get; set; } = "";

        public int StudentCount { get; set; }
        public int StartedCount { get; set; }
        public int CompletedCount { get; set; }
    }
}

