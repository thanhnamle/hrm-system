namespace HRM.Application.Common.Interfaces;

/// <summary>
/// The authenticated caller, read from the JWT claims of the current request.
/// All members are null / false for anonymous requests.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? EmployeeId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
