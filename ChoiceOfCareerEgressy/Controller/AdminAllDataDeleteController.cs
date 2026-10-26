using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChoiceOfCareerEgressy.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize("Admin")]
    public class AdminAllDataDeleteController : ControllerBase
    {
        private readonly AdminAllDataDeleteService _deleteService;

        public AdminAllDataDeleteController(AdminAllDataDeleteService deleteService)
        {
            _deleteService = deleteService;
        }

        // Bejelentkezett felhasználó azonosítója
        private int GetUserId()
        {
            string? userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int id))
            {
                throw new UnauthorizedAccessException(
                    "A felhasználó azonosítása sikertelen.");
            }

            return id;
        }

        // ============================================
        // ÖSSZES PROGRAM TÖRLÉSE
        // ============================================

        [HttpPost("programs")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllPrograms()
        {
            try
            {
                int adminId = GetUserId();

                int deletedCount =
                    await _deleteService.DeleteAllProgramsAsync(
                        adminId);

                return Ok(new
                {
                    success = true,
                    message =
                        $"Sikeresen töröltünk {deletedCount} programot.",
                    deletedCount
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Hiba történt a programok törlése közben."
                });
            }
        }

        // ============================================
        // ÖSSZES TANÁR TÖRLÉSE
        // ============================================

        [HttpPost("teachers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllTeachers()
        {
            try
            {
                int adminId = GetUserId();

                int deletedCount =
                    await _deleteService.DeleteAllTeachersAsync(
                        adminId);

                return Ok(new
                {
                    success = true,
                    message =
                        $"Sikeresen töröltünk {deletedCount} tanárt.",
                    deletedCount
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Hiba történt a tanárok törlése közben."
                });
            }
        }

        // ============================================
        // ÖSSZES DIÁK TÖRLÉSE
        // ============================================

        [HttpPost("students")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllStudents()
        {
            try
            {
                int adminId = GetUserId();

                int deletedCount =
                    await _deleteService.DeleteAllStudentsAsync(
                        adminId);

                return Ok(new
                {
                    success = true,
                    message =
                        $"Sikeresen töröltünk {deletedCount} diákot.",
                    deletedCount
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Hiba történt a diákok törlése közben."
                });
            }
        }
    }
}

