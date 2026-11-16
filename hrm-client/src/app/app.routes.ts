import { Routes } from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './core/auth/auth.guards';
import { Permissions } from './core/auth/permissions';
import { Shell } from './layout/shell/shell';

/**
 * Every page is lazy-loaded: its code is downloaded only when first visited.
 * Guards: authGuard on the shell (every page inside needs a session), permissionGuard per area.
 * They only shape the UI; the API enforces the same rules on every request.
 */
export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in · HRM',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'Dashboard · HRM',
        loadComponent: () =>
          import('./features/dashboard/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'employees',
        canActivate: [permissionGuard(Permissions.EmployeesView)],
        loadChildren: () => import('./features/employees/employees.routes'),
      },
      {
        path: 'departments',
        canActivate: [permissionGuard(Permissions.DepartmentsView)],
        loadChildren: () => import('./features/departments/departments.routes'),
      },
      { path: 'leave', loadChildren: () => import('./features/leave/leave.routes') },
      { path: 'attendance', loadChildren: () => import('./features/attendance/attendance.routes') },
      {
        path: 'admin',
        canActivate: [permissionGuard(Permissions.UsersManage)],
        loadChildren: () => import('./features/admin/admin.routes'),
      },
      {
        path: 'forbidden',
        title: 'No access · HRM',
        loadComponent: () =>
          import('./features/errors/forbidden/forbidden').then((m) => m.Forbidden),
      },
    ],
  },
  {
    path: '**',
    title: 'Not found · HRM',
    loadComponent: () => import('./features/errors/not-found/not-found').then((m) => m.NotFound),
  },
];
