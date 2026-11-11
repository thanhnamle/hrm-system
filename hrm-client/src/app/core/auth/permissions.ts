/**
 * Same names as HRM.Application/Common/Security/Permissions.cs. The API sends the current user's
 * list from /api/auth/me; the UI only uses it to show or hide things (layer 0 in docs/rbac.md).
 * The API enforces every rule itself, so hiding something here is never a security measure.
 */
export const Permissions = {
  EmployeesView: 'Employees.View',
  EmployeesCreate: 'Employees.Create',
  EmployeesUpdate: 'Employees.Update',
  EmployeesTerminate: 'Employees.Terminate',
  DepartmentsView: 'Departments.View',
  DepartmentsManage: 'Departments.Manage',
  LeaveRequest: 'Leave.Request',
  LeaveApprove: 'Leave.Approve',
  LeaveViewAll: 'Leave.ViewAll',
  AttendanceRecord: 'Attendance.Record',
  AttendanceView: 'Attendance.View',
  UsersManage: 'Users.Manage',
} as const;

export type Permission = (typeof Permissions)[keyof typeof Permissions];

export type Role = 'Admin' | 'HR' | 'Manager' | 'Employee';
