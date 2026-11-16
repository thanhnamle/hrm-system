import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/auth.guards';
import { Permissions } from '../../core/auth/permissions';

// Employees.View is already checked by the parent route in app.routes.ts.
export default [
  {
    path: '',
    title: 'Employees · HRM',
    loadComponent: () => import('./employee-list/employee-list').then((m) => m.EmployeeList),
  },
  {
    path: 'new',
    title: 'Add employee · HRM',
    canActivate: [permissionGuard(Permissions.EmployeesCreate)],
    loadComponent: () => import('./employee-form/employee-form').then((m) => m.EmployeeForm),
  },
  {
    path: ':id',
    title: 'Employee · HRM',
    loadComponent: () => import('./employee-view/employee-view').then((m) => m.EmployeeView),
  },
  {
    path: ':id/edit',
    title: 'Edit employee · HRM',
    canActivate: [permissionGuard(Permissions.EmployeesUpdate)],
    loadComponent: () => import('./employee-form/employee-form').then((m) => m.EmployeeForm),
  },
] satisfies Routes;
