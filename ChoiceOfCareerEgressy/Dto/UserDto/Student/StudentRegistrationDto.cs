namespace ChoiceOfCareerEgressy.Dto.UserDto.Student
{
    public class StudentRegistrationDto
    {
        public int EventId { get; set; }

        public List<int> SelectedPlanSessionIds { get; set; } = new();
    }
}
