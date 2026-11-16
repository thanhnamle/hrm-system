import { Routes } from '@angular/router';
import { employeeRecordGuard, permissionGuard } from '../../core/auth/auth.guards';
import { Permissions } from '../../core/auth/permissions';

export default [
  {
    path: '',
    title: 'My leave · HRM',
    canActivate: [permissionGuard(Permissions.LeaveRequest), employeeRecordGuard],
    loadComponent: () => import('./my-requests/my-requests').then((m) => m.MyRequests),
  },
  {
    path: 'approvals',
    title: 'Approvals · HRM',
    canActivate: [permissionGuard(Permissions.LeaveApprove)],
    loadComponent: () => import('./approvals/approvals').then((m) => m.Approvals),
  },
] satisfies Routes;
