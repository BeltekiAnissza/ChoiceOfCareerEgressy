using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Dto.UserDto.Student;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Model
{
    public class UserService
    {
        private readonly ChoiceOfCareerPersistence _context;

        public UserService(ChoiceOfCareerPersistence context)
        {
            _context = context;
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
    }
}
