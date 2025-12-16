import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { redirectGuard } from './core/guards/redirect.guard';
import { Login } from './features/auth/login/login';
import { Dashboard } from './features/dashboard/dashboard';
import { Dailylogs } from './features/dailylogs/dailylogs';
import { Register } from './features/auth/register/register';
import { ForgotPassword } from './features/auth/forgot-password/forgot-password';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'forgot-password', component: ForgotPassword },
  { path: 'register', component: Register },
  { path: 'dashboard', component: Dashboard, canActivate: [authGuard] },
  {
    path: 'dashboard/week/:id',
    component: Dailylogs,
    canActivate: [authGuard],
  },
  { path: '', canActivate: [redirectGuard], children: [] },
  { path: '**', redirectTo: '' },
];
