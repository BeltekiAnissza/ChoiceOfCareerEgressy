using ChoiceOfCareerEgressy.Model.Admin.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Controller.Admin
{
    [Route("api/students")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class StudentsController : ControllerBase
    {
        private readonly StudentImportService _studentImportService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(StudentImportService studentImportService, ILogger<StudentsController> logger)
        {
            _studentImportService = studentImportService;
            _logger = logger;
        }

        // =========================================
        // DIÁKOK IMPORTÁLÁSA EXCEL-FÁJLBÓL
        // =========================================
        //
        // POST: api/students/import
        //
        // Jogosultság: Admin
        //
        // A StudentImportService végzi:
        //
        // - Excel-fájl ellenőrzését
        // - Diákok adatainak beolvasását
        // - Kötelező mezők ellenőrzését
        // - OM-azonosítók ellenőrzését
        // - Email-címek ellenőrzését
        // - Duplikált felhasználók kiszűrését
        // - Osztályok kezelését
        // - Nemek kezelését
        // - Ágazatok kezelését
        // - Jelszavak hashelését
        // - Felhasználók adatbázisba mentését
        //
        // =========================================

        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Import([FromForm] IFormFile file)
        {
            // =====================================
            // FÁJL MEGLÉTÉNEK ELLENŐRZÉSE
            // =====================================

            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    Message = "Válassz ki egy Excel-fájlt!"
                });
            }

            // =====================================
            // FÁJLMÉRET ELLENŐRZÉSE
            // =====================================

            const long maxFileSize = 5 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                return BadRequest(new
                {
                    Message = "Maximum 5 MB-os fájl tölthető fel."
                });
            }

            // =====================================
            // FÁJLKITERJESZTÉS ELLENŐRZÉSE
            // =====================================

            string extension = Path.GetExtension(file.FileName);

            if (!string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    Message = "Csak .xlsx formátumú Excel-fájl tölthető fel."
                });
            }

            // =====================================
            // IMPORTÁLÁS
            // =====================================

            try
            {
                var result = await _studentImportService.ImportAsync(file);

                return Ok(result);
            }

            // =====================================
            // HIBÁS FÁJL
            // =====================================

            catch (InvalidDataException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }

            // =====================================
            // HIBÁS EXCEL-FORMÁTUM
            // =====================================

            catch (FormatException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }

            // =====================================
            // ADATELLENŐRZÉSI HIBA
            // =====================================

            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }

            // =====================================
            // ADATBÁZISHIBA
            // =====================================

            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Adatbázishiba történt a diákok importálása közben.");

                return StatusCode(500, new
                {
                    Message =
                        "Adatbázishiba történt az importálás során. " +
                        "Az importálás nem fejeződött be."
                });
            }

            // =====================================
            // VÁRATLAN HIBA
            // =====================================

            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Váratlan hiba történt a diákok importálása közben.");

                return StatusCode(500, new
                {
                    Message =
                        "Váratlan hiba történt az importálás során."
                });
            }
        }
    }
}