namespace ChoiceOfCareerEgressy.Dto.UserDto
{
    public class ApplicationDto
    {
        public int ApplicationId { get; set; }

        public int PlanSessionId { get; set; }
        public int TimeSlotId { get; set; }

        public string TimeSlotName { get; set; } = "";

        public int PlanId { get; set; }
        public string PlanName { get; set; } = "";

        public string LocationName { get; set; } = "";

        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public DateTimeOffset ApplicationDate { get; set; }
        public DateTimeOffset? UpdateDate { get; set; }
    }
}
