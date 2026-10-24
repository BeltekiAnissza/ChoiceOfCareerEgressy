using ChoiceOfCareerEgressy.Dto.GradeDto;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Teacher")]
    public class GradesController : ControllerBase
    {
        private readonly GradeManagementService _service;
        public GradesController(GradeManagementService service) => _service = service;
        private bool TryGetActorId(out int id) =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);

        [HttpGet]
        public async Task<IActionResult> GetGrades()
        {
            if (!TryGetActorId(out var id)) return Unauthorized();
            var grades = await _service.GetGradesAsync();
            if (User.IsInRole("Admin")) return Ok(grades);
            var ownId = await _service.GetHomeroomGradeIdAsync(id);
            if (ownId == null) return Forbid();
            return Ok(grades.Where(g => g.GradeId == ownId.Value));
        }

        [HttpPut("rename")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename([FromBody] GradeRenameDto dto)
        {
            if (!TryGetActorId(out var id)) return Unauthorized();
            try { await _service.RenameAsync(id, dto); return Ok(new { Message = "Osztály átnevezve." }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (DbUpdateException) { return Conflict(new { Message = "Az átnevezés nem sikerült." }); }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] GradeCreateDto dto)
        {
            if (!TryGetActorId(out var id)) return Unauthorized();
            try { var gradeId = await _service.CreateAsync(id, dto); return Ok(new { GradeId = gradeId, Message = "Osztály létrehozva." }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (DbUpdateException) { return Conflict(new { Message = "A létrehozás nem sikerült." }); }
        }

        [HttpDelete("{gradeId:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int gradeId)
        {
            if (!TryGetActorId(out var id)) return Unauthorized();
            try { await _service.DeleteAsync(id, gradeId); return Ok(new { Message = "Osztály törölve." }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (DbUpdateException) { return Conflict(new { Message = "A törlés nem sikerült. Ellenőrizd a kapcsolódó adatokat." }); }
        }
    }
}
