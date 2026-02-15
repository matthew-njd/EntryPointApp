import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const redirectGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    const user = authService.getCurrentUser();

    if (user?.role === 'Admin') {
      router.navigate(['/admin']);
    } else {
      router.navigate(['/dashboard']);
    }
  } else {
    router.navigate(['/login']);
  }

  return false;
};
