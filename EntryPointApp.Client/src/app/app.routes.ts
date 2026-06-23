import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { redirectGuard } from './core/guards/redirect.guard';
import { Login } from './features/auth/login/login';
import { Dashboard } from './features/user/dashboard/dashboard';
import { Dailylogs } from './features/user/dailylogs/dailylogs';
import { Register } from './features/auth/register/register';
import { ForgotPassword } from './features/auth/forgot-password/forgot-password';
import { ResetPassword } from './features/auth/reset-password/reset-password';
import { CreateTimesheet } from './features/user/create-timesheet/create-timesheet';
import { EditTimesheet } from './features/user/edit-timesheet/edit-timesheet';
import { Admin } from './features/admin/dashboard/dashboard';
import { adminGuard } from './core/guards/admin.guard';
import { UserEdit } from './features/admin/user-edit/user-edit';
import { Manager } from './features/manager/dashboard/dashboard';
import { managerGuard } from './core/guards/manager.guard';
import { ReviewTimsheet } from './features/manager/review-timsheet/review-timsheet';
import { SalesRep } from './features/sales-rep/dashboard/dashboard';
import { SalesRepReview } from './features/sales-rep/sales-rep-review/sales-rep-review';
import { salesRepGuard } from './core/guards/sales-rep.guard';
import { AdminUserTimesheets } from './features/admin/admin-user-timesheets/admin-user-timesheets';
import { AdminTimesheetDetail } from './features/admin/admin-timesheet-detail/admin-timesheet-detail';
import { AdminPayrollSchedule } from './features/admin/admin-payroll-schedule/admin-payroll-schedule';
import { AdminApprovedEmails } from './features/admin/admin-approved-emails/admin-approved-emails';
import { AdminPayrollSummary } from './features/admin/admin-payroll-summary/admin-payroll-summary';

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
  {
    path: 'admin/payroll-schedule',
    component: AdminPayrollSchedule,
    canActivate: [adminGuard],
  },
  {
    path: 'admin/approved-emails',
    component: AdminApprovedEmails,
    canActivate: [adminGuard],
  },
  {
    path: 'admin/payroll-summary',
    component: AdminPayrollSummary,
    canActivate: [adminGuard],
  },
  {
    path: 'admin/users/:id/timesheets',
    component: AdminUserTimesheets,
    canActivate: [adminGuard],
  },
  {
    path: 'admin/users/:id/timesheets/:timesheetId',
    component: AdminTimesheetDetail,
    canActivate: [adminGuard],
  },
  {
    path: 'manager',
    component: Manager,
    canActivate: [managerGuard],
  },
  {
    path: 'manager/timesheets/:id',
    component: ReviewTimsheet,
    canActivate: [managerGuard],
  },
  {
    path: 'sales-rep',
    component: SalesRep,
    canActivate: [salesRepGuard],
  },
  {
    path: 'sales-rep/timesheets/:id',
    component: SalesRepReview,
    canActivate: [salesRepGuard],
  },
  { path: '', canActivate: [redirectGuard], children: [] },
  { path: '**', redirectTo: '' },
];
