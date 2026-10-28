using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Dto.UserDto.Admin;
using ChoiceOfCareerEgressy.Dto.UserDto.Student;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model
{
    public class UserService
    {
        private readonly ChoiceOfCareerPersistence _context;
        private readonly PasswordService _passwordService;

        public UserService(ChoiceOfCareerPersistence context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        // Összes felhasználó lekérése
        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    EmailAddress = u.EmailAddress,
                    Role = u.Role.ToString(),
                    GradeName = u.Grade != null
                        ? u.Grade.GradeName : null,
                    BranchName = u.Branch != null
                        ? u.Branch.BranchName : null,
                    SexName = u.Sex != null
                        ? u.Sex.SexName : null
                })
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        // Egy osztály diákjainak lekérése
        public async Task<List<UserDto>> GetStudentsByGradeAsync(int gradeId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.GradeId == gradeId
                    && u.Role == RoleChoice.Student)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    EmailAddress = u.EmailAddress,
                    Role = u.Role.ToString(),
                    GradeName = u.Grade != null
                        ? u.Grade.GradeName : null,
                    BranchName = u.Branch != null
                        ? u.Branch.BranchName : null,
                    SexName = u.Sex != null
                        ? u.Sex.SexName : null
                })
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        // Egy diák részletes adatainak lekérése
        public async Task<StudentDetailsDto?> GetStudentByIdAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId
                    && u.Role == RoleChoice.Student)
                .Select(u => new StudentDetailsDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    EmailAddress = u.EmailAddress,

                    GradeId = u.GradeId,
                    GradeName = u.Grade != null
                        ? u.Grade.GradeName : null,

                    BranchName = u.Branch != null
                        ? u.Branch.BranchName : null,

                    SexName = u.Sex != null
                        ? u.Sex.SexName : null,

                    Applications = u.Applications
                        .Where(a => a.Status == ApplicationStatus.Active)
                        .Select(a => new ApplicationDto
                        {
                            ApplicationId = a.ApplicationId,
                            PlanSessionId = a.PlanSessionId,
                            TimeSlotId = a.PlanSession.TimeSlotId,
                            TimeSlotName =
                                a.PlanSession.TimeSlot.TimeSlotName,

                            PlanId = a.PlanSession.PlanId,
                            PlanName = a.PlanSession.Plan.PlanName,

                            LocationName =
                                a.PlanSession.Location.LocationName,

                            BeginDate =
                                a.PlanSession.TimeSlot.BeginDate,
                            EndDate =
                                a.PlanSession.TimeSlot.EndDate,

                            ApplicationDate = a.ApplicationDate,
                            UpdateDate = a.UpdateDate
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }


        // =====================================================
        // FELHASZNÁLÓ ADATAINAK LEKÉRDEZÉSE SZERKESZTÉSHEZ
        // =====================================================

        public async Task<AdminUserEditDto?> GetUserForEditAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new AdminUserEditDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    EmailAddress = u.EmailAddress,

                    SexId = u.SexId,
                    BranchId = u.BranchId,
                    GradeId = u.GradeId,

                    Role = u.Role.ToString()
                })
                .FirstOrDefaultAsync();
        }

        // =====================================================
        // OSZTÁLYOK, ÁGAZATOK ÉS NEMEK LEKÉRDEZÉSE
        // =====================================================

        public async Task<AdminUserOptionsDto> GetUserOptionsAsync()
        {
            var grades = await _context.Grades
                .AsNoTracking()
                .OrderBy(g => g.GradeName)
                .Select(g => new AdminUserOptionDto
                {
                    Id = g.GradeId,
                    Name = g.GradeName
                })
                .ToListAsync();

            var branches = await _context.Branches
                .AsNoTracking()
                .OrderBy(b => b.BranchName)
                .Select(b => new AdminUserOptionDto
                {
                    Id = b.BranchId,
                    Name = b.BranchName
                })
                .ToListAsync();

            var sexes = await _context.Sexes
                .AsNoTracking()
                .OrderBy(s => s.SexName)
                .Select(s => new AdminUserOptionDto
                {
                    Id = s.SexId,
                    Name = s.SexName
                })
                .ToListAsync();

            return new AdminUserOptionsDto
            {
                Grades = grades,
                Branches = branches,
                Sexes = sexes
            };
        }


        public async Task UpdateUserAsync(AdminUserEditDto dto, int? adminUserId = null)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.UserId);

            if (user == null)
                throw new InvalidOperationException(
                    "A felhasználó nem található.");

            string userName = dto.UserName?.Trim() ?? "";
            string email = dto.EmailAddress?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(userName))
                throw new InvalidOperationException(
                    "A felhasználó neve kötelező.");

            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException(
                    "Az e-mail-cím megadása kötelező.");

            // E-mail-cím egyediségének ellenőrzése.
            bool emailExists = await _context.Users
                .AnyAsync(u =>
                    u.UserId != dto.UserId &&
                    u.EmailAddress.ToLower() == email.ToLower());

            if (emailExists)
                throw new InvalidOperationException(
                    "Ezzel az e-mail-címmel már létezik felhasználó.");

            // Kapcsolódó adatok ellenőrzése.
            if (dto.SexId.HasValue &&
                !await _context.Sexes.AnyAsync(s =>
                    s.SexId == dto.SexId.Value))
            {
                throw new InvalidOperationException(
                    "A kiválasztott nem nem található.");
            }

            if (dto.BranchId.HasValue &&
                !await _context.Branches.AnyAsync(b =>
                    b.BranchId == dto.BranchId.Value))
            {
                throw new InvalidOperationException(
                    "A kiválasztott ágazat nem található.");
            }

            if (dto.GradeId.HasValue &&
                !await _context.Grades.AnyAsync(g =>
                    g.GradeId == dto.GradeId.Value))
            {
                throw new InvalidOperationException(
                    "A kiválasztott osztály nem található.");
            }

            // A diák már meglévő jelentkezéseinek ellenőrzése.
            // Nem engedjük, hogy a nem vagy ágazat módosítása
            // miatt a jelentkezései jogosulatlanná váljanak.
            if (user.Role == RoleChoice.Student &&
                (user.SexId != dto.SexId ||
                 user.BranchId != dto.BranchId))
            {
                var applications = await _context.Applications
                    .AsNoTracking()
                    .Include(a => a.PlanSession)
                        .ThenInclude(s => s.Plan)
                    .Where(a =>
                        a.UserId == user.UserId &&
                        a.Status == ApplicationStatus.Active)
                    .ToListAsync();

                var invalidPrograms = applications
                    .Where(a =>
                        (a.PlanSession.Plan.SexId.HasValue &&
                         a.PlanSession.Plan.SexId != dto.SexId) ||
                        (a.PlanSession.Plan.BranchId.HasValue &&
                         a.PlanSession.Plan.BranchId != dto.BranchId))
                    .Select(a => a.PlanSession.Plan.PlanName)
                    .Distinct()
                    .ToList();

                if (invalidPrograms.Count > 0)
                {
                    throw new InvalidOperationException(
                        "A módosítás miatt a diák már nem lenne " +
                        "jogosult a következő programokra: " +
                        string.Join(", ", invalidPrograms));
                }
            }

            var oldValue = new
            {
                user.UserName,
                user.EmailAddress,
                user.SexId,
                user.BranchId,
                user.GradeId
            };

            user.UserName = userName;
            user.EmailAddress = email;
            user.SexId = dto.SexId;
            user.BranchId = dto.BranchId;
            user.GradeId = dto.GradeId;

            bool passwordChanged =
                !string.IsNullOrWhiteSpace(dto.NewPassword);

            if (passwordChanged)
            {
                if (dto.NewPassword != dto.ConfirmPassword)
                    throw new InvalidOperationException(
                        "A két jelszó nem egyezik.");

                if (dto.NewPassword!.Length < 8)
                    throw new InvalidOperationException(
                        "Az új jelszó legalább 8 karakter legyen.");

                user.PasswordHash = _passwordService.HashPassword(
                    user,
                    dto.NewPassword);
            }

            // Naplózás – jelszót és jelszóhash-t nem tárolunk.
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminUserId,
                Action = "UPDATE",
                EntityName = "User",
                EntityId = user.UserId,
                OldValue = System.Text.Json.JsonSerializer.Serialize(
                    oldValue),
                NewValue = System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        user.UserName,
                        user.EmailAddress,
                        user.SexId,
                        user.BranchId,
                        user.GradeId,
                        PasswordChanged = passwordChanged
                    }),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }


        public async Task<int> CreateUserAsync(AdminUserCreateDto dto, int? adminUserId = null)
        {
            if (!dto.Role.HasValue ||
                !Enum.IsDefined(typeof(RoleChoice), dto.Role.Value))
            {
                throw new InvalidOperationException(
                    "Érvénytelen szerepkör.");
            }

            string name = dto.UserName?.Trim() ?? "";
            string email = dto.EmailAddress?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException(
                    "A név és az e-mail-cím kötelező.");
            }

            if (string.IsNullOrWhiteSpace(dto.Password) ||
                dto.Password.Length < 8 ||
                dto.Password != dto.ConfirmPassword)
            {
                throw new InvalidOperationException(
                    "A jelszó legalább 8 karakter legyen, " +
                    "és a két jelszónak egyeznie kell.");
            }

            bool emailExists = await _context.Users.AnyAsync(u =>
                u.EmailAddress.ToLower() == email.ToLower());

            if (emailExists)
            {
                throw new InvalidOperationException(
                    "Ezzel az e-mail-címmel már létezik felhasználó.");
            }

            if (dto.SexId.HasValue &&
                !await _context.Sexes.AnyAsync(s =>
                    s.SexId == dto.SexId.Value))
            {
                throw new InvalidOperationException(
                    "A kiválasztott nem nem található.");
            }

            if (dto.BranchId.HasValue &&
                !await _context.Branches.AnyAsync(b =>
                    b.BranchId == dto.BranchId.Value))
            {
                throw new InvalidOperationException(
                    "A kiválasztott ágazat nem található.");
            }

            if (dto.GradeId.HasValue &&
                !await _context.Grades.AnyAsync(g =>
                    g.GradeId == dto.GradeId.Value))
            {
                throw new InvalidOperationException(
                    "A kiválasztott osztály nem található.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var user = new User
            {
                UserName = name,
                EmailAddress = email,
                Role = dto.Role.Value,
                SexId = dto.SexId,
                BranchId = dto.BranchId,
                GradeId = dto.GradeId,
                MustChangePassword = dto.MustChangePassword,
                PasswordHash = ""
            };

            user.PasswordHash = _passwordService.HashPassword(
                user,
                dto.Password);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminUserId,
                Action = "CREATE",
                EntityName = "User",
                EntityId = user.UserId,
                NewValue = System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        user.UserName,
                        user.EmailAddress,
                        Role = user.Role.ToString(),
                        user.SexId,
                        user.BranchId,
                        user.GradeId,
                        user.MustChangePassword
                    }),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return user.UserId;
        }

        public async Task<AdminUserDeleteDto?> GetUserDeleteInfoAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new AdminUserDeleteDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    EmailAddress = u.EmailAddress,
                    Role = u.Role.ToString(),
                    ApplicationCount = u.Applications.Count(),
                    SupervisionCount = u.Supervisions.Count()
                })
                .FirstOrDefaultAsync();
        }


        public async Task DeleteUserAsync(int userId, int? adminUserId = null)
        {
            if (adminUserId == userId)
            {
                throw new InvalidOperationException(
                    "A saját adminisztrátori fiókodat nem törölheted.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var user = await _context.Users
                .Include(u => u.Applications)
                .Include(u => u.Supervisions)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new InvalidOperationException(
                    "A felhasználó nem található.");
            }

            var oldValue = new
            {
                user.UserId,
                user.UserName,
                user.EmailAddress,
                Role = user.Role.ToString(),
                user.SexId,
                user.BranchId,
                user.GradeId,
                ApplicationCount = user.Applications.Count,
                SupervisionCount = user.Supervisions.Count
            };

            _context.Applications.RemoveRange(user.Applications);
            _context.Supervisions.RemoveRange(user.Supervisions);

            await _context.SaveChangesAsync();

            // A korábbi naplóbejegyzések megmaradnak,
            // de az idegenkulcs-kapcsolatot megszüntetjük.
            var auditLogs = await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .ToListAsync();

            foreach (var log in auditLogs)
            {
                log.UserId = null;
            }

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminUserId,
                Action = "DELETE",
                EntityName = "User",
                EntityId = userId,
                OldValue = System.Text.Json.JsonSerializer.Serialize(oldValue),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        // =========================================
        // FELHASZNÁLÓK KERESÉSE ÉS SZŰRÉSE
        // =========================================

        public async Task<List<UserDto>> GetAllUsersAsync(string? search = null, int? gradeId = null, string? sort = null, RoleChoice? role = null, int? branchId = null, bool homeroomOnly = false)
        {
            var query = _context.Users
                .AsNoTracking()
                .AsQueryable();

            // =========================================
            // 1. KERESÉS NÉV VAGY E-MAIL ALAPJÁN
            // =========================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchText = search.Trim();

                query = query.Where(u =>
                    u.UserName.Contains(searchText) ||
                    u.EmailAddress.Contains(searchText));
            }

            // =========================================
            // 2. OSZTÁLY SZERINTI SZŰRÉS
            // =========================================

            if (gradeId.HasValue)
            {
                query = query.Where(u =>
                    u.GradeId == gradeId.Value);
            }

            // =========================================
            // 3. ÁGAZAT SZERINTI SZŰRÉS
            // =========================================

            if (branchId.HasValue)
            {
                query = query.Where(u =>
                    u.BranchId == branchId.Value);
            }

            // =========================================
            // 4. SZEREPKÖR SZERINTI SZŰRÉS
            // =========================================

            if (homeroomOnly)
            {
                // Csak osztályfőnökök

                query = query.Where(u =>
                    u.Role == RoleChoice.Teacher &&
                    u.GradeId != null);
            }
            else if (role.HasValue)
            {
                // Diák, tanár vagy admin

                query = query.Where(u =>
                    u.Role == role.Value);
            }

            // =========================================
            // 5. RENDEZÉS
            // =========================================

            query = sort switch
            {
                "grade_asc" => query
                    .OrderBy(u => u.Grade == null)
                    .ThenBy(u => u.Grade!.GradeName)
                    .ThenBy(u => u.UserName),

                "grade_desc" => query
                    .OrderBy(u => u.Grade == null)
                    .ThenByDescending(u => u.Grade!.GradeName)
                    .ThenBy(u => u.UserName),

                "name_desc" => query
                    .OrderByDescending(u => u.UserName),

                _ => query.OrderBy(u => u.UserName)
            };

            // =========================================
            // 6. FELHASZNÁLÓK LEKÉRDEZÉSE
            // =========================================

            return await query
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    EmailAddress = u.EmailAddress,
                    Role = u.Role.ToString(),

                    GradeName = u.Grade != null
                        ? u.Grade.GradeName : null,

                    BranchName = u.Branch != null
                        ? u.Branch.BranchName : null,

                    SexName = u.Sex != null
                        ? u.Sex.SexName : null
                })
                .ToListAsync();
        }


        // =========================================
        // FELHASZNÁLÓK SZÁMÁNAK LEKÉRDEZÉSE
        // =========================================

        public async Task<UserCountDto> GetUserCountsAsync()
        {
            var counts = await _context.Users
                .AsNoTracking()
                .GroupBy(u => u.Role)
                .Select(g => new
                {
                    Role = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return new UserCountDto
            {
                StudentCount = counts
                    .Where(x => x.Role == RoleChoice.Student)
                    .Sum(x => x.Count),

                TeacherCount = counts
                    .Where(x => x.Role == RoleChoice.Teacher)
                    .Sum(x => x.Count)
            };
        }

        // =========================================
        // OSZTÁLYFŐNÖKÖK LEKÉRDEZÉSE
        // =========================================

        public async Task<List<UserDto>> GetHomeroomTeachersAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.Role == RoleChoice.Teacher &&
                    u.GradeId != null)
                .OrderBy(u => u.UserName)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    EmailAddress = u.EmailAddress,
                    Role = u.Role.ToString(),

                    GradeName = u.Grade != null
                        ? u.Grade.GradeName : null,

                    BranchName = u.Branch != null
                        ? u.Branch.BranchName : null,

                    SexName = u.Sex != null
                        ? u.Sex.SexName : null
                })
                .ToListAsync();
        }
        // =========================================
        // FELHASZNÁLÓK LEKÉRDEZÉSE ÁGAZAT SZERINT
        // =========================================

        public async Task<List<UserDto>> GetUsersByBranchAsync(int branchId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.BranchId == branchId)
                .OrderBy(u => u.UserName)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    EmailAddress = u.EmailAddress,
                    Role = u.Role.ToString(),

                    GradeName = u.Grade != null
                        ? u.Grade.GradeName : null,

                    BranchName = u.Branch != null
                        ? u.Branch.BranchName : null,

                    SexName = u.Sex != null
                        ? u.Sex.SexName : null
                })
                .ToListAsync();
        }
    }
}
