import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { redirectGuard } from './core/guards/redirect.guard';
import { adminGuard } from './core/guards/admin.guard';
import { managerGuard } from './core/guards/manager.guard';
import { salesRepGuard } from './core/guards/sales-rep.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/login/login').then(m => m.Login) },
  { path: 'forgot-password', loadComponent: () => import('./features/auth/forgot-password/forgot-password').then(m => m.ForgotPassword) },
  { path: 'reset-password', loadComponent: () => import('./features/auth/reset-password/reset-password').then(m => m.ResetPassword) },
  { path: 'register', loadComponent: () => import('./features/auth/register/register').then(m => m.Register) },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/user/dashboard/dashboard').then(m => m.Dashboard),
    canActivate: [authGuard],
  },
  {
    path: 'dashboard/create-timesheet',
    loadComponent: () => import('./features/user/create-timesheet/create-timesheet').then(m => m.CreateTimesheet),
    canActivate: [authGuard],
  },
  {
    path: 'dashboard/week/:id',
    loadComponent: () => import('./features/user/dailylogs/dailylogs').then(m => m.Dailylogs),
    canActivate: [authGuard],
  },
  {
    path: 'dashboard/week/:id/edit',
    loadComponent: () => import('./features/user/edit-timesheet/edit-timesheet').then(m => m.EditTimesheet),
    canActivate: [authGuard],
  },
  {
    path: 'admin',
    loadComponent: () => import('./features/admin/dashboard/dashboard').then(m => m.Admin),
    canActivate: [adminGuard],
  },
  {
    path: 'admin/users/:id/edit',
    loadComponent: () => import('./features/admin/user-edit/user-edit').then(m => m.UserEdit),
    canActivate: [adminGuard],
  },
  {
    path: 'admin/payroll-schedule',
    loadComponent: () => import('./features/admin/admin-payroll-schedule/admin-payroll-schedule').then(m => m.AdminPayrollSchedule),
    canActivate: [adminGuard],
  },
  {
    path: 'admin/approved-emails',
    loadComponent: () => import('./features/admin/admin-approved-emails/admin-approved-emails').then(m => m.AdminApprovedEmails),
    canActivate: [adminGuard],
  },
  {
    path: 'admin/payroll-summary',
    loadComponent: () => import('./features/admin/admin-payroll-summary/admin-payroll-summary').then(m => m.AdminPayrollSummary),
    canActivate: [adminGuard],
  },
  {
    path: 'admin/users/:id/timesheets',
    loadComponent: () => import('./features/admin/admin-user-timesheets/admin-user-timesheets').then(m => m.AdminUserTimesheets),
    canActivate: [adminGuard],
  },
  {
    path: 'admin/users/:id/timesheets/:timesheetId',
    loadComponent: () => import('./features/admin/admin-timesheet-detail/admin-timesheet-detail').then(m => m.AdminTimesheetDetail),
    canActivate: [adminGuard],
  },
  {
    path: 'manager',
    loadComponent: () => import('./features/manager/dashboard/dashboard').then(m => m.Manager),
    canActivate: [managerGuard],
  },
  {
    path: 'manager/timesheets/:id',
    loadComponent: () => import('./features/manager/review-timsheet/review-timsheet').then(m => m.ReviewTimsheet),
    canActivate: [managerGuard],
  },
  {
    path: 'sales-rep',
    loadComponent: () => import('./features/sales-rep/dashboard/dashboard').then(m => m.SalesRep),
    canActivate: [salesRepGuard],
  },
  {
    path: 'sales-rep/timesheets/:id',
    loadComponent: () => import('./features/sales-rep/sales-rep-review/sales-rep-review').then(m => m.SalesRepReview),
    canActivate: [salesRepGuard],
  },
  { path: '', canActivate: [redirectGuard], children: [] },
  { path: '**', redirectTo: '' },
];
