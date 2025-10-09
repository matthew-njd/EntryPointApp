import { Routes } from '@angular/router';
import { Dashboard } from './shared/dashboard/dashboard';
import { Login } from './features/auth/login/login';

export const routes: Routes = [
  { path: '', component: Login },
  { path: 'dashboard', component: Dashboard },
];
