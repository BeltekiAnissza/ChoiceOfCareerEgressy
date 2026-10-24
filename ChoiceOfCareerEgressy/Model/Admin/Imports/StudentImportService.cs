using ChoiceOfCareerEgressy.Dto.UserDto.Student;
using ChoiceOfCareerEgressy.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ChoiceOfCareerEgressy.Model.Admin.Imports
{
    public class StudentImportService
    {
        private readonly ChoiceOfCareerPersistence _context;
        private readonly PasswordService _passwordService;

        public StudentImportService(
            ChoiceOfCareerPersistence context,
            PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<StudentImportResultDto> ImportAsync(
            IFormFile file)
        {
            var result = new StudentImportResultDto();

            if (file.Length == 0)
                throw new InvalidDataException("Az Excel-fájl üres.");

            if (file.Length > 5 * 1024 * 1024)
                throw new InvalidDataException("Maximum 5 MB-os fájl tölthető fel.");

            if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Csak .xlsx fájl tölthető fel.");
            }

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var sheet = workbook.Worksheets.FirstOrDefault();

            if (sheet == null)
                throw new InvalidDataException("Az Excel nem tartalmaz munkalapot.");

            string Normalize(string value) =>
                Regex.Replace(value.Trim(), @"\s+", " ")
                    .ToUpperInvariant();

            var requiredHeaders = new[]
            {
            "osztály", "név", "om azonosító",
            "sex", "ágazat", "email"
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
                        $"Hiányzó Excel-oszlop: {header}"
                    );
                }
            }

            string ReadCell(int row, string column)
            {
                return sheet.Cell(
                    row,
                    headers[Normalize(column)]
                ).GetFormattedString().Trim();
            }

            var students = new List<(int Row, StudentImportDto Data)>();

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                var student = new StudentImportDto
                {
                    GradeName = ReadCell(row, "osztály"),
                    UserName = ReadCell(row, "név"),
                    OmIdentifier = ReadCell(row, "om azonosító"),
                    SexName = ReadCell(row, "sex"),
                    BranchName = ReadCell(row, "ágazat"),
                    EmailAddress = ReadCell(row, "email")
                        .ToLowerInvariant()
                };

                if (string.IsNullOrWhiteSpace(student.GradeName)
                    && string.IsNullOrWhiteSpace(student.UserName)
                    && string.IsNullOrWhiteSpace(student.OmIdentifier)
                    && string.IsNullOrWhiteSpace(student.SexName)
                    && string.IsNullOrWhiteSpace(student.BranchName)
                    && string.IsNullOrWhiteSpace(student.EmailAddress))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(student.GradeName)
                    || string.IsNullOrWhiteSpace(student.UserName)
                    || string.IsNullOrWhiteSpace(student.SexName)
                    || string.IsNullOrWhiteSpace(student.BranchName)
                    || string.IsNullOrWhiteSpace(student.EmailAddress)
                    || !Regex.IsMatch(student.OmIdentifier, @"^\d{11}$")
                    || !new EmailAddressAttribute()
                        .IsValid(student.EmailAddress))
                {
                    result.SkippedCount++;
                    result.Errors.Add(new StudentImportErrorDto
                    {
                        RowNumber = row,
                        Message = "Hiányzó vagy érvénytelen adat."
                    });
                    continue;
                }

                students.Add((row, student));
            }

            var existingEmails = (await _context.Users
                .AsNoTracking()
                .Select(u => u.EmailAddress)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var grades = await _context.Grades.ToListAsync();
            var sexes = await _context.Sexes.ToListAsync();
            var branches = await _context.Branches.ToListAsync();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            foreach (var item in students)
            {
                var student = item.Data;

                if (!existingEmails.Add(student.EmailAddress))
                {
                    result.SkippedCount++;
                    result.Errors.Add(new StudentImportErrorDto
                    {
                        RowNumber = item.Row,
                        Message = "Az email-cím már szerepel az adatbázisban vagy a fájlban."
                    });
                    continue;
                }

                var grade = grades.FirstOrDefault(g =>
                    Normalize(g.GradeName) == Normalize(student.GradeName));

                if (grade == null)
                {
                    grade = new Grade
                    {
                        GradeName = student.GradeName
                    };

                    _context.Grades.Add(grade);
                    grades.Add(grade);
                }

                var sex = sexes.FirstOrDefault(s =>
                    Normalize(s.SexName) == Normalize(student.SexName));

                if (sex == null)
                {
                    sex = new Sex
                    {
                        SexName = student.SexName
                    };

                    _context.Sexes.Add(sex);
                    sexes.Add(sex);
                }

                var branch = branches.FirstOrDefault(b =>
                    Normalize(b.BranchName) == Normalize(student.BranchName));

                if (branch == null)
                {
                    branch = new Branch
                    {
                        BranchName = student.BranchName
                    };

                    _context.Branches.Add(branch);
                    branches.Add(branch);
                }

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

                user.PasswordHash = _passwordService.HashPassword(
                    user,
                    student.OmIdentifier
                );

                _context.Users.Add(user);
                result.ImportedCount++;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return result;
        }
    }

}