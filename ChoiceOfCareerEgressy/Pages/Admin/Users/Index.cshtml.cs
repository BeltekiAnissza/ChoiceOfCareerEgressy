using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Dto.UserDto.Admin;
using ChoiceOfCareerEgressy.Model;
using ChoiceOfCareerEgressy.Model.Admin;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserService _userService;
        private readonly AdminAllDataDeleteService _deleteService;

        public IndexModel(UserService userService, AdminAllDataDeleteService deleteService)
        {
            _userService = userService;
            _deleteService = deleteService;
        }

        // =========================================
        // FELHASZNÁLÓK
        // =========================================

        public List<UserDto> Users { get; set; } = new();

        public AdminUserOptionsDto Options { get; set; } = new();

        // =========================================
        // KERESÉSI FELTÉTELEK
        // =========================================

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? GradeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Sort { get; set; }

        [BindProperty(SupportsGet = true)]
        public RoleChoice? Role { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BranchId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool HomeroomOnly { get; set; }
        // =========================================
        // ÖSSZESÍTÉSEK
        // =========================================

        public int StudentCount { get; set; }

        public int TeacherCount { get; set; }

        // =========================================
        // VISSZAJELZÉSEK
        // =========================================

        public string? SuccessMessage { get; set; }

        public string? ErrorMessage { get; set; }

        // =========================================
        // OLDAL BETÖLTÉSE
        // =========================================

        public async Task OnGetAsync()
        {
            SuccessMessage =
                TempData["SuccessMessage"] as string;

            ErrorMessage =
                TempData["ErrorMessage"] as string;

            // Szűréshez szükséges adatok

            Options = await _userService.GetUserOptionsAsync();

            // Felhasználók lekérdezése

            Users = await _userService.GetAllUsersAsync(
                search: Search,
                gradeId: GradeId,
                sort: Sort,
                role: Role,
                branchId: BranchId,
                homeroomOnly: HomeroomOnly);

            // Összes felhasználó száma

            UserCountDto counts =
                await _userService.GetUserCountsAsync();

            StudentCount = counts.StudentCount;

            TeacherCount = counts.TeacherCount;
        }

        // =========================================
        // ÖSSZES DIÁK TÖRLÉSE
        // =========================================

        public async Task<IActionResult> OnPostDeleteAllStudentsAsync()
        {
            int? adminId = GetAdminUserId();

            if (!adminId.HasValue)
            {
                TempData["ErrorMessage"] =
                    "Nem sikerült azonosítani az adminisztrátort.";

                return RedirectToPage();
            }

            try
            {
                int deletedCount =
                    await _deleteService.DeleteAllStudentsAsync(
                        adminId.Value);

                TempData["SuccessMessage"] =
                    $"Sikeresen töröltünk {deletedCount} diákot.";
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "Adatbázishiba történt a diákok törlése során.";
            }

            return RedirectToPage();
        }

        // =========================================
        // ÖSSZES TANÁR TÖRLÉSE
        // =========================================

        public async Task<IActionResult> OnPostDeleteAllTeachersAsync()
        {
            int? adminId = GetAdminUserId();

            if (!adminId.HasValue)
            {
                TempData["ErrorMessage"] =
                    "Nem sikerült azonosítani az adminisztrátort.";

                return RedirectToPage();
            }

            try
            {
                int deletedCount =
                    await _deleteService.DeleteAllTeachersAsync(
                        adminId.Value);

                TempData["SuccessMessage"] =
                    $"Sikeresen töröltünk {deletedCount} tanárt.";
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "Adatbázishiba történt a tanárok törlése során.";
            }

            return RedirectToPage();
        }

        // =========================================
        // ADMIN AZONOSÍTÓ LEKÉRÉSE
        // =========================================

        private int? GetAdminUserId()
        {
            string? value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id
                : null;
        }
    }
}