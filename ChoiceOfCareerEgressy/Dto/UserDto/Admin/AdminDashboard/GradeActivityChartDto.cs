using static ChoiceOfCareerEgressy.Pages.Admin.IndexModel;

namespace ChoiceOfCareerEgressy.Dto.UserDto.Admin.AdminDashboard
{
    public class GradeActivityChartDto
    {
        public List<GradeStatisticDto> Grades { get; set; } = new();
    }
    public class GradeStatisticDto
    {
        public string GradeName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int ActiveStudentCount { get; set; }
        public double ActivityPercent { get; set; }
    }
}
