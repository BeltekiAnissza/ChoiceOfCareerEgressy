using static ChoiceOfCareerEgressy.Pages.Admin.IndexModel;

namespace ChoiceOfCareerEgressy.Dto.UserDto.Admin.AdminDashboard
{
    public class PopularProgramsChartDto
    {
        public List<PopularProgramDto> Programs { get; set; } = new();
    }
    public class PopularProgramDto
    {
        public string Name { get; set; } = string.Empty;
        public int ApplicationCount { get; set; }
        public int Capacity { get; set; }
        public double OccupancyPercent { get; set; }
    }
}
