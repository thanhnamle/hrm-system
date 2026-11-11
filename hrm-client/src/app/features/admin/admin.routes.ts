import { Routes } from '@angular/router';

export default [
  { path: '', pathMatch: 'full', redirectTo: 'users' },
  {
    path: 'users',
    title: 'Users · HRM',
    loadComponent: () => import('./users/user-list/user-list').then((m) => m.UserList),
  },
] satisfies Routes;
