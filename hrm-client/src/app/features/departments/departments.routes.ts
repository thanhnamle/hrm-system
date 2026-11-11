import { Routes } from '@angular/router';

export default [
  {
    path: '',
    title: 'Departments · HRM',
    loadComponent: () => import('./department-list/department-list').then((m) => m.DepartmentList),
  },
] satisfies Routes;
