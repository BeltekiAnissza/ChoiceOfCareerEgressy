namespace ChoiceOfCareerEgressy.Dto.UserDto.Admin
{
    public class AdminUserDeleteDto
    {
        public int UserId { get; set; }

        public string UserName { get; set; } = "";
        public string EmailAddress { get; set; } = "";
        public string Role { get; set; } = "";

        public int ApplicationCount { get; set; }
        public int SupervisionCount { get; set; }
    }
}
