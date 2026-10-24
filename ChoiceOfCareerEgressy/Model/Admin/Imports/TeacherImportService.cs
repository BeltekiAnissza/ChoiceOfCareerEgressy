using ChoiceOfCareerEgressy.Dto.UserDto.Teacher;
using ChoiceOfCareerEgressy.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ChoiceOfCareerEgressy.Model.Admin.Imports
{
    public class TeacherImportService
    {
        private readonly ChoiceOfCareerPersistence _context;
        private readonly PasswordService _passwordService;

        public TeacherImportService(
            ChoiceOfCareerPersistence context,
            PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<TeacherImportResultDto> ImportAsync(
            IFormFile file)
        {
            if (file.Length == 0 || file.Length > 5 * 1024 * 1024)
                throw new InvalidDataException(
                    "A fájl mérete 1 bájt és 5 MB között lehet.");

            if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Csak .xlsx fájl tölthető fel.");
            }

            var result = new TeacherImportResultDto();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var sheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidDataException(
                    "A fájl nem tartalmaz munkalapot.");

            static string Normalize(string value) =>
                Regex.Replace(value.Trim(), @"\s+", " ")
                    .ToUpperInvariant();

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
                    throw new InvalidDataException(
                        $"Hiányzó oszlop: {header}");
            }

            string ReadCell(int row, string column) =>
                sheet.Cell(row, headers[Normalize(column)])
                    .GetFormattedString()
                    .Trim();

            var grades = await _context.Grades.ToListAsync();

            var existingEmails = (await _context.Users
                .AsNoTracking()
                .Select(u => u.EmailAddress)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var reservedGrades = new HashSet<int>();

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            for (int row = 2; row <= lastRow; row++)
            {
                var teacher = new TeacherImportDto
                {
                    UserName = ReadCell(row, "név"),
                    GradeName = ReadCell(row, "osztály"),
                    OmIdentifier = ReadCell(row, "om azonosító"),
                    EmailAddress = ReadCell(row, "email")
                        .ToLowerInvariant()
                };

                if (string.IsNullOrWhiteSpace(teacher.UserName)
                    && string.IsNullOrWhiteSpace(teacher.GradeName)
                    && string.IsNullOrWhiteSpace(teacher.OmIdentifier)
                    && string.IsNullOrWhiteSpace(teacher.EmailAddress))
                {
                    continue;
                }

                string? error = null;

                if (string.IsNullOrWhiteSpace(teacher.UserName)
                    || !Regex.IsMatch(
                        teacher.OmIdentifier, @"^\d{11}$")
                    || !new EmailAddressAttribute()
                        .IsValid(teacher.EmailAddress))
                {
                    error = "Hiányzó vagy érvénytelen adat.";
                }

                if (error == null &&
                    existingEmails.Contains(teacher.EmailAddress))
                {
                    error = "Az email-cím már szerepel az adatbázisban.";
                }

                Grade? grade = null;

                if (error == null &&
                    !string.IsNullOrWhiteSpace(teacher.GradeName))
                {
                    grade = grades.FirstOrDefault(g =>
                        Normalize(g.GradeName) ==
                        Normalize(teacher.GradeName));

                    if (grade == null)
                    {
                        error = "A megadott osztály nem létezik.";
                    }
                    else if (grade.HomeroomTeacherId != null
                        || reservedGrades.Contains(grade.GradeId))
                    {
                        error = "Az osztálynak már van osztályfőnöke.";
                    }
                }

                if (error != null)
                {
                    result.SkippedCount++;
                    result.Errors.Add(new TeacherImportErrorDto
                    {
                        RowNumber = row,
                        Message = error
                    });
                    continue;
                }

                var user = new User
                {
                    UserName = teacher.UserName,
                    EmailAddress = teacher.EmailAddress,
                    PasswordHash = "",
                    Role = RoleChoice.Teacher,
                    MustChangePassword = true
                };

                user.PasswordHash = _passwordService.HashPassword(
                    user,
                    teacher.OmIdentifier
                );

                _context.Users.Add(user);

                // Az új tanár ID-ját az adatbázis generálja
                await _context.SaveChangesAsync();

                if (grade != null)
                {
                    grade.HomeroomTeacherId = user.UserId;
                    reservedGrades.Add(grade.GradeId);
                }

                existingEmails.Add(teacher.EmailAddress);
                result.ImportedCount++;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return result;
        }

    }
}
