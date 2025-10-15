import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { redirectGuard } from './core/guards/redirect.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./shared/dashboard/dashboard').then((m) => m.Dashboard),
    canActivate: [authGuard],
  },
  {
    path: '',
    canActivate: [redirectGuard],
    children: [],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
