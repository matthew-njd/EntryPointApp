import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { redirectGuard } from './core/guards/redirect.guard';
import { Login } from './features/auth/login/login';
import { Dashboard } from './features/dashboard/dashboard';
import { Dailylogs } from './features/dailylogs/dailylogs';
import { Register } from './features/auth/register/register';
import { ForgotPassword } from './features/auth/forgot-password/forgot-password';
import { ResetPassword } from './features/auth/reset-password/reset-password';
import { CreateTimesheet } from './features/create-timesheet/create-timesheet';
import { EditTimesheet } from './features/edit-timesheet/edit-timesheet';
import { Admin } from './features/admin/admin';
import { adminGuard } from './core/guards/admin.guard';
import { UserEdit } from './features/user-edit/user-edit';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'forgot-password', component: ForgotPassword },
  { path: 'reset-password', component: ResetPassword },
  { path: 'register', component: Register },
  { path: 'dashboard', component: Dashboard, canActivate: [authGuard] },
  {
    path: 'dashboard/create-timesheet',
    component: CreateTimesheet,
    canActivate: [authGuard],
  },
  {
    path: 'dashboard/week/:id',
    component: Dailylogs,
    canActivate: [authGuard],
  },
  {
    path: 'dashboard/week/:id/edit',
    component: EditTimesheet,
    canActivate: [authGuard],
  },
  {
    path: 'admin',
    component: Admin,
    canActivate: [adminGuard],
  },
  {
    path: 'admin/users/:id/edit',
    component: UserEdit,
    canActivate: [adminGuard],
  },
  { path: '', canActivate: [redirectGuard], children: [] },
  { path: '**', redirectTo: '' },
];
