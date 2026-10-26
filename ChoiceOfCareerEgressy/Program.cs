using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Model;
using ChoiceOfCareerEgressy.Model.Admin;
using ChoiceOfCareerEgressy.Model.Admin.Imports;
using ChoiceOfCareerEgressy.Model.Admin.Programs;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Teacher");
    options.Conventions.AuthorizeFolder("/Student");
    options.Conventions.AuthorizeFolder("/Homeroom");
});

builder.Services.AddControllers();


var databasePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "Database",
    "choiceofcareer.db"
);

// Database mappa létrehozása, ha még nem létezik
Directory.CreateDirectory(
    Path.GetDirectoryName(databasePath)!
);

// SQLite adatbázis kapcsolat
builder.Services.AddDbContext<ChoiceOfCareerPersistence>(options =>
    options.UseSqlite($"Data Source={databasePath}")
);

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StudentImportService>();
builder.Services.AddScoped<TeacherImportService>();
builder.Services.AddScoped<ProgramImportService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<StudentRegistrationService>();
builder.Services.AddScoped<TeacherSupervisionService>();
builder.Services.AddScoped<HomeroomService>();
builder.Services.AddScoped<AdminRegistrationStudentService>();
builder.Services.AddScoped<AdminRegistrationTeacherService>();
builder.Services.AddScoped<AdminProgramService>();
builder.Services.AddScoped<GradeManagementService>();
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddScoped<SchoolYearService>();
builder.Services.AddScoped<AdminAllDataDeleteService>();

builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme
)
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<ChoiceOfCareerPersistence>();

    var passwordService = scope.ServiceProvider
        .GetRequiredService<PasswordService>();

    // Adatbázis létrehozása és migrációk alkalmazása
    await context.Database.MigrateAsync();

    // Első admin létrehozása, ha még nincs
    await AdminSeeder.SeedAsync(
        context,
        passwordService,
        app.Configuration
    );
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();

app.UseRouting();

// Bejelentkezett felhasználó azonosítása
app.UseAuthentication();

// Jogosultságok ellenőrzése
app.UseAuthorization();

app.MapControllers();

app.MapStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();