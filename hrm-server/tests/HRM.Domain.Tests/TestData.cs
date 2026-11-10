using HRM.Domain.Entities;

namespace HRM.Domain.Tests;

internal static class TestData
{
    /// <summary>Monday 2 Nov 2026 — used as "today" so working-day counts are predictable.</summary>
    public static readonly DateOnly Monday = new(2026, 11, 2);
    public static readonly DateOnly Friday = Monday.AddDays(4);

    public static readonly DateTimeOffset Now = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);

    public static Employee NewEmployee(string code = "emp-0001", string email = "An.Nguyen@HRM.local", Guid? departmentId = null) =>
        Employee.Hire(code, "An", "Nguyen", email, "Developer", departmentId ?? Guid.CreateVersion7(), new DateOnly(2024, 1, 15));
}
