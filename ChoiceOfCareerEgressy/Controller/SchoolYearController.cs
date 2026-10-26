using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class SchoolYearController : ControllerBase
    {
        private readonly SchoolYearService _schoolYearService;

        public SchoolYearController(
            SchoolYearService schoolYearService)
        {
            _schoolYearService = schoolYearService;
        }


        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            bool alreadyStarted =
                await _schoolYearService.HasStartedThisYearAsync();

            return Ok(new
            {
                AlreadyStarted = alreadyStarted,
                CanStart = !alreadyStarted
            });
        }


        [HttpPost("start")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartSchoolYear(  [FromForm] string confirmation)
        {
            int? adminId = GetCurrentUserId();

            if (adminId == null)
            {
                return Unauthorized(new
                {
                    Message = "A felhasználó nem azonosítható."
                });
            }

            if (confirmation != "ÚJ TANÉV")
            {
                return BadRequest(new
                {
                    Message =
                        "A tanévindításhoz pontosan ezt kell megadnod: ÚJ TANÉV"
                });
            }

            try
            {
                var result = await _schoolYearService.StartAsync(
                    adminId.Value);

                return Ok(new
                {
                    Success = true,
                    Message = "Az új tanév sikeresen elindult.",
                    AdvancedGrades = result.Advanced,
                    GraduatedGrades = result.Graduated,
                    GraduatedStudents = result.Students,
                    CreatedGrades = new[]
                    {
                        "9.I",
                        "9.S",
                        "9.E",
                        "9.KNy"
                    }
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message =
                        "Adatbázishiba történt a tanévindítás során."
                });
            }
        }

        private int? GetCurrentUserId()
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int userId)
                ? userId
                : null;
        }
    }
}

