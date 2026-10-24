using ChoiceOfCareerEgressy.Model.Admin.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChoiceOfCareerEgressy.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class StudentsController : ControllerBase
    {
        private readonly StudentImportService _studentImportService;

        public StudentsController(
            StudentImportService studentImportService)
        {
            _studentImportService = studentImportService;
        }

        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(
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
                var result = await _studentImportService.ImportAsync(file);

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
