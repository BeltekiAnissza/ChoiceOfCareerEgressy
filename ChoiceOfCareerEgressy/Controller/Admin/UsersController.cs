using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Dto.UserDto.Admin;
using ChoiceOfCareerEgressy.Dto.UserDto.Student;
using ChoiceOfCareerEgressy.Model;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller.Admin
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // =========================================
        // BEJELENTKEZETT ADMIN AZONOSÍTÓJA
        // =========================================

        private bool TryGetAdminId(out int adminId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out adminId);
        }

        // =========================================
        // FELHASZNÁLÓK LISTÁZÁSA
        //
        // GET: api/users
        //
        // Opcionális paraméterek:
        // - search
        // - gradeId
        // - sort
        // - role
        //
        // Példák:
        // api/users?search=Kovács
        // api/users?gradeId=3
        // api/users?sort=name_desc
        // =========================================

        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers([FromQuery] string? search = null, [FromQuery] int? gradeId = null, [FromQuery] string? sort = null, [FromQuery] RoleChoice? role = null, [FromQuery] int? branchId = null , [FromQuery] bool homeroomOnly = false)
        {
            if (gradeId.HasValue && gradeId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen osztályazonosító."
                });
            }

            if (role.HasValue &&
                !Enum.IsDefined(typeof(RoleChoice), role.Value))
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen szerepkör."
                });
            }

            // A UserService szűrést és rendezést
            // támogató túlterhelését használjuk.
            var users = await _userService.GetAllUsersAsync(
                    search: search,
                    gradeId: gradeId,
                    sort: sort,
                    role: role,
                    branchId: branchId,
                    homeroomOnly: homeroomOnly);

            return Ok(users);
        }

        // =========================================
        // EGY OSZTÁLY DIÁKJAINAK LEKÉRDEZÉSE
        //
        // GET: api/users/grade/3
        // =========================================

        [HttpGet("grade/{gradeId:int}")]
        public async Task<ActionResult<List<UserDto>>> GetStudentsByGrade(int gradeId)
        {
            if (gradeId <= 0)
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen osztályazonosító."
                });
            }

            var students =
                await _userService.GetStudentsByGradeAsync(gradeId);

            return Ok(students);
        }

        // =========================================
        // EGY DIÁK RÉSZLETES ADATAI
        //
        // GET: api/users/student/5
        //
        // Tartalmazza a diák aktív
        // programjelentkezéseit is.
        // =========================================

        [HttpGet("student/{userId:int}")]
        public async Task<ActionResult<StudentDetailsDto>> GetStudentById(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen felhasználóazonosító."
                });
            }

            var student =
                await _userService.GetStudentByIdAsync(userId);

            if (student == null)
            {
                // Megtartjuk a korábbi válaszformátumot.
                return NotFound("A diák nem található.");
            }

            return Ok(student);
        }

        // =========================================
        // FELHASZNÁLÓ ADATAI SZERKESZTÉSHEZ
        //
        // GET: api/users/5/edit
        // =========================================

        [HttpGet("{userId:int}/edit")]
        public async Task<ActionResult<AdminUserEditDto>> GetUserForEdit(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen felhasználóazonosító."
                });
            }

            var user =
                await _userService.GetUserForEditAsync(userId);

            if (user == null)
            {
                return NotFound(new
                {
                    Message = "A felhasználó nem található."
                });
            }

            return Ok(user);
        }

        // =========================================
        // FELHASZNÁLÓKEZELŐ ŰRLAPOK ADATAI
        //
        // GET: api/users/options
        //
        // Visszaadja:
        // - Osztályok
        // - Ágazatok
        // - Nemek
        // =========================================

        [HttpGet("options")]
        public async Task<ActionResult<AdminUserOptionsDto>> GetOptions()
        {
            var options =
                await _userService.GetUserOptionsAsync();

            return Ok(options);
        }

        // =========================================
        // ÚJ FELHASZNÁLÓ LÉTREHOZÁSA
        //
        // POST: api/users
        //
        // A Service kezeli:
        // - Kötelező adatok ellenőrzését
        // - E-mail-cím egyediségét
        // - Szerepkör ellenőrzését
        // - Kapcsolódó adatok ellenőrzését
        // - Jelszó hashelését
        // - Auditnaplózást
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] AdminUserCreateDto dto)
        {
            if (!TryGetAdminId(out int adminId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                int userId =
                    await _userService.CreateUserAsync(
                        dto,
                        adminId);

                return StatusCode(201, new
                {
                    Success = true,
                    Message = "A felhasználó sikeresen létrehozva.",
                    UserId = userId
                });
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
                    "Adatbázishiba felhasználó létrehozásakor.");

                return Conflict(new
                {
                    Message =
                        "A felhasználó létrehozása nem sikerült."
                });
            }
        }

        // =========================================
        // FELHASZNÁLÓ SZERKESZTÉSE
        //
        // PUT: api/users/5
        //
        // A Service kezeli:
        // - Név módosítását
        // - E-mail-cím módosítását
        // - Osztály módosítását
        // - Ágazat módosítását
        // - Nem módosítását
        // - Jelszó módosítását
        // - Auditnaplózást
        // =========================================

        [HttpPut("{userId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int userId, [FromBody] AdminUserEditDto dto)
        {
            if (!TryGetAdminId(out int adminId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (userId <= 0)
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen felhasználóazonosító."
                });
            }

            if (dto.UserId != 0 && dto.UserId != userId)
            {
                return BadRequest(new
                {
                    Message =
                        "Az URL-ben és a kérésben szereplő " +
                        "felhasználóazonosító eltér."
                });
            }

            dto.UserId = userId;

            try
            {
                await _userService.UpdateUserAsync(
                    dto,
                    adminId);

                return Ok(new
                {
                    Success = true,
                    Message = "A felhasználó sikeresen módosítva.",
                    UserId = userId
                });
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
                    "Adatbázishiba felhasználó módosításakor.");

                return Conflict(new
                {
                    Message =
                        "A felhasználó módosítása nem sikerült."
                });
            }
        }

        // =========================================
        // FELHASZNÁLÓ TÖRLÉSI INFORMÁCIÓI
        //
        // GET: api/users/5/delete-info
        //
        // Megmutatja:
        // - Felhasználó adatait
        // - Jelentkezések számát
        // - Felügyeletek számát
        // =========================================

        [HttpGet("{userId:int}/delete-info")]
        public async Task<ActionResult<AdminUserDeleteDto>> GetDeleteInfo(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen felhasználóazonosító."
                });
            }

            var info =
                await _userService.GetUserDeleteInfoAsync(userId);

            if (info == null)
            {
                return NotFound(new
                {
                    Message = "A felhasználó nem található."
                });
            }

            return Ok(info);
        }

        // =========================================
        // FELHASZNÁLÓ TÖRLÉSE
        //
        // DELETE: api/users/5
        //
        // A Service kezeli:
        // - Saját adminfiók törlésének tiltását
        // - Jelentkezések törlését
        // - Felügyeletek törlését
        // - Auditnapló megőrzését
        // - Tranzakciót
        // =========================================

        [HttpDelete("{userId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int userId)
        {
            if (!TryGetAdminId(out int adminId))
            {
                return Unauthorized();
            }

            if (userId <= 0)
            {
                return BadRequest(new
                {
                    Message = "Érvénytelen felhasználóazonosító."
                });
            }

            try
            {
                await _userService.DeleteUserAsync(
                    userId,
                    adminId);

                return Ok(new
                {
                    Success = true,
                    Message = "A felhasználó sikeresen törölve.",
                    UserId = userId
                });
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
                    "Adatbázishiba felhasználó törlésekor.");

                return Conflict(new
                {
                    Message =
                        "A felhasználó törlése nem sikerült. " +
                        "Ellenőrizd a kapcsolódó adatokat."
                });
            }
        }

        // =========================================
        // FELHASZNÁLÓK SZÁMÁNAK LEKÉRDEZÉSE
        // GET: api/users/counts
        // =========================================

        [HttpGet("counts")]
        public async Task<ActionResult<UserCountDto>> GetUserCounts()
        {
            var counts = await _userService.GetUserCountsAsync();

            return Ok(counts);
        }


    }
}