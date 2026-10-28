namespace ChoiceOfCareerEgressy.Dto.UserDto.Admin.AdminDashboard
{
    public class ApplicationTrendChartDto
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Values { get; set; } = new();
    }
}
