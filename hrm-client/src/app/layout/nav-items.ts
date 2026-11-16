import { Permission, Permissions } from '../core/auth/permissions';

/**
 * Sidebar menu, filtered by the signed-in user. An item without `permission` is for everyone;
 * `selfService` items also need an employee record (the system admin account has none).
 * Keep in sync with the guards in app.routes.ts and the feature route files.
 */
export interface NavItem {
  label: string;
  route: string;
  permission?: Permission;
  selfService?: boolean;
}

export const NAV_ITEMS: readonly NavItem[] = [
  { label: 'Dashboard', route: '/dashboard' },
  { label: 'Employees', route: '/employees', permission: Permissions.EmployeesView },
  { label: 'Departments', route: '/departments', permission: Permissions.DepartmentsView },
  { label: 'My leave', route: '/leave', permission: Permissions.LeaveRequest, selfService: true },
  { label: 'Approvals', route: '/leave/approvals', permission: Permissions.LeaveApprove },
  {
    label: 'My attendance',
    route: '/attendance',
    permission: Permissions.AttendanceRecord,
    selfService: true,
  },
  { label: 'Team attendance', route: '/attendance/team', permission: Permissions.AttendanceView },
  { label: 'Users', route: '/admin/users', permission: Permissions.UsersManage },
];
