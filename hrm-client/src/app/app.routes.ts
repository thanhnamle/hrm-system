import { Routes } from '@angular/router';
import { Shell } from './layout/shell/shell';

/**
 * Every page is lazy-loaded: its code is downloaded only when first visited.
 * authGuard and permissionGuard are added to these routes with the AuthService (next part of Phase 6).
 */
export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in · HRM',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: '',
    component: Shell,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'Dashboard · HRM',
        loadComponent: () =>
          import('./features/dashboard/dashboard/dashboard').then((m) => m.Dashboard),
      },
      { path: 'employees', loadChildren: () => import('./features/employees/employees.routes') },
      {
        path: 'departments',
        loadChildren: () => import('./features/departments/departments.routes'),
      },
      { path: 'leave', loadChildren: () => import('./features/leave/leave.routes') },
      { path: 'attendance', loadChildren: () => import('./features/attendance/attendance.routes') },
      { path: 'admin', loadChildren: () => import('./features/admin/admin.routes') },
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
