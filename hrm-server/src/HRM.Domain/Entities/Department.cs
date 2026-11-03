using HRM.Domain.Common;
using HRM.Domain.Enums;

namespace HRM.Domain.Entities;

public class Department : AuditableEntity
{
    private readonly List<Employee> _employees = [];

    private Department() { } // EF Core

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>The employee who approves leave for this department. Optional.</summary>
    public Guid? ManagerId { get; private set; }
    public Employee? Manager { get; private set; }

    public IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();

    public static Department Create(string code, string name, string? description = null) => new()
    {
        Code = Guard.Required(code, "Department code").ToUpperInvariant(),
        Name = Guard.Required(name, "Department name"),
        Description = Guard.Optional(description),
    };

    public void Update(string name, string? description)
    {
        Name = Guard.Required(name, "Department name");
        Description = Guard.Optional(description);
    }

    public void AssignManager(Employee manager)
    {
        if (manager.Status != EmployeeStatus.Active)
            throw new DomainException("A terminated employee cannot manage a department.");

        ManagerId = manager.Id;
        Manager = manager;
    }

    public void RemoveManager()
    {
        ManagerId = null;
        Manager = null;
    }
}
