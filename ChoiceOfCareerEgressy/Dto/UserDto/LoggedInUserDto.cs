namespace ChoiceOfCareerEgressy.Dto.UserDto
{
    public class LoggedInUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string EmailAddress { get; set; } = "";
        public string Role { get; set; } = "";
        public bool MustChangePassword { get; set; }
    }
}
