import { Permission, Permissions } from '../core/auth/permissions';

/**
 * Sidebar menu. An item without `permission` is shown to every signed-in user.
 * Filtering by the user's permissions is added with the AuthService (next part of Phase 6).
 */
export interface NavItem {
  label: string;
  route: string;
  permission?: Permission;
}

export const NAV_ITEMS: readonly NavItem[] = [
  { label: 'Dashboard', route: '/dashboard' },
  { label: 'Employees', route: '/employees', permission: Permissions.EmployeesView },
  { label: 'Departments', route: '/departments', permission: Permissions.DepartmentsView },
  { label: 'My leave', route: '/leave', permission: Permissions.LeaveRequest },
  { label: 'Approvals', route: '/leave/approvals', permission: Permissions.LeaveApprove },
  { label: 'My attendance', route: '/attendance', permission: Permissions.AttendanceRecord },
  { label: 'Team attendance', route: '/attendance/team', permission: Permissions.AttendanceView },
  { label: 'Users', route: '/admin/users', permission: Permissions.UsersManage },
];
