using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model.Admin
{
    public class AdminAllDataDeleteService
    {
        private readonly ChoiceOfCareerPersistence _context;

        public AdminAllDataDeleteService(ChoiceOfCareerPersistence context)
        {
            _context = context;
        }

        // ADMIN JOGOSULTSÁG ELLENŐRZÉSE
        private async Task CheckAdminAsync(int adminId)
        {
            bool isAdmin = await _context.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.UserId == adminId &&
                    u.Role == RoleChoice.Admin);

            if (!isAdmin)
            {
                throw new UnauthorizedAccessException(
                    "A műveletet kizárólag adminisztrátor végezheti el.");
            }
        }

        // =====================================================
        // 1. ÖSSZES PROGRAM TÖRLÉSE
        // =====================================================

        public async Task<int> DeleteAllProgramsAsync(int adminId)
        {
            await CheckAdminAsync(adminId);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            int programCount = await _context.Plans.CountAsync();

            // Jelentkezések törlése
            await _context.Applications.ExecuteDeleteAsync();

            // Programokhoz tartozó felügyeletek törlése
            await _context.Supervisions
                .Where(s => s.PlanSessionId != null)
                .ExecuteDeleteAsync();

            // Programalkalmak törlése
            await _context.PlanSessions.ExecuteDeleteAsync();

            // Programok törlése
            await _context.Plans.ExecuteDeleteAsync();

            // Auditnapló
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminId,
                Action = "DeleteAllPrograms",
                EntityName = "Plan",
                EntityId = 0,
                OldValue = $"Programok száma: {programCount}",
                NewValue = "Az összes program törölve.",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return programCount;
        }

        // =====================================================
        // 2. ÖSSZES TANÁR TÖRLÉSE
        // =====================================================

        public async Task<int> DeleteAllTeachersAsync(int adminId)
        {
            await CheckAdminAsync(adminId);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var teacherIds = await _context.Users
                .Where(u => u.Role == RoleChoice.Teacher)
                .Select(u => u.UserId)
                .ToListAsync();

            int teacherCount = teacherIds.Count;

            if (teacherCount > 0)
            {
                // Osztályfőnöki kapcsolatok megszüntetése
                await _context.Grades
                    .Where(g =>
                        g.HomeroomTeacherId.HasValue &&
                        teacherIds.Contains(
                            g.HomeroomTeacherId.Value))
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            g => g.HomeroomTeacherId,
                            (int?)null));

                // Tanárok jelentkezéseinek törlése,
                // ha esetleg ilyen rekord is létezik
                await _context.Applications
                    .Where(a => teacherIds.Contains(a.UserId))
                    .ExecuteDeleteAsync();

                // Tanári felügyeletek törlése
                await _context.Supervisions
                    .Where(s => teacherIds.Contains(s.UserId))
                    .ExecuteDeleteAsync();

                // Tanári fiókok törlése
                await _context.Users
                    .Where(u => u.Role == RoleChoice.Teacher)
                    .ExecuteDeleteAsync();
            }

            // Auditnapló
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminId,
                Action = "DeleteAllTeachers",
                EntityName = "User",
                EntityId = 0,
                OldValue = $"Tanárok száma: {teacherCount}",
                NewValue = "Az összes tanár törölve.",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return teacherCount;
        }

        // =====================================================
        // 3. ÖSSZES DIÁK TÖRLÉSE
        // =====================================================

        public async Task<int> DeleteAllStudentsAsync(int adminId)
        {
            await CheckAdminAsync(adminId);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var studentIds = await _context.Users
                .Where(u => u.Role == RoleChoice.Student)
                .Select(u => u.UserId)
                .ToListAsync();

            int studentCount = studentIds.Count;

            if (studentCount > 0)
            {
                // Diákok jelentkezéseinek törlése
                await _context.Applications
                    .Where(a => studentIds.Contains(a.UserId))
                    .ExecuteDeleteAsync();

                // Diákokhoz kapcsolódó felügyeletek törlése,
                // ha esetleg ilyen rekord is létezik
                await _context.Supervisions
                    .Where(s => studentIds.Contains(s.UserId))
                    .ExecuteDeleteAsync();

                // Diákfiókok törlése
                await _context.Users
                    .Where(u => u.Role == RoleChoice.Student)
                    .ExecuteDeleteAsync();
            }

            // Auditnapló
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminId,
                Action = "DeleteAllStudents",
                EntityName = "User",
                EntityId = 0,
                OldValue = $"Diákok száma: {studentCount}",
                NewValue = "Az összes diák törölve.",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return studentCount;
        }
    }
}