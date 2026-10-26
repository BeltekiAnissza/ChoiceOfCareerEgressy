using ChoiceOfCareerEgressy.Dto.ProgramDto.ImportPrograms;
using ChoiceOfCareerEgressy.Model.Admin.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChoiceOfCareerEgressy.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ProgramsController : ControllerBase
    {
        private readonly ProgramImportService _programImportService;

        public ProgramsController(ProgramImportService programImportService)
        {
            _programImportService = programImportService;
        }

        [HttpPost("import/{eventId:int}")]
        [Consumes("multipart/form-data")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<ProgramImportResultDto>> Import(
            int eventId,
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
                var result = await _programImportService.ImportAsync(
                    file, eventId);

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

