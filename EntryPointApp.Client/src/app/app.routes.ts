import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { redirectGuard } from './core/guards/redirect.guard';
import { Login } from './features/auth/login/login';
import { Dashboard } from './shared/dashboard/dashboard';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'dashboard', component: Dashboard, canActivate: [authGuard] },
  { path: '', canActivate: [redirectGuard], children: [] },
  { path: '**', redirectTo: '' },
];
