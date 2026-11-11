import { Routes } from '@angular/router';

export default [
  {
    path: '',
    title: 'My attendance · HRM',
    loadComponent: () => import('./my-attendance/my-attendance').then((m) => m.MyAttendance),
  },
  {
    path: 'team',
    title: 'Team attendance · HRM',
    loadComponent: () => import('./team-attendance/team-attendance').then((m) => m.TeamAttendance),
  },
] satisfies Routes;
