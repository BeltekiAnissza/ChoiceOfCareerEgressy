using ChoiceOfCareerEgressy.Dto.UserDto.Teacher;
using ChoiceOfCareerEgressy.Model.Admin.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoiceOfCareerEgressy.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class TeachersController : ControllerBase
    {
        private readonly TeacherImportService _teacherImportService;

        public TeachersController(
            TeacherImportService teacherImportService)
        {
            _teacherImportService = teacherImportService;
        }

        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<TeacherImportResultDto>> Import(
            [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    Message = "Válassz ki egy Excel-fájlt!"
                });
            }

            try
            {
                var result = await _teacherImportService.ImportAsync(file);

                return Ok(result);
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
        }
    }
}
