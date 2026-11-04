namespace HRM.Application.Common.Security;

/// <summary>
/// JWT claim names. Inbound claim mapping is turned off, so the API reads these names back unchanged.
/// </summary>
public static class HrmClaimTypes
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string EmployeeId = "employee_id";
}
