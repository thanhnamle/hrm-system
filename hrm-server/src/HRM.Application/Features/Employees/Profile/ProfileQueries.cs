using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Employees.Profile;

public sealed record GetEmployeeProfileQuery(Guid EmployeeId) : IRequest<EmployeeProfileDto>;

public sealed record GetCvFileQuery(Guid EmployeeId) : IRequest<CvFileDto>;

/// <summary>
/// Same data scope as the employee record itself: Admin/HR everyone, a Manager their departments,
/// an Employee only themselves. Outside the scope is "not found".
/// </summary>
public sealed class ProfileQueryHandlers(IApplicationDbContext db, ICurrentUser currentUser, IIdentityService identityService) :
    IRequestHandler<GetEmployeeProfileQuery, EmployeeProfileDto>,
    IRequestHandler<GetCvFileQuery, CvFileDto>
{
    public async Task<EmployeeProfileDto> Handle(GetEmployeeProfileQuery request, CancellationToken cancellationToken)
    {
        var id = request.EmployeeId;
        var skills = await db.Employees.AsNoTracking()
                .ApplyScope(currentUser)
                .Where(e => e.Id == id)
                .Select(e => e.Skills)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), id);

        var qualifications = await db.Qualifications.AsNoTracking()
            .Where(q => q.EmployeeId == id)
            .OrderByDescending(q => q.StartDate)
            .Select(q => new QualificationDto(q.Id, q.Type, q.Title, q.Organization, q.StartDate, q.EndDate, q.Description))
            .ToListAsync(cancellationToken);

        // Guid v7 ids are time-ordered, so they break ties between changes on the same day.
        var history = await (
                from h in db.JobHistory.AsNoTracking()
                join d in db.Departments on h.DepartmentId equals d.Id
                where h.EmployeeId == id
                orderby h.EffectiveDate, h.Id
                select new JobHistoryDto(h.Id, h.ChangeType, h.EffectiveDate, h.Position, h.DepartmentId, d.Name, h.Note))
            .ToListAsync(cancellationToken);

        // Metadata only: the file content is only read by the download query.
        var cv = await db.EmployeeDocuments.AsNoTracking()
            .Where(d => d.EmployeeId == id)
            .Select(d => new CvInfoDto(d.FileName, d.SizeBytes, d.UpdatedAt ?? d.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        var account = await identityService.GetAccountByEmployeeIdAsync(id, cancellationToken);

        return new EmployeeProfileDto(
            id,
            account is null ? null : new AccountDto(account.Role, account.IsLockedOut),
            skills,
            qualifications,
            history,
            cv);
    }

    public async Task<CvFileDto> Handle(GetCvFileQuery request, CancellationToken cancellationToken)
    {
        var id = request.EmployeeId;
        if (!await db.Employees.AsNoTracking().ApplyScope(currentUser).AnyAsync(e => e.Id == id, cancellationToken))
            throw new NotFoundException(nameof(Employee), id);

        return await db.EmployeeDocuments.AsNoTracking()
                .Where(d => d.EmployeeId == id)
                .Select(d => new CvFileDto(d.FileName, d.ContentType, d.Content))
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("CV", id);
    }
}
