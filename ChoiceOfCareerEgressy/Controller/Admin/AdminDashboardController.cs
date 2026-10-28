using ChoiceOfCareerEgressy.Dto.UserDto.Admin.AdminDashboard;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChoiceOfCareerEgressy.Controller.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly AdminDashboardService _dashboardService;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(AdminDashboardService dashboardService, ILogger<AdminDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        // =========================================
        // 1. ESEMÉNYEK LEKÉRDEZÉSE
        // GET: api/admin/dashboard/events
        // =========================================

        [HttpGet("events")]
        public async Task<ActionResult<List<DashboardEventDto>>> GetEvents()
        {
            var events = await _dashboardService
                .GetAvailableEventsAsync();

            return Ok(events);
        }

        // =========================================
        // 2. STATISZTIKAI KÁRTYÁK
        // GET: api/admin/dashboard/statistics
        // GET: api/admin/dashboard/statistics?eventId=1
        // =========================================

        [HttpGet("statistics")]
        public async Task<ActionResult<DashboardStatisticsDto>> GetStatistics([FromQuery] int? eventId)
        {
            var statistics = await _dashboardService
                .GetStatisticsAsync(eventId);

            return Ok(statistics);
        }

        // =========================================
        // 3. ÁGAZATI DIAGRAM
        // GET: api/admin/dashboard/branches
        // GET: api/admin/dashboard/branches?eventId=1
        // =========================================

        [HttpGet("branches")]
        public async Task<ActionResult<BranchChartDto>> GetBranchChart([FromQuery] int? eventId)
        {
            var chart = await _dashboardService
                .GetBranchChartAsync(eventId);

            return Ok(chart);
        }

        // =========================================
        // 4. KIHASZNÁLTSÁGI DIAGRAM
        // GET: api/admin/dashboard/capacity
        // GET: api/admin/dashboard/capacity?eventId=1
        // =========================================

        [HttpGet("capacity")]
        public async Task<ActionResult<CapacityChartDto>> GetCapacityChart([FromQuery] int? eventId)
        {
            var chart = await _dashboardService
                .GetCapacityChartAsync(eventId);

            return Ok(chart);
        }

        // =========================================
        // 5. JELENTKEZÉSEK IDŐBELI ALAKULÁSA
        // GET: api/admin/dashboard/application-trend
        // GET: api/admin/dashboard/application-trend?eventId=1
        // =========================================

        [HttpGet("application-trend")]
        public async Task<ActionResult<ApplicationTrendChartDto>> GetApplicationTrend([FromQuery] int? eventId)
        {
            var chart = await _dashboardService
                .GetApplicationTrendAsync(eventId);

            return Ok(chart);
        }

        // =========================================
        // 6. LEGNÉPSZERŰBB PROGRAMOK
        // GET: api/admin/dashboard/popular-programs
        // GET: api/admin/dashboard/popular-programs?eventId=1
        // =========================================

        [HttpGet("popular-programs")]
        public async Task<ActionResult<PopularProgramsChartDto>> GetPopularPrograms([FromQuery] int? eventId)
        {
            var chart = await _dashboardService
                .GetPopularProgramsAsync(eventId);

            return Ok(chart);
        }

        // =========================================
        // 7. OSZTÁLYOK AKTIVITÁSA
        // GET: api/admin/dashboard/grade-activity
        // GET: api/admin/dashboard/grade-activity?eventId=1
        // =========================================

        [HttpGet("grade-activity")]
        public async Task<ActionResult<GradeActivityChartDto>> GetGradeActivity([FromQuery] int? eventId)
        {
            var chart = await _dashboardService
                .GetGradeActivityAsync(eventId);

            return Ok(chart);
        }
    }
}

