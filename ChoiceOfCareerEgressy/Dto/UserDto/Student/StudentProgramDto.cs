namespace ChoiceOfCareerEgressy.Dto.UserDto.Student
{
    public class StudentProgramDto
    {
        public int PlanSessionId { get; set; }
        public int PlanId { get; set; }
        public int TimeSlotId { get; set; }

        public string PlanName { get; set; } = "";
        public string Description { get; set; } = "";
        public string LocationName { get; set; } = "";

        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public int Capacity { get; set; }
        public int AvailablePlaces { get; set; }
        public bool IsSelected { get; set; }
    }
}
