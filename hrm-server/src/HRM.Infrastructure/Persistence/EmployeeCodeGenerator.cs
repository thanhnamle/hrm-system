using HRM.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Infrastructure.Persistence;

/// <summary>
/// Employee codes from a PostgreSQL sequence. nextval() never gives two callers the same number,
/// so simultaneous creates can't collide; the old "highest code + 1" read could, and it also broke
/// at 10,000 because it compared codes as text ("EMP-9999" sorts after "EMP-10000").
/// </summary>
public sealed class EmployeeCodeGenerator(AppDbContext db) : IEmployeeCodeGenerator
{
    public const string SequenceName = "employee_code_seq";
    private const string Prefix = "EMP-";

    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        var next = await db.Database
            .SqlQuery<long>($"SELECT nextval('employee_code_seq') AS \"Value\"")
            .SingleAsync(cancellationToken);
        return Format(next);
    }

    /// <summary>At least four digits (EMP-0042); larger numbers just get longer (EMP-12345).</summary>
    public static string Format(long number) => $"{Prefix}{number:0000}";
}
