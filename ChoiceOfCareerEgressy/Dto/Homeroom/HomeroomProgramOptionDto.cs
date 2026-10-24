namespace ChoiceOfCareerEgressy.Dto.Homeroom
{
    public class HomeroomProgramOptionDto
    {
        public int PlanSessionId { get; set; }
        public string PlanName { get; set; } = "";
        public string LocationName { get; set; } = "";
        public string TimeSlotName { get; set; } = "";

        public int AvailablePlaces { get; set; }
    }
}
