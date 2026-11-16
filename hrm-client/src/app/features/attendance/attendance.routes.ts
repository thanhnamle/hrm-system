import { Routes } from '@angular/router';
import { employeeRecordGuard, permissionGuard } from '../../core/auth/auth.guards';
import { Permissions } from '../../core/auth/permissions';

export default [
  {
    path: '',
    title: 'My attendance · HRM',
    canActivate: [permissionGuard(Permissions.AttendanceRecord), employeeRecordGuard],
    loadComponent: () => import('./my-attendance/my-attendance').then((m) => m.MyAttendance),
  },
  {
    path: 'team',
    title: 'Team attendance · HRM',
    canActivate: [permissionGuard(Permissions.AttendanceView)],
    loadComponent: () => import('./team-attendance/team-attendance').then((m) => m.TeamAttendance),
  },
] satisfies Routes;
