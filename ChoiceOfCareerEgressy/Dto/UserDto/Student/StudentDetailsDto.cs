namespace ChoiceOfCareerEgressy.Dto.UserDto.Student
{
    public class StudentDetailsDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string EmailAddress { get; set; } = "";

        public int? GradeId { get; set; }
        public string? GradeName { get; set; }

        public string? BranchName { get; set; }
        public string? SexName { get; set; }

        public List<ApplicationDto> Applications { get; set; } = new();
    }
}

