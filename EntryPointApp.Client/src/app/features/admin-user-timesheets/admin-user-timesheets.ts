import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../core/services/admin.service';
import { ToastService } from '../../core/services/toast.service';
import { AdminTimesheetResponse, UserDto } from '../../core/models/admin.model';
import { Nav } from '../../shared/nav/nav';
import { Footer } from '../../shared/footer/footer';

@Component({
  selector: 'app-admin-user-timesheets',
  imports: [CommonModule, Nav, Footer],
  templateUrl: './admin-user-timesheets.html',
  styleUrl: './admin-user-timesheets.css',
})
export class AdminUserTimesheets {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminService = inject(AdminService);
  private toastService = inject(ToastService);

  userId = toSignal(this.route.paramMap);
  isLoading = signal(true);
  timesheets = signal<AdminTimesheetResponse[]>([]);
  user = signal<UserDto | null>(null);

  loadEffect = effect(() => {
    const id = this.userId()?.get('id');
    if (id) {
      this.loadData(+id);
    }
  });

  loadData(userId: number): void {
    this.isLoading.set(true);

    this.adminService.getUserById(userId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.user.set(response.data);
        }
      },
      error: () => {
        this.toastService.error('Failed to load user');
        this.router.navigate(['/admin']);
      },
    });

    this.adminService.getUserTimesheets(userId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.timesheets.set(response.data);
        } else {
          this.toastService.error(response.message || 'Failed to load timesheets');
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load timesheets');
        this.isLoading.set(false);
      },
    });
  }

  viewTimesheet(timesheetId: number): void {
    const id = this.userId()?.get('id');
    if (id) {
      this.router.navigate(['/admin/users', id, 'timesheets', timesheetId]);
    }
  }

  goBack(): void {
    const id = this.userId()?.get('id');
    if (id) {
      this.router.navigate(['/admin/users', id, 'edit']);
    } else {
      this.router.navigate(['/admin']);
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending': return 'badge-warning';
      case 'Approved': return 'badge-success';
      case 'Denied': return 'badge-error';
      case 'Draft': return 'badge-ghost';
      default: return '';
    }
  }

  getUserDisplayName(user: UserDto): string {
    if (user.firstName && user.lastName) {
      return `${user.firstName} ${user.lastName}`;
    }
    return user.email;
  }
}
