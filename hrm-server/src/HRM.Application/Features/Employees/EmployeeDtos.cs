using HRM.Domain.Enums;

namespace HRM.Application.Features.Employees;

public sealed record EmployeeListItemDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string Position,
    Guid DepartmentId,
    string DepartmentName,
    EmployeeStatus Status,
    DateOnly HireDate);

public sealed record EmployeeDetailDto(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    DateOnly? DateOfBirth,
    DateOnly HireDate,
    string Position,
    Guid DepartmentId,
    string DepartmentName,
    bool IsDepartmentManager,
    EmployeeStatus Status,
    decimal AnnualLeaveBalance,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
