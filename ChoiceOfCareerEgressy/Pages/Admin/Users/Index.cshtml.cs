using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Dto.UserDto.Admin;
using ChoiceOfCareerEgressy.Model;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChoiceOfCareerEgressy.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserService _userService;

        public IndexModel(UserService userService)
        {
            _userService = userService;
        }

        public List<UserDto> Users { get; set; } = new();

        public AdminUserOptionsDto Options { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? GradeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Sort { get; set; }

        [BindProperty(SupportsGet = true)]
        public RoleChoice? Role { get; set; }

        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            SuccessMessage =
                TempData["SuccessMessage"] as string;

            Options = await _userService.GetUserOptionsAsync();

            Users = await _userService.GetAllUsersAsync(
                Search,
                GradeId,
                Sort,
                Role);
        }
    }
}

