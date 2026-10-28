namespace ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms
{
    public class AdminTeacherProgramSummaryDto
    {
        public int PlanSessionId { get; set; }

        public string PlanName { get; set; } = "";

        public string TimeSlotName { get; set; } = "";

        public string LocationName { get; set; } = "";

        public int RequiredSupervisors { get; set; }

        public int RegisteredSupervisors { get; set; }
    }
}
