import {
  Component,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { Nav } from '../../../shared/nav/nav';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { UserDto } from '../../../core/models/admin.model';
import { AdminService } from '../../../core/services/admin.service';
import { Router } from '@angular/router';
import { ToastService } from '../../../core/services/toast.service';
import { Card } from '../../../shared/card/card';
import { Footer } from '../../../shared/footer/footer';
import { Modal } from '../../../shared/modal/modal';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-admin',
  imports: [CommonModule, FormsModule, Nav, Card, Footer, Modal, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Admin {
  readonly adminService = inject(AdminService);
  readonly authService = inject(AuthService);
  private router = inject(Router);
  private toastService = inject(ToastService);
  today = new Date();

  userFullName = '';

  toggleStatusModal = viewChild<Modal>('toggleStatusModal');
  pendingStatusUser = signal<UserDto | null>(null);

  roleFilter = signal<string>('All');
  statusFilter = signal<string>('All');
  searchQuery = signal<string>('');

  constructor() {
    const user = this.authService.getCurrentUser();
    this.userFullName = user ? `${user.firstName} ${user.lastName}` : '';
  }

  pageNumbers = computed(() => {
    const totalPages = this.adminService.totalPages();
    const current = this.adminService.page();
    const pages: number[] = [];
    const maxButtons = 5;

    let startPage = Math.max(1, current - Math.floor(maxButtons / 2));
    let endPage = Math.min(totalPages, startPage + maxButtons - 1);

    if (endPage - startPage < maxButtons - 1) {
      startPage = Math.max(1, endPage - maxButtons + 1);
    }

    for (let i = startPage; i <= endPage; i++) pages.push(i);

    return pages;
  });

  loadEffect = effect(() => {
    this.adminService.loadUsers(
      this.adminService.page(),
      this.adminService.pageSize(),
      this.roleFilter(),
      this.statusFilter(),
      this.searchQuery(),
    );
  });

  onFilterChange() {
    this.adminService.loadUsers(
      1,
      this.adminService.pageSize(),
      this.roleFilter(),
      this.statusFilter(),
      this.searchQuery(),
    );
  }

  onPageChange(page: number) {
    this.adminService.loadUsers(
      page,
      this.adminService.pageSize(),
      this.roleFilter(),
      this.statusFilter(),
      this.searchQuery(),
    );
  }

  editUser(userId: number) {
    this.router.navigate(['/admin/users', userId, 'edit']);
  }

  toggleUserStatus(user: UserDto) {
    this.pendingStatusUser.set(user);
    this.toggleStatusModal()?.open();
  }

  onToggleStatusConfirmed() {
    const user = this.pendingStatusUser();
    if (!user) return;

    const operation = user.isActive
      ? this.adminService.deactivateUser(user.id)
      : this.adminService.activateUser(user.id);

    operation.subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.success(response.message);
          this.onFilterChange();
        } else {
          this.toastService.error(response.message);
        }
        this.pendingStatusUser.set(null);
      },
      error: (err) => {
        const action = user.isActive ? 'deactivate' : 'activate';
        this.toastService.error(err.message || `Failed to ${action} user`);
        this.pendingStatusUser.set(null);
      },
    });
  }

  goToPayrollSchedule(): void {
    this.router.navigate(['/admin/payroll-schedule']);
  }

  goToPayrollSummary(): void {
    this.router.navigate(['/admin/payroll-summary']);
  }

  goToApprovedEmails(): void {
    this.router.navigate(['/admin/approved-emails']);
  }

  getUserFullName(user: UserDto): string {
    if (user.firstName && user.lastName) {
      return `${user.firstName} ${user.lastName}`;
    }
    return user.email;
  }

  getRoleClass(role: string): string {
    switch (role) {
      case 'Admin':
        return 'badge-error';
      case 'Manager':
        return 'badge-warning';
      case 'SalesRep':
        return 'badge-secondary';
      case 'User':
        return 'badge-info';
      default:
        return '';
    }
  }
}
