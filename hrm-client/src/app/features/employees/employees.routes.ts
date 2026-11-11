import { Routes } from '@angular/router';

export default [
  {
    path: '',
    title: 'Employees · HRM',
    loadComponent: () => import('./employee-list/employee-list').then((m) => m.EmployeeList),
  },
] satisfies Routes;
