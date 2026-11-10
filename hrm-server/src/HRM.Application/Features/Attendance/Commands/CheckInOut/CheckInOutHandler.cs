using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using HRM.Domain.Common;
using HRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Application.Features.Attendance.Commands.CheckInOut;

public sealed class CheckInOutHandler(IApplicationDbContext db, ICurrentUser currentUser, IClock clock)
    : IRequestHandler<CheckInCommand, AttendanceRecordDto>, IRequestHandler<CheckOutCommand, AttendanceRecordDto>
{
    public async Task<AttendanceRecordDto> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var employeeId = currentUser.RequireEmployeeId();
        var today = clock.Today;

        // Friendly message for the common case; the unique index on (employee_id, work_date) covers races.
        if (await db.AttendanceRecords.AnyAsync(a => a.EmployeeId == employeeId && a.WorkDate == today, cancellationToken))
            throw new ConflictException("You have already checked in today.");

        var employee = await db.Employees.SingleOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), employeeId);

        var record = AttendanceRecord.CheckIn(employee, today, clock.UtcNow);
        db.AttendanceRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(record);
    }

    public async Task<AttendanceRecordDto> Handle(CheckOutCommand request, CancellationToken cancellationToken)
    {
        var employeeId = currentUser.RequireEmployeeId();
        var today = clock.Today;

        var record = await db.AttendanceRecords
                .SingleOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == today, cancellationToken)
            ?? throw new DomainException("You have not checked in today.");

        record.CheckOut(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(record);
    }

    private static AttendanceRecordDto ToDto(AttendanceRecord record) =>
        new(record.Id, record.WorkDate, record.CheckInAt, record.CheckOutAt);
}
