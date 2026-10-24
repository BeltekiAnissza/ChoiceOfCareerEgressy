namespace ChoiceOfCareerEgressy.Dto.ProgramDto.ImportPrograms
{
    public class ProgramImportDto
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string? BranchName { get; set; }
        public string SexName { get; set; } = "";
        public string LocationName { get; set; } = "";
        public int Capacity { get; set; }
        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public int RequiredSupervisors { get; set; }
    }
}
