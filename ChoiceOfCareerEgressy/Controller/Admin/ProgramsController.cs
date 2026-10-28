using ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms;
using ChoiceOfCareerEgressy.Dto.ProgramDto.ImportPrograms;
using ChoiceOfCareerEgressy.Model.Admin.Imports;
using ChoiceOfCareerEgressy.Model.Admin.Programs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller.Admin
{
    [Route("api/programs")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ProgramsController : ControllerBase
    {
        private readonly AdminProgramService _programService;
        private readonly ProgramImportService _programImportService;

        public ProgramsController(AdminProgramService programService, ProgramImportService programImportService)
        {
            _programService = programService;
            _programImportService = programImportService;
        }

        // =========================================
        // BEJELENTKEZETT ADMIN AZONOSÍTÓJA
        // =========================================

        private int? GetAdminId()
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int adminId)
                ? adminId
                : null;
        }

        // =========================================
        // PROGRAMOK LISTÁZÁSA
        // GET: api/programs
        // GET: api/programs?eventId=1
        // GET: api/programs?search=informatika
        // =========================================

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? eventId = null, [FromQuery] string? search = null, [FromQuery] int? timeSlotId = null)
        {
            var programs = await _programService.GetProgramsAsync(
                eventId,
                search, timeSlotId);

            return Ok(programs);
        }

        // =========================================
        // EGY PROGRAM LEKÉRDEZÉSE
        // GET: api/programs/5
        // =========================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var program = await _programService.GetProgramAsync(id);

            if (program == null)
            {
                return NotFound(new
                {
                    Message = "A program nem található."
                });
            }

            return Ok(program);
        }

        // =========================================
        // LEGÖRDÜLŐ LISTÁK ADATAI
        // GET: api/programs/options
        // =========================================

        [HttpGet("options")]
        public async Task<IActionResult> GetOptions()
        {
            var options = await _programService.GetOptionsAsync();

            return Ok(options);
        }

        // =========================================
        // ÚJ PROGRAM LÉTREHOZÁSA
        // POST: api/programs
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] AdminProgramDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                int programId = await _programService.CreateProgramAsync(
                    dto,
                    GetAdminId());

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = programId },
                    new
                    {
                        Success = true,
                        Message = "A program sikeresen létrehozva.",
                        PlanSessionId = programId
                    });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
        }

        // =========================================
        // PROGRAM SZERKESZTÉSE
        // PUT: api/programs/5
        // =========================================

        [HttpPut("{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, [FromBody] AdminProgramDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (dto.PlanSessionId != 0 &&
                dto.PlanSessionId != id)
            {
                return BadRequest(new
                {
                    Message =
                        "Az URL-ben és a kérésben szereplő programazonosító eltér."
                });
            }

            dto.PlanSessionId = id;

            try
            {
                await _programService.UpdateProgramAsync(
                    dto,
                    GetAdminId());

                return Ok(new
                {
                    Success = true,
                    Message = "A program sikeresen módosítva.",
                    PlanSessionId = id
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    Message = ex.Message
                });
            }
        }

        // =========================================
        // PROGRAM TÖRLÉSI INFORMÁCIÓI
        // GET: api/programs/5/delete-info
        // =========================================

        [HttpGet("{id:int}/delete-info")]
        public async Task<IActionResult> GetDeleteInfo(int id)
        {
            var info = await _programService.GetDeleteInfoAsync(id);

            if (info == null)
            {
                return NotFound(new
                {
                    Message = "A program nem található."
                });
            }

            return Ok(info);
        }

        // =========================================
        // PROGRAM TÖRLÉSE
        // DELETE: api/programs/5
        // =========================================

        [HttpDelete("{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _programService.DeleteProgramAsync(
                    id,
                    GetAdminId());

                return Ok(new
                {
                    Success = true,
                    Message = "A program sikeresen törölve.",
                    PlanSessionId = id
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    Message = ex.Message
                });
            }
        }

        // =========================================
        // ESEMÉNY ÖSSZES PROGRAMJÁNAK TÖRLÉSE
        // DELETE: api/programs/event/1
        // =========================================

        [HttpDelete("event/{eventId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllByEvent(int eventId)
        {
            try
            {
                int deletedCount =
                    await _programService.DeleteAllProgramsAsync(
                        eventId,
                        GetAdminId());

                return Ok(new
                {
                    Success = true,
                    Message =
                        "Az esemény összes programja sikeresen törölve.",
                    DeletedCount = deletedCount
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    Message = ex.Message
                });
            }
        }

        // =========================================
        // PROGRAMOK IMPORTÁLÁSA EXCELBŐL
        // POST: api/programs/import/1
        //
        // Megtartjuk a korábbi API-útvonalat.
        // =========================================

        [HttpPost("import/{eventId:int}")]
        [Consumes("multipart/form-data")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<ProgramImportResultDto>> Import(int eventId, [FromForm] IFormFile file)
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
                    file,
                    eventId);

                return Ok(result);
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            catch (FormatException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    Message = ex.Message
                });
            }
        }
    }
}