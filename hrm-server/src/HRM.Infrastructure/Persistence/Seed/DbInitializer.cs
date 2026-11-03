using HRM.Application.Common.Security;
using HRM.Domain.Entities;
using HRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HRM.Infrastructure.Persistence.Seed;

/// <summary>
/// Applies migrations and inserts demo data. Called at startup in Development only.
/// Safe to run repeatedly: roles are created if missing, demo data only if there are no departments yet.
/// </summary>
public class DbInitializer(
    AppDbContext db,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<DbInitializer> logger)
{
    public const string AdminEmail = "admin@hrm.local";

    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        db.Database.MigrateAsync(cancellationToken);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();

        if (await db.Departments.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Seed data already present, skipping");
            return;
        }

        var password = configuration["Seed:DefaultPassword"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "Seed:DefaultPassword is not set. Run: dotnet user-secrets set \"Seed:DefaultPassword\" \"<password>\" --project src/HRM.API");

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var hr = Department.Create("HR", "Human Resources", "People operations");
        var engineering = Department.Create("ENG", "Engineering", "Product development");
        var sales = Department.Create("SAL", "Sales", "Customers and revenue");
        db.Departments.AddRange(hr, engineering, sales);

        (string First, string Last, string Position, Department Department, string Role, int MonthsEmployed)[] people =
        [
            ("Linh", "Tran", "HR Manager", hr, Roles.HR, 48),
            ("Mai", "Pham", "HR Specialist", hr, Roles.HR, 20),
            ("Minh", "Nguyen", "Engineering Manager", engineering, Roles.Manager, 60),
            ("Huy", "Le", "Senior Developer", engineering, Roles.Employee, 36),
            ("Thao", "Vo", "Developer", engineering, Roles.Employee, 14),
            ("Khoa", "Dang", "QA Engineer", engineering, Roles.Employee, 9),
            ("Tuan", "Phan", "Developer", engineering, Roles.Employee, 3),
            ("Lan", "Bui", "Sales Manager", sales, Roles.Manager, 40),
            ("Quang", "Do", "Account Executive", sales, Roles.Employee, 18),
            ("Ngoc", "Hoang", "Sales Associate", sales, Roles.Employee, 6),
        ];

        var employees = people
            .Select((p, i) => (
                Employee: Employee.Hire(
                    $"EMP-{i + 1:0000}", p.First, p.Last, $"{p.First}.{p.Last}@hrm.local",
                    p.Position, p.Department.Id, today.AddMonths(-p.MonthsEmployed)),
                p.Role))
            .ToList();
        db.Employees.AddRange(employees.Select(e => e.Employee));
        await db.SaveChangesAsync(cancellationToken);

        // departments.manager_id and employees.department_id reference each other, so managers are set in a second save.
        hr.AssignManager(employees[0].Employee);
        engineering.AssignManager(employees[2].Employee);
        sales.AssignManager(employees[7].Employee);
        await db.SaveChangesAsync(cancellationToken);

        await CreateUserAsync(AdminEmail, employeeId: null, Roles.Admin, password);
        foreach (var (employee, role) in employees)
            await CreateUserAsync(employee.Email, employee.Id, role, password);

        logger.LogInformation("Seeded {Departments} departments, {Employees} employees and {Users} user accounts",
            3, employees.Count, employees.Count + 1);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                EnsureSucceeded(await roleManager.CreateAsync(new AppRole(role)), $"role {role}");
        }
    }

    private async Task CreateUserAsync(string email, Guid? employeeId, string role, string password)
    {
        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, EmployeeId = employeeId };
        EnsureSucceeded(await userManager.CreateAsync(user, password), email);
        EnsureSucceeded(await userManager.AddToRoleAsync(user, role), $"{email} → {role}");
    }

    private static void EnsureSucceeded(IdentityResult result, string what)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Seeding {what} failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}
