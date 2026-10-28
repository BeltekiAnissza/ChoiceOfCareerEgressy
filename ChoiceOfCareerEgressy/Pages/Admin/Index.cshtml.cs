
using ChoiceOfCareerEgressy.Dto.UserDto.Admin.AdminDashboard;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChoiceOfCareerEgressy.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AdminDashboardService _dashboardService;

        public IndexModel(AdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // =========================================
        // ESEMÉNYSZŰRÉS
        // =========================================

        [BindProperty(SupportsGet = true, Name = "eventId")]
        public int? SelectedEventId { get; set; }

        public List<DashboardEventDto> AvailableEvents { get; set; }
            = new();

        // =========================================
        // STATISZTIKAI KÁRTYÁK
        // =========================================

        public int StudentCount { get; set; }

        public int TeacherCount { get; set; }

        public int ProgramCount { get; set; }

        public int ApplicationCount { get; set; }

        public int EventCount { get; set; }

        public int SupervisionCount { get; set; }

        // =========================================
        // ÁGAZATI DIAGRAM
        // =========================================

        public List<string> BranchChartLabels { get; set; }
            = new();

        public List<int> BranchChartValues { get; set; }
            = new();

        // =========================================
        // KIHASZNÁLTSÁGI DIAGRAM
        // =========================================

        public List<int> CapacityChartValues { get; set; }
            = new();

        // =========================================
        // JELENTKEZÉSI DIAGRAM
        // =========================================

        public List<string> TrendChartLabels { get; set; }
            = new();

        public List<int> TrendChartValues { get; set; }
            = new();

        // =========================================
        // NÉPSZERŰ PROGRAMOK
        // =========================================

        public List<PopularProgramDto> PopularPrograms { get; set; }
            = new();

        // =========================================
        // OSZTÁLYOK AKTIVITÁSA
        // =========================================

        public List<GradeStatisticDto> GradeStatistics { get; set; }
            = new();

        // =========================================
        // OLDAL BETÖLTÉSE
        // =========================================

        public async Task OnGetAsync()
        {
            await LoadEventsAsync();

            await LoadStatisticsAsync();

            await LoadBranchChartAsync();

            await LoadCapacityChartAsync();

            await LoadApplicationTrendAsync();

            await LoadPopularProgramsAsync();

            await LoadGradeActivityAsync();
        }

        // =========================================
        // 1. ESEMÉNYEK BETÖLTÉSE
        // =========================================

        private async Task LoadEventsAsync()
        {
            AvailableEvents =
                await _dashboardService.GetAvailableEventsAsync();

            // Nem létező esemény kiválasztásának kezelése

            if (SelectedEventId.HasValue &&
                !AvailableEvents.Any(e =>
                    e.EventId == SelectedEventId.Value))
            {
                SelectedEventId = null;
            }
        }

        // =========================================
        // 2. STATISZTIKAI KÁRTYÁK BETÖLTÉSE
        // =========================================

        private async Task LoadStatisticsAsync()
        {
            DashboardStatisticsDto statistics =
                await _dashboardService.GetStatisticsAsync(
                    SelectedEventId);

            StudentCount = statistics.StudentCount;

            TeacherCount = statistics.TeacherCount;

            ProgramCount = statistics.ProgramCount;

            ApplicationCount = statistics.ApplicationCount;

            EventCount = statistics.EventCount;

            SupervisionCount = statistics.SupervisionCount;
        }

        // =========================================
        // 3. ÁGAZATI DIAGRAM BETÖLTÉSE
        // =========================================

        private async Task LoadBranchChartAsync()
        {
            BranchChartDto chart =
                await _dashboardService.GetBranchChartAsync(
                    SelectedEventId);

            BranchChartLabels = chart.Labels;

            BranchChartValues = chart.Values;
        }

        // =========================================
        // 4. KIHASZNÁLTSÁGI DIAGRAM BETÖLTÉSE
        // =========================================

        private async Task LoadCapacityChartAsync()
        {
            CapacityChartDto chart =
                await _dashboardService.GetCapacityChartAsync(
                    SelectedEventId);

            CapacityChartValues = new List<int>
            {
                chart.Full,
                chart.Partial,
                chart.Empty
            };
        }

        // =========================================
        // 5. JELENTKEZÉSEK IDŐBELI ALAKULÁSA
        // =========================================

        private async Task LoadApplicationTrendAsync()
        {
            ApplicationTrendChartDto chart =
                await _dashboardService.GetApplicationTrendAsync(
                    SelectedEventId);

            TrendChartLabels = chart.Labels;

            TrendChartValues = chart.Values;
        }

        // =========================================
        // 6. LEGNÉPSZERŰBB PROGRAMOK
        // =========================================

        private async Task LoadPopularProgramsAsync()
        {
            PopularProgramsChartDto chart =
                await _dashboardService.GetPopularProgramsAsync(
                    SelectedEventId);

            PopularPrograms = chart.Programs;
        }

        // =========================================
        // 7. OSZTÁLYOK AKTIVITÁSA
        // =========================================

        private async Task LoadGradeActivityAsync()
        {
            GradeActivityChartDto chart =
                await _dashboardService.GetGradeActivityAsync(
                    SelectedEventId);

            GradeStatistics = chart.Grades;
        }

        // =========================================
        // KÜLÖN LEKÉRDEZÉSEK DIAGRAMONKÉNT
        // =========================================

        // -----------------------------------------
        // STATISZTIKAI KÁRTYÁK
        // GET: /Admin?handler=Statistics
        // -----------------------------------------

        public async Task<IActionResult> OnGetStatisticsAsync()
        {
            var result =
                await _dashboardService.GetStatisticsAsync(
                    SelectedEventId);

            return new JsonResult(result);
        }

        // -----------------------------------------
        // ÁGAZATI DIAGRAM
        // GET: /Admin?handler=BranchChart
        // -----------------------------------------

        public async Task<IActionResult> OnGetBranchChartAsync()
        {
            var result =
                await _dashboardService.GetBranchChartAsync(
                    SelectedEventId);

            return new JsonResult(result);
        }

        // -----------------------------------------
        // KIHASZNÁLTSÁGI DIAGRAM
        // GET: /Admin?handler=CapacityChart
        // -----------------------------------------

        public async Task<IActionResult> OnGetCapacityChartAsync()
        {
            var result =
                await _dashboardService.GetCapacityChartAsync(
                    SelectedEventId);

            return new JsonResult(result);
        }

        // -----------------------------------------
        // JELENTKEZÉSI DIAGRAM
        // GET: /Admin?handler=ApplicationTrend
        // -----------------------------------------

        public async Task<IActionResult> OnGetApplicationTrendAsync()
        {
            var result =
                await _dashboardService.GetApplicationTrendAsync(
                    SelectedEventId);

            return new JsonResult(result);
        }

        // -----------------------------------------
        // LEGNÉPSZERŰBB PROGRAMOK
        // GET: /Admin?handler=PopularPrograms
        // -----------------------------------------

        public async Task<IActionResult> OnGetPopularProgramsAsync()
        {
            var result =
                await _dashboardService.GetPopularProgramsAsync(
                    SelectedEventId);

            return new JsonResult(result);
        }

        // -----------------------------------------
        // OSZTÁLYOK AKTIVITÁSA
        // GET: /Admin?handler=GradeActivity
        // -----------------------------------------

        public async Task<IActionResult> OnGetGradeActivityAsync()
        {
            var result =
                await _dashboardService.GetGradeActivityAsync(
                    SelectedEventId);

            return new JsonResult(result);
        }
    }
}