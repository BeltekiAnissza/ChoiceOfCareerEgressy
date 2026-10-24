namespace ChoiceOfCareerEgressy.Dto.UserDto.Admin
{
    public class AdminUserOptionsDto
    {

        public List<AdminUserOptionDto> Grades { get; set; }
            = new();

        public List<AdminUserOptionDto> Branches { get; set; }
            = new();

        public List<AdminUserOptionDto> Sexes { get; set; }
            = new();
    }
}
