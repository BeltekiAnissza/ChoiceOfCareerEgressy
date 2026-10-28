namespace ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms
{
    public class AdminTeacherProgramDetailDto
    {
        public int PlanSessionId { get; set; }

        public int TimeSlotId { get; set; }

        public string PlanName { get; set; } = "";

        public string TimeSlotName { get; set; } = "";

        public string LocationName { get; set; } = "";

        public int RequiredSupervisors { get; set; }

        public List<AdminTeacherProgramSupervisorDto> Supervisors { get; set; } = new();
    }
}
