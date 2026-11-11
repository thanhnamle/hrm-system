import { Routes } from '@angular/router';

export default [
  {
    path: '',
    title: 'My leave · HRM',
    loadComponent: () => import('./my-requests/my-requests').then((m) => m.MyRequests),
  },
  {
    path: 'approvals',
    title: 'Approvals · HRM',
    loadComponent: () => import('./approvals/approvals').then((m) => m.Approvals),
  },
] satisfies Routes;
