namespace ChoiceOfCareerEgressy.Dto.Homeroom
{
    public class HomeroomApplicationDto
    {
        public int ApplicationId { get; set; }
        public int PlanSessionId { get; set; }

        public string PlanName { get; set; } = "";
        public string LocationName { get; set; } = "";
        public string TimeSlotName { get; set; } = "";

        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }
}


