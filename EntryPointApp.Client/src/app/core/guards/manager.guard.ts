import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const managerGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/login'], {
      queryParams: { returnUrl: state.url },
    });
    return false;
  }

  const currentUser = authService.getCurrentUser();

  if (
    currentUser &&
    (currentUser.role === 'Manager' || currentUser.role === 'Admin')
  ) {
    return true;
  }

  router.navigate(['/dashboard']);
  return false;
};
