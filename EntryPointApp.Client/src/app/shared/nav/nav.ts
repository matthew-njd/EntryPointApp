import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-nav',
  imports: [],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
})
export class Nav {
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private router = inject(Router);

  onLogout(): void {
    this.authService.logout();
    this.toastService.success("You've successfully logged out!");
    this.router.navigate(['/login']);
  }
}
