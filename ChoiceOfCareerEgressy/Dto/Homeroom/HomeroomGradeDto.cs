namespace ChoiceOfCareerEgressy.Dto.Homeroom
{
    public class HomeroomGradeDto
    {
        public int GradeId { get; set; }
        public string GradeName { get; set; } = "";

        public List<HomeroomStudentDto> Students { get; set; } = new();
    }
}

