using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Dto.UserDto.Student;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Mvc;

namespace ChoiceOfCareerEgressy.Controller
{

    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();

            return Ok(users);
        }

        // GET: api/users/grade/3
        [HttpGet("grade/{gradeId:int}")]
        public async Task<ActionResult<List<UserDto>>> GetStudentsByGrade(int gradeId)
        {
            var students = await _userService.GetStudentsByGradeAsync(gradeId);

            return Ok(students);
        }

        // GET: api/users/student/5
        [HttpGet("student/{userId:int}")]
        public async Task<ActionResult<StudentDetailsDto>> GetStudentById(int userId)
        {
            var student = await _userService.GetStudentByIdAsync(userId);

            if (student == null)
                return NotFound("A diák nem található.");

            return Ok(student);
        }
    }
}

