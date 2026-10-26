using ChoiceOfCareerEgressy.Dto.GradeDto;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model
{
    public class GradeManagementService
    {
        private readonly ChoiceOfCareerPersistence _context;

        public GradeManagementService(ChoiceOfCareerPersistence context)
        {
            _context = context;
        }

        public async Task<List<GradeListItemDto>> GetGradesAsync()
        {
            var grades = await _context.Grades.AsNoTracking()
                .Select(g => new GradeListItemDto
                {
                    GradeId = g.GradeId,
                    GradeName = g.GradeName,
                    StudentCount = _context.Users.Count(u => u.GradeId == g.GradeId && u.Role == RoleChoice.Student),
                    TeacherCount = _context.Users.Count(u => u.GradeId == g.GradeId && u.Role == RoleChoice.Teacher)
                }).ToListAsync();
            return grades.OrderBy(g => g.GradeName).ToList();
        }

        public async Task<int?> GetHomeroomGradeIdAsync(int teacherId) =>
            await _context.Users.AsNoTracking()
                .Where(u => u.UserId == teacherId && u.Role == RoleChoice.Teacher)
                .Select(u => u.GradeId).FirstOrDefaultAsync();

        private async Task<RoleChoice> AuthorizeAsync(int actorId, int? sourceGradeId, bool adminOnly = false)
        {
            var actor = await _context.Users.AsNoTracking()
                .Where(u => u.UserId == actorId)
                .Select(u => new { u.Role, u.GradeId })
                .FirstOrDefaultAsync();

            if (actor == null || (actor.Role != RoleChoice.Admin && actor.Role != RoleChoice.Teacher))
                throw new UnauthorizedAccessException("Nincs jogosultságod ehhez a művelethez.");
            if (adminOnly && actor.Role != RoleChoice.Admin)
                throw new UnauthorizedAccessException("Ezt csak adminisztrátor végezheti el.");
            if (actor.Role == RoleChoice.Teacher &&
                (!sourceGradeId.HasValue || actor.GradeId != sourceGradeId))
                throw new UnauthorizedAccessException("Csak a saját osztályodat nevezheted át.");
            return actor.Role;
        }

        private static string ValidateName(string? value)
        {
            var name = (value ?? "").Trim();
            if (name.Length == 0 || name.Length > 100)
                throw new InvalidOperationException("Az osztálynév 1–100 karakter hosszú lehet.");
            return name;
        }

        private async Task EnsureUniqueNameAsync(string name, int? exceptId = null)
        {
            var existing = await _context.Grades.AsNoTracking()
                .Where(g => !exceptId.HasValue || g.GradeId != exceptId.Value)
                .Select(g => g.GradeName).ToListAsync();
            if (existing.Any(n => string.Equals(n.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Már létezik ilyen nevű osztály.");
        }

        public async Task RenameAsync(int actorId, GradeRenameDto dto)
        {
            await AuthorizeAsync(actorId, dto.GradeId);
            var name = ValidateName(dto.NewGradeName);
            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.GradeId == dto.GradeId)
                ?? throw new InvalidOperationException("Az osztály nem található.");
            await EnsureUniqueNameAsync(name, dto.GradeId);
            if (grade.GradeName == name) return;
            grade.GradeName = name;
            await _context.SaveChangesAsync();
        }

        public async Task<int> CreateAsync(int actorId, GradeCreateDto dto)
        {
            await AuthorizeAsync(actorId, null, adminOnly: true);
            var name = ValidateName(dto.GradeName);
            await EnsureUniqueNameAsync(name);
            var grade = new Grade { GradeName = name };
            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();
            return grade.GradeId;
        }

        public async Task DeleteAsync(int actorId, int gradeId)
        {
            await AuthorizeAsync(actorId, null, adminOnly: true);

            var grade = await _context.Grades
                .FirstOrDefaultAsync(g => g.GradeId == gradeId)
                ?? throw new InvalidOperationException(
                    "Az osztály nem található.");

            bool hasUsers = await _context.Users
                .AnyAsync(u => u.GradeId == gradeId);

            if (hasUsers || grade.HomeroomTeacherId != null)
            {
                throw new InvalidOperationException(
                    "Az osztályhoz még diák vagy tanár tartozik. " +
                    "Előbb szüntesd meg a hozzárendeléseket.");
            }

            _context.Grades.Remove(grade);

            await _context.SaveChangesAsync();
        }
        public async Task GraduateAsync(int actorId, int gradeId)
        {
            await AuthorizeAsync(actorId, null, adminOnly: true);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var grade = await _context.Grades
                .FirstOrDefaultAsync(g => g.GradeId == gradeId)
                ?? throw new InvalidOperationException(
                    "Az osztály nem található.");

            if (string.Equals(
                grade.GradeName.Trim(),
                "Elballagott",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Az Elballagott osztály nem ballagtatható.");
            }

            var graduatedGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.GradeName == "Elballagott");

            if (graduatedGrade == null)
            {
                graduatedGrade = new Grade
                {
                    GradeName = "Elballagott"
                };

                _context.Grades.Add(graduatedGrade);
                await _context.SaveChangesAsync();
            }

            var users = await _context.Users
                .Where(u => u.GradeId == gradeId)
                .ToListAsync();

            foreach (var user in users)
            {
                if (user.Role == RoleChoice.Student)
                {
                    user.GradeId = graduatedGrade.GradeId;
                }
                else if (user.Role == RoleChoice.Teacher)
                {
                    user.GradeId = null;
                }
                else
                {
                    throw new InvalidOperationException(
                        "Az osztályhoz más szerepkörű felhasználó is tartozik.");
                }
            }

            await _context.SaveChangesAsync();

            _context.Grades.Remove(grade);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        public async Task<int> DeleteGraduatedAsync(int actorId, int gradeId)
        {
            // Csak adminisztrátor végezheti el.
            await AuthorizeAsync(actorId, null, adminOnly: true);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var grade = await _context.Grades
                .FirstOrDefaultAsync(g => g.GradeId == gradeId)
                ?? throw new InvalidOperationException(
                    "Az osztály nem található.");

            // Más osztályt ezzel a művelettel nem lehet törölni.
            if (!string.Equals(
                grade.GradeName.Trim(),
                "Elballagott",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Ez a művelet csak az Elballagott osztálynál használható.");
            }

            // Biztonsági ellenőrzés: tanárokat nem törlünk.
            bool hasNonStudents = await _context.Users
                .AnyAsync(u =>
                    u.GradeId == gradeId &&
                    u.Role != RoleChoice.Student);

            if (hasNonStudents)
            {
                throw new InvalidOperationException(
                    "Az Elballagott osztályhoz nem diák szerepkörű felhasználó is tartozik. Előbb módosítsd a hozzárendelését.");
            }

            // Az osztályhoz tartozó diákok azonosítói.
            var studentIds = await _context.Users
                .Where(u =>
                    u.GradeId == gradeId &&
                    u.Role == RoleChoice.Student)
                .Select(u => u.UserId)
                .ToListAsync();

            // Ellenőrizzük, hogy valamelyik diák nincs-e
            // más osztály osztályfőnökeként megadva.
            bool referencedAsTeacher = await _context.Grades
                .AnyAsync(g =>
                    g.HomeroomTeacherId.HasValue &&
                    studentIds.Contains(g.HomeroomTeacherId.Value));

            if (referencedAsTeacher)
            {
                throw new InvalidOperationException(
                    "Egy törlendő diák másik osztály osztályfőnökeként szerepel. Előbb javítsd ezt a hozzárendelést.");
            }

            // 1. Jelentkezések törlése.
            var applications = await _context.Applications
                .Where(a => studentIds.Contains(a.UserId))
                .ToListAsync();

            _context.Applications.RemoveRange(applications);

            // 2. Felügyeleti hozzárendelések törlése.
            var supervisions = await _context.Supervisions
                .Where(s => studentIds.Contains(s.UserId))
                .ToListAsync();

            _context.Supervisions.RemoveRange(supervisions);

            // 3. Diákok törlése.
            var students = await _context.Users
                .Where(u => studentIds.Contains(u.UserId))
                .ToListAsync();

            _context.Users.RemoveRange(students);

            // A kapcsolódó rekordokat a felhasználók előtt
            // mentjük, mert az idegen kulcsok Restrict típusúak.
            await _context.SaveChangesAsync();

            // 4. Osztály törlése.
            _context.Grades.Remove(grade);

            // 5. Auditnapló.
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = actorId,
                Action = "DeleteGraduatedGrade",
                EntityName = "Grade",
                EntityId = gradeId,
                OldValue = grade.GradeName,
                NewValue =
                    $"Véglegesen törölve: {students.Count} diák, " +
                    $"{applications.Count} jelentkezés, " +
                    $"{supervisions.Count} felügyeleti hozzárendelés.",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return students.Count;
        }

    }
}
