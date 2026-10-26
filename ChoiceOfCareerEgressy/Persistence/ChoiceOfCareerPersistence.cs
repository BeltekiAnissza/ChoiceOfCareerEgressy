using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace ChoiceOfCareerEgressy.Persistence
{


    public class ChoiceOfCareerPersistence : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Grade> Grades => Set<Grade>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Sex> Sexes => Set<Sex>();
        public DbSet<Location> Locations => Set<Location>();

        public DbSet<Plan> Plans => Set<Plan>();
        public DbSet<PlanSession> PlanSessions => Set<PlanSession>();
        public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();

        public DbSet<Application> Applications => Set<Application>();
        public DbSet<Supervision> Supervisions => Set<Supervision>();
        public DbSet<SupervisionPlace> SupervisionPlaces => Set<SupervisionPlace>();

        public DbSet<Event> Events => Set<Event>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<SchoolYearTransition> SchoolYearTransitions => Set<SchoolYearTransition>();

        public ChoiceOfCareerPersistence(
            DbContextOptions<ChoiceOfCareerPersistence> options
        ) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // USER - GRADE
            modelBuilder.Entity<User>()
                .HasOne(u => u.Grade)
                .WithMany(g => g.Users)
                .HasForeignKey(u => u.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            // GRADE - HOMEROOM TEACHER
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.HomeroomTeacher)
                .WithMany()
                .HasForeignKey(g => g.HomeroomTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // USER - BRANCH
            modelBuilder.Entity<User>()
                .HasOne(u => u.Branch)
                .WithMany(b => b.Users)
                .HasForeignKey(u => u.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // USER - SEX
            modelBuilder.Entity<User>()
                .HasOne(u => u.Sex)
                .WithMany(s => s.Users)
                .HasForeignKey(u => u.SexId)
                .OnDelete(DeleteBehavior.Restrict);

            // PLAN - BRANCH
            modelBuilder.Entity<Plan>()
                .HasOne(p => p.Branch)
                .WithMany(b => b.Plans)
                .HasForeignKey(p => p.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // PLAN - SEX
            modelBuilder.Entity<Plan>()
                .HasOne(p => p.Sex)
                .WithMany(s => s.Plans)
                .HasForeignKey(p => p.SexId)
                .OnDelete(DeleteBehavior.Restrict);

            // ONE PROGRAM PER TIME SLOT
            modelBuilder.Entity<PlanSession>()
                .HasIndex(p => new { p.PlanId, p.TimeSlotId })
                .IsUnique();

            // ONE PROGRAM PER ROOM AND TIME SLOT
            modelBuilder.Entity<PlanSession>()
                .HasIndex(p => new { p.LocationId, p.TimeSlotId })
                .IsUnique();

            // PLAN SESSION - LOCATION
            modelBuilder.Entity<PlanSession>()
                .HasOne(p => p.Location)
                .WithMany(l => l.PlanSessions)
                .HasForeignKey(p => p.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // APPLICATION UNIQUE INDEX
            modelBuilder.Entity<Application>()
                .HasIndex(a => new { a.UserId, a.PlanSessionId })
                .IsUnique();

            // APPLICATION - USER
            modelBuilder.Entity<Application>()
                .HasOne(a => a.User)
                .WithMany(u => u.Applications)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // APPLICATION - PLAN SESSION
            modelBuilder.Entity<Application>()
                .HasOne(a => a.PlanSession)
                .WithMany(p => p.Applications)
                .HasForeignKey(a => a.PlanSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            // SUPERVISION - USER
            modelBuilder.Entity<Supervision>()
                .HasOne(s => s.User)
                .WithMany(u => u.Supervisions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // SUPERVISION - PLAN SESSION
            modelBuilder.Entity<Supervision>()
                .HasOne(s => s.PlanSession)
                .WithMany(p => p.Supervisions)
                .HasForeignKey(s => s.PlanSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            // SUPERVISION - SUPERVISION PLACE
            modelBuilder.Entity<Supervision>()
                .HasOne(s => s.SupervisionPlace)
                .WithMany(p => p.Supervisions)
                .HasForeignKey(s => s.SupervisionPlaceId)
                .OnDelete(DeleteBehavior.Restrict);

            // SUPERVISION MUST HAVE EXACTLY ONE TARGET
            modelBuilder.Entity<Supervision>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Supervision_ExactlyOneTarget",
                    "(PlanSessionId IS NOT NULL AND SupervisionPlaceId IS NULL) " +
                    "OR (PlanSessionId IS NULL AND SupervisionPlaceId IS NOT NULL)"
                ));

            // UNIQUE SUPERVISION PLACE PER TIME SLOT
            modelBuilder.Entity<SupervisionPlace>()
                .HasIndex(p => new { p.Name, p.TimeSlotId })
                .IsUnique();
        }
    }

    // ======================================================
    // USER
    // ======================================================

    [Index(nameof(EmailAddress), IsUnique = true)]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [MaxLength(150)]
        public required string UserName { get; set; }

        [MaxLength(255)]
        public required string EmailAddress { get; set; }

        public RoleChoice Role { get; set; }

        public required string PasswordHash { get; set; }

        public bool MustChangePassword { get; set; }

        public int? GradeId { get; set; }
        public Grade? Grade { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int? SexId { get; set; }
        public Sex? Sex { get; set; }

        public List<Application> Applications { get; set; } = new();

        public List<Supervision> Supervisions { get; set; } = new();
    }

    public enum RoleChoice
    {
        Student,
        Teacher,
        Admin
    }

    // ======================================================
    // GRADE
    // ======================================================

    [Index(nameof(GradeName), IsUnique = true)]
    public class Grade
    {
        [Key]
        public int GradeId { get; set; }

        public required string GradeName { get; set; }

        public int? HomeroomTeacherId { get; set; }
        public User? HomeroomTeacher { get; set; }

        public List<User> Users { get; set; } = new();
    }

    // ======================================================
    // BRANCH
    // ======================================================

    [Index(nameof(BranchName), IsUnique = true)]
    public class Branch
    {
        [Key]
        public int BranchId { get; set; }

        public required string BranchName { get; set; }

        public List<User> Users { get; set; } = new();

        public List<Plan> Plans { get; set; } = new();
    }

    // ======================================================
    // SEX
    // ======================================================

    [Index(nameof(SexName), IsUnique = true)]
    public class Sex
    {
        [Key]
        public int SexId { get; set; }

        public required string SexName { get; set; }

        public List<User> Users { get; set; } = new();

        public List<Plan> Plans { get; set; } = new();
    }

    // ======================================================
    // LOCATION
    // ======================================================

    [Index(nameof(LocationName), IsUnique = true)]
    public class Location
    {
        [Key]
        public int LocationId { get; set; }

        public required string LocationName { get; set; }

        public int? Capacity { get; set; }

        public List<PlanSession> PlanSessions { get; set; } = new();
    }

    // ======================================================
    // TIME SLOT
    // ======================================================

    public class TimeSlot
    {
        [Key]
        public int TimeSlotId { get; set; }

        public required string TimeSlotName { get; set; }

        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        public List<PlanSession> PlanSessions { get; set; } = new();

        public List<SupervisionPlace> SupervisionPlaces { get; set; } = new();
    }

    // ======================================================
    // PLAN
    // ======================================================

    public class Plan
    {
        [Key]
        public int PlanId { get; set; }

        public required string PlanName { get; set; }

        public required string PlanDescription { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int? SexId { get; set; }
        public Sex? Sex { get; set; }

        public List<PlanSession> Sessions { get; set; } = new();
    }

    // ======================================================
    // PLAN SESSION
    // ======================================================

    public class PlanSession
    {
        [Key]
        public int PlanSessionId { get; set; }

        public int PlanId { get; set; }
        public Plan Plan { get; set; } = null!;

        public int TimeSlotId { get; set; }
        public TimeSlot TimeSlot { get; set; } = null!;

        public int LocationId { get; set; }
        public Location Location { get; set; } = null!;

        public int Capacity { get; set; }

        public int RequiredSupervisors { get; set; } = 1;

        public List<Application> Applications { get; set; } = new();

        public List<Supervision> Supervisions { get; set; } = new();
    }

    // ======================================================
    // APPLICATION
    // ======================================================

    public class Application
    {
        [Key]
        public int ApplicationId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int PlanSessionId { get; set; }
        public PlanSession PlanSession { get; set; } = null!;

        public DateTimeOffset ApplicationDate { get; set; }
            = DateTime.UtcNow;

        public DateTimeOffset? UpdateDate { get; set; }

        public ApplicationStatus Status { get; set; }
            = ApplicationStatus.Active;
    }

    public enum ApplicationStatus
    {
        Active,
        Cancelled
    }

    // ======================================================
    // SUPERVISION
    // ======================================================

    public class Supervision
    {
        [Key]
        public int SupervisionId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? PlanSessionId { get; set; }
        public PlanSession? PlanSession { get; set; }

        public int? SupervisionPlaceId { get; set; }
        public SupervisionPlace? SupervisionPlace { get; set; }

        public DateTimeOffset ApplicationDate { get; set; }
            = DateTime.UtcNow;

        public DateTimeOffset? UpdateDate { get; set; }
    }

    // ======================================================
    // SUPERVISION PLACE
    // ======================================================

    public class SupervisionPlace
    {
        [Key]
        public int SupervisionPlaceId { get; set; }

        public required string Name { get; set; }

        public int TimeSlotId { get; set; }
        public TimeSlot TimeSlot { get; set; } = null!;

        public int RequiredSupervisors { get; set; } = 1;

        public List<Supervision> Supervisions { get; set; } = new();
    }

    // ======================================================
    // SETTINGS
    // ======================================================

    public class Event
    {
        [Key]
        public int EventId { get; set; }

        public required string EventName { get; set; }

        public DateTimeOffset EventDate { get; set; }

        public DateTimeOffset RegistrationStart { get; set; }
        public DateTimeOffset RegistrationEnd { get; set; }

        public bool IsRegistrationEnabled { get; set; }

        public List<TimeSlot> TimeSlots { get; set; } = new();
    }

    // ======================================================
    // AUDIT LOG
    // ======================================================

    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        public int? UserId { get; set; }

        public required string Action { get; set; }

        public required string EntityName { get; set; }

        public int EntityId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }

    [Index(nameof(CalendarYear), IsUnique = true)]
    public class SchoolYearTransition
    {
        [Key]
        public int SchoolYearTransitionId { get; set; }

        // A naptári év, amelyben megtörtént a léptetés
        public int CalendarYear { get; set; }

        // A léptetés pontos időpontja (UTC)
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        // A műveletet végrehajtó adminisztrátor azonosítója
        public int? ExecutedByUserId { get; set; }
    }

}