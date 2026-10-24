namespace ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms
{
    public class AdminProgramSummaryDto
    {
        public int PlanSessionId { get; set; }
        public string PlanName { get; set; } = "";
        public string TimeSlotName { get; set; } = "";
        public string LocationName { get; set; } = "";

        public int Capacity { get; set; }
        public int RegisteredCount { get; set; }
    }
}
