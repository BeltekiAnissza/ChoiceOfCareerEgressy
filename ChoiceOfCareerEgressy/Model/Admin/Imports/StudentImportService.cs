using ChoiceOfCareerEgressy.Dto.UserDto.Student;
using ChoiceOfCareerEgressy.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ChoiceOfCareerEgressy.Model.Admin.Imports
{
    public class StudentImportService
    {
        private readonly ChoiceOfCareerPersistence _context;
        private readonly PasswordService _passwordService;
        private readonly ILogger<StudentImportService> _logger;

        public StudentImportService(ChoiceOfCareerPersistence context, PasswordService passwordService, ILogger<StudentImportService> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _logger = logger;
        }

        public async Task<StudentImportResultDto> ImportAsync(IFormFile file)
        {
            var totalTimer = Stopwatch.StartNew();

            var result = new StudentImportResultDto();

            // =========================================
            // 1. FÁJL ELLENŐRZÉSE
            // =========================================

            if (file.Length == 0)
            {
                throw new InvalidDataException(
                    "Az Excel-fájl üres.");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                throw new InvalidDataException(
                    "Maximum 5 MB-os fájl tölthető fel.");
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

            var sheet = workbook.Worksheets.FirstOrDefault();

            if (sheet == null)
            {
                throw new InvalidDataException(
                    "Az Excel nem tartalmaz munkalapot.");
            }

            static string Normalize(string value)
            {
                return Regex.Replace(
                    value.Trim(),
                    @"\s+",
                    " ").ToUpperInvariant();
            }

            var requiredHeaders = new[]
            {
                "osztály",
                "név",
                "om azonosító",
                "sex",
                "ágazat",
                "email"
            };

            var headers = sheet.Row(1)
                .CellsUsed()
                .ToDictionary(
                    c => Normalize(c.GetString()),
                    c => c.Address.ColumnNumber
                );

            foreach (var header in requiredHeaders)
            {
                if (!headers.ContainsKey(Normalize(header)))
                {
                    throw new InvalidDataException(
                        $"Hiányzó Excel-oszlop: {header}");
                }
            }

            // Oszlopindexek egyszeri meghatározása

            int gradeColumn = headers[Normalize("osztály")];
            int nameColumn = headers[Normalize("név")];
            int omColumn = headers[Normalize("om azonosító")];
            int sexColumn = headers[Normalize("sex")];
            int branchColumn = headers[Normalize("ágazat")];
            int emailColumn = headers[Normalize("email")];

            var students =
                new List<(int Row, StudentImportDto Data)>();

            int lastRow =
                sheet.LastRowUsed()?.RowNumber() ?? 1;

            var emailValidator = new EmailAddressAttribute();

            for (int row = 2; row <= lastRow; row++)
            {
                var student = new StudentImportDto
                {
                    GradeName = sheet.Cell(row, gradeColumn)
                        .GetFormattedString().Trim(),

                    UserName = sheet.Cell(row, nameColumn)
                        .GetFormattedString().Trim(),

                    OmIdentifier = sheet.Cell(row, omColumn)
                        .GetFormattedString().Trim(),

                    SexName = sheet.Cell(row, sexColumn)
                        .GetFormattedString().Trim(),

                    BranchName = sheet.Cell(row, branchColumn)
                        .GetFormattedString().Trim(),

                    EmailAddress = sheet.Cell(row, emailColumn)
                        .GetFormattedString().Trim()
                        .ToLowerInvariant()
                };

                // Üres sorok kihagyása

                if (string.IsNullOrWhiteSpace(student.GradeName)
                    && string.IsNullOrWhiteSpace(student.UserName)
                    && string.IsNullOrWhiteSpace(student.OmIdentifier)
                    && string.IsNullOrWhiteSpace(student.SexName)
                    && string.IsNullOrWhiteSpace(student.BranchName)
                    && string.IsNullOrWhiteSpace(student.EmailAddress))
                {
                    continue;
                }

                // Adatok ellenőrzése

                bool invalid =
                    string.IsNullOrWhiteSpace(student.GradeName)
                    || string.IsNullOrWhiteSpace(student.UserName)
                    || string.IsNullOrWhiteSpace(student.SexName)
                    || string.IsNullOrWhiteSpace(student.BranchName)
                    || string.IsNullOrWhiteSpace(student.EmailAddress)
                    || !Regex.IsMatch(
                        student.OmIdentifier,
                        @"^\d{11}$")
                    || !emailValidator.IsValid(
                        student.EmailAddress);

                if (invalid)
                {
                    result.SkippedCount++;

                    result.Errors.Add(
                        new StudentImportErrorDto
                        {
                            RowNumber = row,
                            Message = "Hiányzó vagy érvénytelen adat."
                        });

                    continue;
                }

                students.Add((row, student));
            }

            excelTimer.Stop();

            // =========================================
            // 3. MEGLÉVŐ ADATOK BETÖLTÉSE
            // =========================================

            var databaseReadTimer = Stopwatch.StartNew();

            var existingEmails = (await _context.Users
                .AsNoTracking()
                .Select(u => u.EmailAddress)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // FONTOS:
            // Ezeket trackinggel töltjük be,
            // mert navigációs tulajdonságként
            // hozzárendeljük őket a felhasználókhoz.

            var grades = await _context.Grades.ToListAsync();

            var sexes = await _context.Sexes.ToListAsync();

            var branches = await _context.Branches.ToListAsync();

            // =========================================
            // 4. SZÓTÁRAK LÉTREHOZÁSA
            // =========================================

            var gradeDictionary = grades
                .GroupBy(g => Normalize(g.GradeName))
                .ToDictionary(
                    g => g.Key,
                    g => g.First());

            var sexDictionary = sexes
                .GroupBy(s => Normalize(s.SexName))
                .ToDictionary(
                    s => s.Key,
                    s => s.First());

            var branchDictionary = branches
                .GroupBy(b => Normalize(b.BranchName))
                .ToDictionary(
                    b => b.Key,
                    b => b.First());

            databaseReadTimer.Stop();

            // =========================================
            // 5. FELHASZNÁLÓK ELŐKÉSZÍTÉSE
            // =========================================

            var preparationTimer = Stopwatch.StartNew();

            var usersToInsert = new List<User>();

            // Ebben tároljuk a felhasználókat
            // és a hozzájuk tartozó kezdeti jelszavakat.

            var usersToHash =
                new List<(User User, string Password)>();

            var gradesToInsert = new List<Grade>();
            var sexesToInsert = new List<Sex>();
            var branchesToInsert = new List<Branch>();

            foreach (var item in students)
            {
                var student = item.Data;

                // =========================================
                // DUPLIKÁLT EMAIL ELLENŐRZÉSE
                // =========================================

                if (!existingEmails.Add(student.EmailAddress))
                {
                    result.SkippedCount++;

                    result.Errors.Add(
                        new StudentImportErrorDto
                        {
                            RowNumber = item.Row,
                            Message =
                                "Az email-cím már szerepel az adatbázisban vagy a fájlban."
                        });

                    continue;
                }

                // =========================================
                // OSZTÁLY
                // =========================================

                string gradeKey = Normalize(student.GradeName);

                if (!gradeDictionary.TryGetValue(
                    gradeKey,
                    out var grade))
                {
                    grade = new Grade
                    {
                        GradeName = student.GradeName
                    };

                    gradeDictionary.Add(gradeKey, grade);

                    gradesToInsert.Add(grade);
                }

                // =========================================
                // NEM
                // =========================================

                string sexKey = Normalize(student.SexName);

                if (!sexDictionary.TryGetValue(
                    sexKey,
                    out var sex))
                {
                    sex = new Sex
                    {
                        SexName = student.SexName
                    };

                    sexDictionary.Add(sexKey, sex);

                    sexesToInsert.Add(sex);
                }

                // =========================================
                // ÁGAZAT
                // =========================================

                string branchKey = Normalize(student.BranchName);

                if (!branchDictionary.TryGetValue(
                    branchKey,
                    out var branch))
                {
                    branch = new Branch
                    {
                        BranchName = student.BranchName
                    };

                    branchDictionary.Add(branchKey, branch);

                    branchesToInsert.Add(branch);
                }

                // =========================================
                // FELHASZNÁLÓ LÉTREHOZÁSA
                // =========================================

                var user = new User
                {
                    UserName = student.UserName,
                    EmailAddress = student.EmailAddress,
                    PasswordHash = "",
                    Role = RoleChoice.Student,
                    MustChangePassword = true,
                    Grade = grade,
                    Sex = sex,
                    Branch = branch
                };

                usersToInsert.Add(user);

                usersToHash.Add((
                    user,
                    student.OmIdentifier
                ));
            }

            preparationTimer.Stop();

            // =========================================
            // 6. PÁRHUZAMOS JELSZÓHASHELÉS
            // =========================================

            var hashTimer = Stopwatch.StartNew();

            // Egyszerre maximum 4 felhasználó
            // jelszavát hasheljük.

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
                    // Itt kizárólag a saját User objektum
                    // PasswordHash tulajdonságát módosítjuk.
                    //
                    // EF Core adatbázis-műveletet
                    // nem végzünk párhuzamosan.

                    item.User.PasswordHash =
                        _passwordService.HashPassword(
                            item.User,
                            item.Password);
                });

            hashTimer.Stop();

            // =========================================
            // 7. ADATBÁZISBA MENTÉS
            // =========================================

            var saveTimer = Stopwatch.StartNew();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Új osztályok hozzáadása

                _context.Grades.AddRange(gradesToInsert);

                // Új nemek hozzáadása

                _context.Sexes.AddRange(sexesToInsert);

                // Új ágazatok hozzáadása

                _context.Branches.AddRange(branchesToInsert);

                // Új felhasználók hozzáadása

                _context.Users.AddRange(usersToInsert);

                // Egyetlen mentés

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Adatbázishiba a diákimport során. " +
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
            // 8. TELJESÍTMÉNYMÉRÉS
            // =========================================

            _logger.LogInformation(
                "Diákimport befejezve. " +
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