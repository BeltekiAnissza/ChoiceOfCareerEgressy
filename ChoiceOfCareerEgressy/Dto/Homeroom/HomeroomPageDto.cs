namespace ChoiceOfCareerEgressy.Dto.Homeroom
{
    public class HomeroomPageDto
    {
        public int EventId { get; set; }
        public List<HomeroomEventDto> Events { get; set; } = new();
        public List<HomeroomGradeDto> Grades { get; set; } = new();
    }
}

