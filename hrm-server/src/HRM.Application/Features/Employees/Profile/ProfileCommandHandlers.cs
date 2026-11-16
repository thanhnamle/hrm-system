using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Domain.Common;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Employees.Profile;

/// <summary>
/// Editing the CV part of a profile (HR and Admin, via Employees.Update). The rules live in the
/// Employee aggregate and EmployeeDocument; these handlers load, call, save.
/// </summary>
public sealed class ProfileCommandHandlers(IApplicationDbContext db) :
    IRequestHandler<UpdateSkillsCommand>,
    IRequestHandler<AddQualificationCommand, Guid>,
    IRequestHandler<UpdateQualificationCommand>,
    IRequestHandler<DeleteQualificationCommand>,
    IRequestHandler<UploadCvCommand, CvInfoDto>,
    IRequestHandler<DeleteCvCommand>
{
    public async Task Handle(UpdateSkillsCommand request, CancellationToken cancellationToken)
    {
        var employee = await FindAsync(request.EmployeeId, withQualifications: false, cancellationToken);
        employee.SetSkills(request.Skills);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> Handle(AddQualificationCommand request, CancellationToken cancellationToken)
    {
        var employee = await FindAsync(request.EmployeeId, withQualifications: true, cancellationToken);
        var qualification = employee.AddQualification(
            request.Type, request.Title, request.Organization, request.StartDate, request.EndDate, request.Description);
        await db.SaveChangesAsync(cancellationToken);
        return qualification.Id;
    }

    public async Task Handle(UpdateQualificationCommand request, CancellationToken cancellationToken)
    {
        var employee = await FindWithQualificationAsync(request.EmployeeId, request.QualificationId, cancellationToken);
        employee.UpdateQualification(
            request.QualificationId, request.Type, request.Title, request.Organization,
            request.StartDate, request.EndDate, request.Description);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(DeleteQualificationCommand request, CancellationToken cancellationToken)
    {
        var employee = await FindWithQualificationAsync(request.EmployeeId, request.QualificationId, cancellationToken);
        employee.RemoveQualification(request.QualificationId);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CvInfoDto> Handle(UploadCvCommand request, CancellationToken cancellationToken)
    {
        var employee = await FindAsync(request.EmployeeId, withQualifications: false, cancellationToken);
        if (!employee.IsActive)
            throw new DomainException("Employee is terminated.");

        var document = await db.EmployeeDocuments.SingleOrDefaultAsync(d => d.EmployeeId == employee.Id, cancellationToken);
        if (document is null)
        {
            document = EmployeeDocument.CreateCv(employee.Id, request.FileName, request.Content);
            db.EmployeeDocuments.Add(document);
        }
        else
        {
            document.Replace(request.FileName, request.Content); // same checks: PDF bytes, size
        }

        await db.SaveChangesAsync(cancellationToken);
        return new CvInfoDto(document.FileName, document.SizeBytes, document.UpdatedAt ?? document.CreatedAt);
    }

    public async Task Handle(DeleteCvCommand request, CancellationToken cancellationToken)
    {
        var document = await db.EmployeeDocuments.SingleOrDefaultAsync(d => d.EmployeeId == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("CV", request.EmployeeId);
        db.EmployeeDocuments.Remove(document);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Employee> FindAsync(Guid id, bool withQualifications, CancellationToken cancellationToken)
    {
        var query = db.Employees.AsQueryable();
        if (withQualifications)
            query = query.Include(e => e.Qualifications);

        return await query.SingleOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), id);
    }

    /// <summary>A qualification of another employee is "not found", not a business-rule error.</summary>
    private async Task<Employee> FindWithQualificationAsync(Guid employeeId, Guid qualificationId, CancellationToken cancellationToken)
    {
        var employee = await FindAsync(employeeId, withQualifications: true, cancellationToken);
        if (employee.Qualifications.All(q => q.Id != qualificationId))
            throw new NotFoundException(nameof(Qualification), qualificationId);
        return employee;
    }
}
