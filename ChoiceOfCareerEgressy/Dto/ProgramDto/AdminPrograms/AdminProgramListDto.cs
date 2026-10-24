namespace ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms
{
    public class AdminProgramListDto
    {
        public int PlanSessionId { get; set; }
        public int PlanId { get; set; }

        public string PlanName { get; set; } = "";
        public string PlanDescription { get; set; } = "";

        public string EventName { get; set; } = "";
        public int EventId { get; set; }

        public string TimeSlotName { get; set; } = "";
        public int TimeSlotId { get; set; }

        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public string LocationName { get; set; } = "";

        public string BranchName { get; set; } = "";
        public string SexName { get; set; } = "";

        public int Capacity { get; set; }
        public int RequiredSupervisors { get; set; }

        public int ApplicationCount { get; set; }
        public int SupervisionCount { get; set; }

        public int AvailablePlaces =>
            Math.Max(0, Capacity - ApplicationCount);
    }
}
