using ChoiceOfCareerEgressy.Dto.UserDto.Teacher;
using ChoiceOfCareerEgressy.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ChoiceOfCareerEgressy.Model.Admin.Imports
{
    public class TeacherImportService
    {
        private readonly ChoiceOfCareerPersistence _context;
        private readonly PasswordService _passwordService;
        private readonly ILogger<TeacherImportService> _logger;

        public TeacherImportService(
            ChoiceOfCareerPersistence context,
            PasswordService passwordService,
            ILogger<TeacherImportService> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _logger = logger;
        }

        public async Task<TeacherImportResultDto> ImportAsync(IFormFile file)
        {
            var totalTimer = Stopwatch.StartNew();
            var result = new TeacherImportResultDto();

            // =========================================
            // 1. FÁJL ELLENŐRZÉSE
            // =========================================

            if (file.Length == 0 || file.Length > 5 * 1024 * 1024)
            {
                throw new InvalidDataException(
                    "A fájl mérete 1 bájt és 5 MB között lehet.");
            }

            if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Csak .xlsx fájl tölthető fel.");
            }

            // =========================================
            // 2. EXCEL BEOLVASÁSA
            // =========================================

            var excelTimer = Stopwatch.StartNew();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var sheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidDataException(
                    "A fájl nem tartalmaz munkalapot.");

            static string Normalize(string value)
            {
                return Regex.Replace(
                    value.Trim(),
                    @"\s+",
                    " ").ToUpperInvariant();
            }

            var headers = sheet.Row(1)
                .CellsUsed()
                .ToDictionary(
                    c => Normalize(c.GetString()),
                    c => c.Address.ColumnNumber
                );

            string[] requiredHeaders =
            {
                "név",
                "osztály",
                "om azonosító",
                "email"
            };

            foreach (var header in requiredHeaders)
            {
                if (!headers.ContainsKey(Normalize(header)))
                {
                    throw new InvalidDataException(
                        $"Hiányzó oszlop: {header}");
                }
            }

            // Oszlopindexek egyszeri meghatározása

            int nameColumn = headers[Normalize("név")];
            int gradeColumn = headers[Normalize("osztály")];
            int omColumn = headers[Normalize("om azonosító")];
            int emailColumn = headers[Normalize("email")];

            var teachers =
                new List<(int Row, TeacherImportDto Data)>();

            var emailValidator = new EmailAddressAttribute();

            int lastRow =
                sheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                var teacher = new TeacherImportDto
                {
                    UserName = sheet.Cell(row, nameColumn)
                        .GetFormattedString().Trim(),

                    GradeName = sheet.Cell(row, gradeColumn)
                        .GetFormattedString().Trim(),

                    OmIdentifier = sheet.Cell(row, omColumn)
                        .GetFormattedString().Trim(),

                    EmailAddress = sheet.Cell(row, emailColumn)
                        .GetFormattedString().Trim()
                        .ToLowerInvariant()
                };

                // Teljesen üres sorok kihagyása

                if (string.IsNullOrWhiteSpace(teacher.UserName)
                    && string.IsNullOrWhiteSpace(teacher.GradeName)
                    && string.IsNullOrWhiteSpace(teacher.OmIdentifier)
                    && string.IsNullOrWhiteSpace(teacher.EmailAddress))
                {
                    continue;
                }

                // Alapvető adatok ellenőrzése

                if (string.IsNullOrWhiteSpace(teacher.UserName)
                    || !Regex.IsMatch(
                        teacher.OmIdentifier,
                        @"^\d{11}$")
                    || !emailValidator.IsValid(
                        teacher.EmailAddress))
                {
                    result.SkippedCount++;

                    result.Errors.Add(
                        new TeacherImportErrorDto
                        {
                            RowNumber = row,
                            Message = "Hiányzó vagy érvénytelen adat."
                        });

                    continue;
                }

                teachers.Add((row, teacher));
            }

            excelTimer.Stop();

            // =========================================
            // 3. MEGLÉVŐ ADATOK BETÖLTÉSE
            // =========================================

            var databaseReadTimer = Stopwatch.StartNew();

            // Az osztályokat trackinggel töltjük be,
            // mert később módosítjuk a HomeroomTeacherId-t.

            var grades = await _context.Grades.ToListAsync();

            var gradeDictionary = grades
                .GroupBy(g => Normalize(g.GradeName))
                .ToDictionary(
                    g => g.Key,
                    g => g.First());

            var existingEmails = (await _context.Users
                .AsNoTracking()
                .Select(u => u.EmailAddress)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Osztályonként már hozzárendelt tanárok száma

            var teacherCounts = await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.Role == RoleChoice.Teacher &&
                    u.GradeId != null)
                .GroupBy(u => u.GradeId!.Value)
                .Select(g => new
                {
                    GradeId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.GradeId,
                    x => x.Count
                );

            // Már meglévő tanárok és osztálykapcsolataik

            var linkedTeachers = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == RoleChoice.Teacher)
                .Select(u => new
                {
                    u.UserId,
                    u.GradeId
                })
                .ToListAsync();

            var linkedTeacherSet = linkedTeachers
                .Where(u => u.GradeId.HasValue)
                .Select(u => (u.UserId, GradeId: u.GradeId!.Value))
                .ToHashSet();

            // Az elsődleges osztályfőnököt is beleszámítjuk,
            // ha nincs User.GradeId kapcsolata.

            foreach (var grade in grades)
            {
                if (grade.HomeroomTeacherId == null)
                {
                    continue;
                }

                bool primaryTeacherAlreadyCounted =
                    linkedTeacherSet.Contains((
                        grade.HomeroomTeacherId.Value,
                        grade.GradeId
                    ));

                if (!primaryTeacherAlreadyCounted)
                {
                    teacherCounts.TryGetValue(
                        grade.GradeId,
                        out int count);

                    teacherCounts[grade.GradeId] = count + 1;
                }
            }

            databaseReadTimer.Stop();

            // =========================================
            // 4. TANÁROK ELŐKÉSZÍTÉSE
            // =========================================

            var preparationTimer = Stopwatch.StartNew();

            var usersToInsert = new List<User>();

            var usersToHash =
                new List<(User User, string Password)>();

            // Osztályonként megjegyezzük,
            // melyik új tanár lesz az osztályfőnök.

            var newHomeroomTeachers =
                new Dictionary<int, User>();

            foreach (var item in teachers)
            {
                var teacher = item.Data;

                string? error = null;

                // =========================================
                // DUPLIKÁLT EMAIL ELLENŐRZÉSE
                // =========================================

                if (existingEmails.Contains(teacher.EmailAddress))
                {
                    error =
                        "Az email-cím már szerepel az adatbázisban vagy a fájlban.";
                }

                // =========================================
                // OSZTÁLY ELLENŐRZÉSE
                // =========================================

                Grade? grade = null;
                int assignedTeacherCount = 0;

                if (error == null &&
                    !string.IsNullOrWhiteSpace(teacher.GradeName))
                {
                    string gradeKey = Normalize(teacher.GradeName);

                    if (!gradeDictionary.TryGetValue(
                        gradeKey,
                        out grade))
                    {
                        error = "A megadott osztály nem létezik.";
                    }
                    else
                    {
                        teacherCounts.TryGetValue(
                            grade.GradeId,
                            out assignedTeacherCount);

                        if (assignedTeacherCount >= 2)
                        {
                            error =
                                "Az osztályhoz már két tanár tartozik.";
                        }
                    }
                }

                // =========================================
                // HIBÁS SOR KIHAGYÁSA
                // =========================================

                if (error != null)
                {
                    result.SkippedCount++;

                    result.Errors.Add(
                        new TeacherImportErrorDto
                        {
                            RowNumber = item.Row,
                            Message = error
                        });

                    continue;
                }

                // =========================================
                // TANÁR LÉTREHOZÁSA
                // =========================================

                var user = new User
                {
                    UserName = teacher.UserName,
                    EmailAddress = teacher.EmailAddress,
                    PasswordHash = "",
                    Role = RoleChoice.Teacher,
                    GradeId = grade?.GradeId,
                    MustChangePassword = true
                };

                usersToInsert.Add(user);

                usersToHash.Add((
                    user,
                    teacher.OmIdentifier
                ));

                // A sikeresen feldolgozott emailt
                // azonnal hozzáadjuk a HashSet-hez.

                existingEmails.Add(teacher.EmailAddress);

                // =========================================
                // OSZTÁLYFŐNÖKI KAPCSOLAT ELŐKÉSZÍTÉSE
                // =========================================

                if (grade != null)
                {
                    // Ha még nincs osztályfőnök,
                    // az első új tanár lesz az.

                    if (grade.HomeroomTeacherId == null &&
                        !newHomeroomTeachers.ContainsKey(
                            grade.GradeId))
                    {
                        newHomeroomTeachers.Add(
                            grade.GradeId,
                            user);
                    }

                    // A következő tanár már beleszámít
                    // a maximum 2 fős korlátba.

                    teacherCounts[grade.GradeId] =
                        assignedTeacherCount + 1;
                }
            }

            preparationTimer.Stop();

            // =========================================
            // 5. PÁRHUZAMOS JELSZÓHASHELÉS
            // =========================================

            var hashTimer = Stopwatch.StartNew();

            int maxParallelism = Math.Max(
                1,
                Math.Min(Environment.ProcessorCount, 4)
            );

            Parallel.ForEach(
                usersToHash,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxParallelism
                },
                item =>
                {
                    // Minden szál csak a saját User
                    // objektumának jelszavát módosítja.
                    //
                    // Itt nincs EF Core adatbázis-művelet.

                    item.User.PasswordHash =
                        _passwordService.HashPassword(
                            item.User,
                            item.Password);
                });

            hashTimer.Stop();

            // =========================================
            // 6. ADATBÁZISBA MENTÉS
            // =========================================

            var saveTimer = Stopwatch.StartNew();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // =========================================
                // ELSŐ MENTÉS: TANÁROK
                // =========================================

                _context.Users.AddRange(usersToInsert);

                await _context.SaveChangesAsync();

                // Az új tanárok már rendelkeznek UserId-val.

                // =========================================
                // OSZTÁLYFŐNÖKÖK BEÁLLÍTÁSA
                // =========================================

                foreach (var item in newHomeroomTeachers)
                {
                    int gradeId = item.Key;

                    User teacher = item.Value;

                    if (gradeDictionary.Values
                        .FirstOrDefault(g =>
                            g.GradeId == gradeId) is Grade grade)
                    {
                        grade.HomeroomTeacherId =
                            teacher.UserId;
                    }
                }

                // =========================================
                // MÁSODIK MENTÉS: OSZTÁLYFŐNÖKÖK
                // =========================================

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Adatbázishiba a tanárimport során. " +
                    "Belső hiba: {InnerError}",
                    ex.InnerException?.Message);

                throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            saveTimer.Stop();

            result.ImportedCount = usersToInsert.Count;

            totalTimer.Stop();

            // =========================================
            // 7. TELJESÍTMÉNYMÉRÉS
            // =========================================

            _logger.LogInformation(
                "Tanárimport befejezve. " +
                "Importált: {Imported}, " +
                "Kihagyott: {Skipped}, " +
                "Excel: {ExcelMs} ms, " +
                "Adatok betöltése: {ReadMs} ms, " +
                "Előkészítés: {PreparationMs} ms, " +
                "Jelszóhashelés: {HashMs} ms, " +
                "Mentés: {SaveMs} ms, " +
                "Összesen: {TotalMs} ms, " +
                "Párhuzamos szálak: {Parallelism}",
                result.ImportedCount,
                result.SkippedCount,
                excelTimer.ElapsedMilliseconds,
                databaseReadTimer.ElapsedMilliseconds,
                preparationTimer.ElapsedMilliseconds,
                hashTimer.ElapsedMilliseconds,
                saveTimer.ElapsedMilliseconds,
                totalTimer.ElapsedMilliseconds,
                maxParallelism);

            return result;
        }
    }
}