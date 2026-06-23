import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Nav } from '../../../shared/nav/nav';
import { Card } from '../../../shared/card/card';
import { WeeklyLogService } from '../../../core/services/weeklog.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { Footer } from '../../../shared/footer/footer';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, Nav, Card, Footer, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard {
  readonly weeklyLogService = inject(WeeklyLogService);
  readonly authService = inject(AuthService);
  private router = inject(Router);
  today = new Date();

  userFullName = '';

  statusFilter = signal<string>('All');
  startDate = signal<string>('');
  endDate = signal<string>('');

  constructor() {
    const user = this.authService.getCurrentUser();
    this.userFullName = user ? `${user.firstName} ${user.lastName}` : '';
  }

  totalApproved = computed(() => this.weeklyLogService.totalApproved());
  totalPending = computed(() => this.weeklyLogService.totalPending());
  totalDenied = computed(() => this.weeklyLogService.totalDenied());
  totalDrafts = computed(() => this.weeklyLogService.totalDraft());

  pageNumbers = computed(() => {
    const totalPages = this.weeklyLogService.totalPages();
    const current = this.weeklyLogService.page();
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
    this.weeklyLogService.loadWeeklyLogs(
      this.weeklyLogService.page(),
      this.weeklyLogService.pageSize(),
      this.statusFilter(),
      this.startDate(),
      this.endDate(),
    );
  });

  onFilterChange() {
    this.weeklyLogService.loadWeeklyLogs(
      1,
      this.weeklyLogService.pageSize(),
      this.statusFilter(),
      this.startDate(),
      this.endDate(),
    );
  }

  viewDailyLogs(logId: number) {
    this.router.navigate(['/dashboard/week', logId]);
  }

  editTimesheet(logId: number) {
    this.router.navigate(['/dashboard/week', logId, 'edit']);
  }

  createNewTimesheet() {
    this.router.navigate(['/dashboard/create-timesheet']);
  }

  onPageChange(page: number) {
    this.weeklyLogService.loadWeeklyLogs(
      page,
      this.weeklyLogService.pageSize(),
      this.statusFilter(),
      this.startDate(),
      this.endDate(),
    );
  }

  getStatusBadgeClass(status: string | undefined): string {
    switch (status) {
      case 'Draft':
        return 'badge-info';
      case 'PendingSalesRep':
      case 'PendingManager':
        return 'badge-warning';
      case 'Approved':
        return 'badge-success';
      case 'Denied':
        return 'badge-error';
      default:
        return '';
    }
  }
}
