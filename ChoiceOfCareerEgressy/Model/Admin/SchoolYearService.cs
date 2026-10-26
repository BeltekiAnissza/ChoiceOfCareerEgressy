using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ChoiceOfCareerEgressy.Model.Admin
{
    public class SchoolYearService
    {
        private readonly ChoiceOfCareerPersistence _db;

        public SchoolYearService(ChoiceOfCareerPersistence db)
        {
            _db = db;
        }

        // Magyarországi naptári év meghatározása
        private static int GetCurrentYear()
        {
            TimeZoneInfo timeZone;

            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest");
            }
            catch (TimeZoneNotFoundException)
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
            }

            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).Year;
        }

        // Következő osztálynév meghatározása
        private static string? GetNextGradeName(string gradeName)
        {
            string name = gradeName.Trim();

            if (name.Equals("9.KNy", StringComparison.OrdinalIgnoreCase))
            {
                return "9.K";
            }

            var match = Regex.Match(name,
                @"^(9|10|11|12|13)\s*\.\s*([ISEK])$",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return null;
            }

            int year = int.Parse(match.Groups[1].Value);

            string letter = match.Groups[2]
                .Value.ToUpperInvariant();

            if (year == 13)
            {
                return "Elballagott";
            }

            return $"{year + 1}.{letter}";
        }

        // Megtörtént-e már az idei tanévindítás?
        public async Task<bool> HasStartedThisYearAsync()
        {
            int year = GetCurrentYear();

            return await _db.SchoolYearTransitions
                .AsNoTracking()
                .AnyAsync(t => t.CalendarYear == year);
        }

        // Új tanév indítása
        public async Task<(int Advanced, int Graduated, int Students)> StartAsync(int adminId)
        {
            // Jogosultságellenőrzés
            bool isAdmin = await _db.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.UserId == adminId &&
                    u.Role == RoleChoice.Admin);

            if (!isAdmin)
            {
                throw new UnauthorizedAccessException(
                    "Csak adminisztrátor indíthat új tanévet.");
            }

            int currentYear = GetCurrentYear();

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            // Éves korlátozás ellenőrzése
            bool alreadyStarted = await _db.SchoolYearTransitions
                .AnyAsync(t => t.CalendarYear == currentYear);

            if (alreadyStarted)
            {
                throw new InvalidOperationException(
                    $"{currentYear}-ban már megtörtént a tanévindítás.");
            }

            // Osztályok betöltése
            var grades = await _db.Grades.ToListAsync();

            var transitions = grades
                .Select(g => new
                {
                    Grade = g,
                    NextName = GetNextGradeName(g.GradeName)
                })
                .Where(x => x.NextName != null)
                .ToList();

            if (transitions.Count == 0)
            {
                throw new InvalidOperationException(
                    "Nincs léptethető osztály.");
            }

            // Ismeretlen osztálynevek ellenőrzése
            var unknownGrades = grades
                .Where(g =>
                    !g.GradeName.Trim().Equals(
                        "Elballagott",
                        StringComparison.OrdinalIgnoreCase)
                    && GetNextGradeName(g.GradeName) == null)
                .Select(g => g.GradeName)
                .ToList();

            if (unknownGrades.Count > 0)
            {
                throw new InvalidOperationException(
                    "Nem támogatott osztálynevek: " +
                    string.Join(", ", unknownGrades));
            }

            // Az új osztálynevek nem ütközhetnek egymással
            var nextNames = transitions
                .Where(x => x.NextName != "Elballagott")
                .Select(x => x.NextName!)
                .ToList();

            if (nextNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != nextNames.Count)
            {
                throw new InvalidOperationException(
                    "Több osztály ugyanazt az új nevet kapná.");
            }

            // Az éves végrehajtás rögzítése.
            // Az egyedi index az ismételt végrehajtás ellen is véd.
            _db.SchoolYearTransitions.Add(
                new SchoolYearTransition
                {
                    CalendarYear = currentYear,
                    ExecutedAt = DateTime.UtcNow,
                    ExecutedByUserId = adminId
                });

            await _db.SaveChangesAsync();

            // Végzős osztályok
            var graduatingGrades = transitions
                .Where(x => x.NextName == "Elballagott")
                .Select(x => x.Grade)
                .ToList();

            int graduatedStudentCount = 0;

            if (graduatingGrades.Count > 0)
            {
                var graduatedGrade = grades.FirstOrDefault(g =>
                    g.GradeName.Trim().Equals(
                        "Elballagott",
                        StringComparison.OrdinalIgnoreCase));

                if (graduatedGrade == null)
                {
                    graduatedGrade = new Grade
                    {
                        GradeName = "Elballagott"
                    };

                    _db.Grades.Add(graduatedGrade);
                    await _db.SaveChangesAsync();
                }

                var graduatingIds = graduatingGrades
                    .Select(g => g.GradeId)
                    .ToList();

                var users = await _db.Users
                    .Where(u =>
                        u.GradeId.HasValue &&
                        graduatingIds.Contains(u.GradeId.Value))
                    .ToListAsync();

                // Diákok áthelyezése, tanárok leválasztása
                foreach (var user in users)
                {
                    if (user.Role == RoleChoice.Student)
                    {
                        user.GradeId = graduatedGrade.GradeId;
                        graduatedStudentCount++;
                    }
                    else if (user.Role == RoleChoice.Teacher)
                    {
                        user.GradeId = null;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "A végzős osztályhoz nem megfelelő " +
                            "szerepkörű felhasználó tartozik.");
                    }
                }

                await _db.SaveChangesAsync();

                // Végzős osztályok törlése
                // Osztályfőnöki kapcsolatok megszüntetése
                foreach (var grade in graduatingGrades)
                {
                    grade.HomeroomTeacherId = null;
                }

                await _db.SaveChangesAsync();

                // Végzős osztályok törlése
                _db.Grades.RemoveRange(graduatingGrades);

                await _db.SaveChangesAsync();

                await _db.SaveChangesAsync();
            }

            // Nem végzős osztályok
            var advancingGrades = transitions
                .Where(x => x.NextName != "Elballagott")
                .ToList();

            // Átmeneti nevek az egyedi index miatt
            foreach (var item in advancingGrades)
            {
                item.Grade.GradeName =
                    $"__YEAR_CHANGE_{Guid.NewGuid():N}";
            }

            await _db.SaveChangesAsync();

            // Végleges osztálynevek
            foreach (var item in advancingGrades)
            {
                item.Grade.GradeName = item.NextName!;
            }

            await _db.SaveChangesAsync();

            string[] newGradeNames = {"9.I","9.S","9.E","9.KNy"};

            // A már létező osztályok nevei
            var existingGradeNames = await _db.Grades
                .AsNoTracking()
                .Select(g => g.GradeName)
                .ToListAsync();

            // Új osztályok létrehozása
            foreach (string gradeName in newGradeNames)
            {
                bool alreadyExists = existingGradeNames.Any(name =>
                    string.Equals(
                        name.Trim(),
                        gradeName,
                        StringComparison.OrdinalIgnoreCase));

                if (!alreadyExists)
                {
                    _db.Grades.Add(new Grade
                    {
                        GradeName = gradeName,
                        HomeroomTeacherId = null
                    });
                }
            }

            // Új osztályok mentése
            await _db.SaveChangesAsync();

            // Auditnapló
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = adminId,
                Action = "StartSchoolYear",
                EntityName = "SchoolYear",
                EntityId = currentYear,
                OldValue = null,
                NewValue =
                    $"Léptetett osztályok: {advancingGrades.Count}; " +
                    $"Ballagó osztályok: {graduatingGrades.Count}; " +
                    $"Ballagó diákok: {graduatedStudentCount}",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // Csak sikeres végrehajtás után véglegesítünk
            await transaction.CommitAsync();

            return (
                advancingGrades.Count,
                graduatingGrades.Count,
                graduatedStudentCount
            );
        }
    }
}

