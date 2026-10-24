namespace ChoiceOfCareerEgressy.Dto.UserDto
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string EmailAddress { get; set; } = "";
        public string Role { get; set; } = "";
        public string? GradeName { get; set; }
        public string? BranchName { get; set; }
        public string? SexName { get; set; }
    }
}
