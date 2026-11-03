using HRM.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HRM.Infrastructure.Identity;

/// <summary>
/// Login account. Lives in Infrastructure so the Domain stays free of ASP.NET Identity.
/// A pure system admin may have no employee record.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public AppUser() => Id = Guid.CreateVersion7();

    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}

public class AppRole : IdentityRole<Guid>
{
    public AppRole() => Id = Guid.CreateVersion7();

    public AppRole(string roleName) : this() => Name = roleName;
}
