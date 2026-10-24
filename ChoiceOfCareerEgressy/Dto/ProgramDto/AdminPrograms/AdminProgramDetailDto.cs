namespace ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms
{
    public class AdminProgramDetailDto
    {
        public int PlanSessionId { get; set; }
        public string PlanName { get; set; } = "";
        public string TimeSlotName { get; set; } = "";
        public string LocationName { get; set; } = "";

        public int Capacity { get; set; }

        public List<AdminProgramStudentDto> Students { get; set; }
            = new();
        public int TimeSlotId { get; set; }
    }
}
