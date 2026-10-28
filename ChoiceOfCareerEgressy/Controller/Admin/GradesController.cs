using ChoiceOfCareerEgressy.Dto.GradeDto;
using ChoiceOfCareerEgressy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Teacher")]
    public class GradesController : ControllerBase
    {
        private readonly GradeManagementService _service;
        private readonly ILogger<GradesController> _logger;

        public GradesController(GradeManagementService service, ILogger<GradesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =========================================
        // BEJELENTKEZETT FELHASZNÁLÓ AZONOSÍTÓJA
        // =========================================

        private bool TryGetActorId(out int id)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out id
            );
        }

        // =========================================
        // 1. OSZTÁLYOK LEKÉRDEZÉSE
        // GET: api/grades
        // =========================================

        [HttpGet]
        public async Task<IActionResult> GetGrades()
        {
            if (!TryGetActorId(out int actorId))
            {
                return Challenge();
            }

            try
            {
                var grades = await _service.GetGradesAsync();

                // Az adminisztrátor minden osztályt láthat.
                if (User.IsInRole("Admin"))
                {
                    return Ok(grades);
                }

                // A tanár csak a saját osztályát láthatja.
                var ownGradeId =
                    await _service.GetHomeroomGradeIdAsync(actorId);

                if (ownGradeId == null)
                {
                    return Forbid();
                }

                var ownGrades = grades
                    .Where(g => g.GradeId == ownGradeId.Value)
                    .ToList();

                return Ok(ownGrades);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Hiba történt az osztályok lekérdezésekor."
                );

                return StatusCode(500, new
                {
                    Message = "Az osztályok lekérdezése nem sikerült."
                });
            }
        }

        // =========================================
        // 2. ÚJ OSZTÁLY LÉTREHOZÁSA
        // POST: api/grades
        // =========================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] GradeCreateDto dto)
        {
            if (!TryGetActorId(out int actorId))
            {
                return Challenge();
            }

            try
            {
                int gradeId =
                    await _service.CreateAsync(actorId, dto);

                return Ok(new
                {
                    GradeId = gradeId,
                    Message = "Az osztály sikeresen létrejött."
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
                    Message = ex.Message
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Hiba történt az osztály létrehozásakor."
                );

                return Conflict(new
                {
                    Message = "Az osztály létrehozása nem sikerült."
                });
            }
        }

        // =========================================
        // 3. OSZTÁLY ÁTNEVEZÉSE
        // PUT: api/grades/rename
        // =========================================

        [HttpPut("rename")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename([FromBody] GradeRenameDto dto)
        {
            if (!TryGetActorId(out int actorId))
            {
                return Challenge();
            }

            try
            {
                await _service.RenameAsync(actorId, dto);

                return Ok(new
                {
                    Message = "Az osztály neve sikeresen megváltozott."
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
                    Message = ex.Message
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Hiba történt az osztály átnevezésekor."
                );

                return Conflict(new
                {
                    Message = "Az osztály átnevezése nem sikerült."
                });
            }
        }

        // =========================================
        // 4. ÜRES OSZTÁLY TÖRLÉSE
        // DELETE: api/grades/5
        // =========================================

        [HttpDelete("{gradeId:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int gradeId)
        {
            if (!TryGetActorId(out int actorId))
            {
                return Challenge();
            }

            try
            {
                await _service.DeleteAsync(actorId, gradeId);

                return Ok(new
                {
                    Message = "Az osztály sikeresen törölve."
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
                    Message = ex.Message
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Hiba történt az osztály törlésekor."
                );

                return Conflict(new
                {
                    Message =
                        "Az osztály törlése nem sikerült. " +
                        "Ellenőrizd a kapcsolódó adatokat."
                });
            }
        }

        // =========================================
        // 5. OSZTÁLY BALLAGTATÁSA
        // POST: api/grades/5/graduate
        // =========================================

        [HttpPost("{gradeId:int}/graduate")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Graduate(int gradeId)
        {
            if (!TryGetActorId(out int actorId))
            {
                return Challenge();
            }

            try
            {
                await _service.GraduateAsync(actorId, gradeId);

                _logger.LogInformation(
                    "Osztály ballagtatása sikeres. " +
                    "AdminId: {AdminId}, GradeId: {GradeId}",
                    actorId,
                    gradeId
                );

                return Ok(new
                {
                    Message =
                        "Az osztály ballagtatása sikeres. " +
                        "A diákok az Elballagott osztályba kerültek."
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
                    Message = ex.Message
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Hiba történt az osztály ballagtatásakor. " +
                    "GradeId: {GradeId}",
                    gradeId
                );

                return Conflict(new
                {
                    Message =
                        "Az osztály ballagtatása adatbázishiba " +
                        "miatt nem sikerült."
                });
            }
        }

        // =========================================
        // 6. ELBALLAGOTT OSZTÁLY VÉGLEGES TÖRLÉSE
        // DELETE: api/grades/5/graduated
        // =========================================

        [HttpDelete("{gradeId:int}/graduated")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGraduated(int gradeId, [FromBody] DeleteGraduatedGradeRequest request)
        {
            if (!TryGetActorId(out int actorId))
            {
                return Challenge();
            }

            // =====================================
            // MEGERŐSÍTÉS ELLENŐRZÉSE
            // =====================================

            if (request == null ||
                request.Confirmation != "VÉGLEGES TÖRLÉS")
            {
                return BadRequest(new
                {
                    Message =
                        "A végleges törléshez pontosan ezt " +
                        "kell megadnod: VÉGLEGES TÖRLÉS"
                });
            }

            // =====================================
            // VÉGLEGES TÖRLÉS
            // =====================================

            try
            {
                int deletedStudents =
                    await _service.DeleteGraduatedAsync(
                        actorId,
                        gradeId
                    );

                _logger.LogWarning(
                    "Elballagott osztály véglegesen törölve. " +
                    "AdminId: {AdminId}, GradeId: {GradeId}, " +
                    "DeletedStudents: {DeletedStudents}",
                    actorId,
                    gradeId,
                    deletedStudents
                );

                return Ok(new
                {
                    DeletedStudents = deletedStudents,
                    Message =
                        "Az Elballagott osztály és a hozzá " +
                        "tartozó diákok véglegesen törölve lettek."
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
                    Message = ex.Message
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Hiba történt az Elballagott osztály " +
                    "végleges törlésekor. GradeId: {GradeId}",
                    gradeId
                );

                return Conflict(new
                {
                    Message =
                        "Az Elballagott osztály végleges " +
                        "törlése adatbázishiba miatt nem sikerült."
                });
            }
        }
    }

    // =========================================
    // VÉGLEGES TÖRLÉS MEGERŐSÍTÉSE
    // =========================================

    public class DeleteGraduatedGradeRequest
    {
        public string Confirmation { get; set; } = string.Empty;
    }
}