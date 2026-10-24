namespace ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms
{
    public class AdminSupervisionOptionDto
    {
        public int PlanSessionId { get; set; }
        public string PlanName { get; set; } = "";
        public string LocationName { get; set; } = "";

        public int RequiredSupervisors { get; set; }
        public int RegisteredSupervisors { get; set; }

        public int AvailablePlaces =>
            Math.Max(0, RequiredSupervisors - RegisteredSupervisors);
    }
}
