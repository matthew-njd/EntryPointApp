import { Component, computed, effect, inject, signal } from '@angular/core';
import { ManagerService } from '../../core/services/manager.service';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Footer } from '../../shared/footer/footer';
import { Card } from '../../shared/card/card';
import { Nav } from '../../shared/nav/nav';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-manager',
  imports: [CommonModule, FormsModule, Footer, Card, Nav, TranslatePipe],
  templateUrl: './manager.html',
  styleUrl: './manager.css',
})
export class Manager {
  readonly managerService = inject(ManagerService);
  readonly authService = inject(AuthService);
  private router = inject(Router);
  today = new Date();

  userFullName = '';

  statusFilter = signal<string>('All');
  searchQuery = signal<string>('');

  constructor() {
    const user = this.authService.getCurrentUser();
    this.userFullName = user ? `${user.firstName} ${user.lastName}` : '';
  }

  pageNumbers = computed(() => {
    const totalPages = this.managerService.totalPages();
    const current = this.managerService.page();
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
    this.managerService.loadTimesheets(
      this.managerService.page(),
      this.managerService.pageSize(),
      this.statusFilter(),
      this.searchQuery(),
    );
  });

  onPageChange(page: number) {
    this.managerService.loadTimesheets(
      page,
      this.managerService.pageSize(),
      this.statusFilter(),
      this.searchQuery(),
    );
  }

  onStatusFilterChange() {
    this.managerService.loadTimesheets(
      1,
      this.managerService.pageSize(),
      this.statusFilter(),
      this.searchQuery(),
    );
  }

  onSearchChange() {
    this.managerService.loadTimesheets(
      1,
      this.managerService.pageSize(),
      this.statusFilter(),
      this.searchQuery(),
    );
  }

  viewTimesheet(timesheetId: number) {
    this.router.navigate(['/manager/timesheets', timesheetId]);
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'PendingSalesRep':
        return 'badge-info';
      case 'PendingManager':
        return 'badge-warning';
      case 'Approved':
        return 'badge-success';
      case 'Denied':
        return 'badge-error';
      case 'Draft':
        return 'badge-ghost';
      default:
        return '';
    }
  }
}
