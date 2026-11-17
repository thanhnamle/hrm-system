import { Permission, Permissions } from '../core/auth/permissions';
import { IconName } from '../shared/ui/icon/icon';

/**
 * Sidebar menu, filtered by the signed-in user. An item without `permission` is for everyone;
 * `selfService` items also need an employee record (the system admin account has none).
 * Keep in sync with the guards in app.routes.ts and the feature route files.
 */
export interface NavItem {
  label: string;
  route: string;
  icon: IconName;
  permission?: Permission;
  selfService?: boolean;
  /**
   * Highlight only on this exact URL. Needed where another item's route starts with this one
   * (/leave vs /leave/approvals); otherwise sub-pages (/employees/42) keep their item active.
   */
  exact?: boolean;
  /** Shows the number of leave requests waiting for this user's review. */
  badge?: 'pendingApprovals';
}

export interface NavGroup {
  label: string;
  items: readonly NavItem[];
}

export const NAV_GROUPS: readonly NavGroup[] = [
  {
    label: 'Overview',
    items: [{ label: 'Dashboard', route: '/dashboard', icon: 'grid', exact: true }],
  },
  {
    label: 'People',
    items: [
      {
        label: 'Employees',
        route: '/employees',
        icon: 'users',
        permission: Permissions.EmployeesView,
      },
      {
        label: 'Departments',
        route: '/departments',
        icon: 'building',
        permission: Permissions.DepartmentsView,
      },
    ],
  },
  {
    label: 'Time off',
    items: [
      {
        label: 'My leave',
        route: '/leave',
        icon: 'calendar',
        permission: Permissions.LeaveRequest,
        selfService: true,
        exact: true,
      },
      {
        label: 'Approvals',
        route: '/leave/approvals',
        icon: 'approvals',
        permission: Permissions.LeaveApprove,
        badge: 'pendingApprovals',
      },
    ],
  },
  {
    label: 'Attendance',
    items: [
      {
        label: 'My attendance',
        route: '/attendance',
        icon: 'clock',
        permission: Permissions.AttendanceRecord,
        selfService: true,
        exact: true,
      },
      {
        label: 'Team attendance',
        route: '/attendance/team',
        icon: 'userCheck',
        permission: Permissions.AttendanceView,
      },
    ],
  },
  {
    label: 'Administration',
    items: [
      { label: 'Users', route: '/admin/users', icon: 'key', permission: Permissions.UsersManage },
    ],
  },
];

/** Every item, in menu order. */
export const NAV_ITEMS: readonly NavItem[] = NAV_GROUPS.flatMap((g) => g.items);
