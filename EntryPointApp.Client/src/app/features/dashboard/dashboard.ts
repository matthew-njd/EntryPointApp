import { Component, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Nav } from '../../shared/nav/nav';
import { Card } from '../../shared/card/card';
import { WeeklyLogService } from '../../core/services/weeklog.service';
import { Router } from '@angular/router';
import { UserResponse } from '../../core/models/auth.model';
import { Footer } from '../../shared/footer/footer';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, Nav, Card, Footer, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard {
  public service = inject(WeeklyLogService);
  private router = inject(Router);

  userFullName = '';

  constructor() {
    this.loadUserFullName();
  }

  totalApproved = computed(() => this.service.totalApproved());
  totalPending = computed(() => this.service.totalPending());
  totalDenied = computed(() => this.service.totalDenied());
  totalDrafts = computed(() => this.service.totalDraft());

  pageNumbers = computed(() => {
    const totalPages = this.service.totalPages();
    const current = this.service.page();
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
    this.service.loadWeeklyLogs(this.service.page(), this.service.pageSize());
  });

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
    this.service.loadWeeklyLogs(page, this.service.pageSize());
  }

  getStatusBadgeClass(status: string | undefined): string {
    switch (status) {
      case 'Draft':    return 'badge-warning';
      case 'Pending':  return 'badge-info';
      case 'Approved': return 'badge-success';
      case 'Denied':   return 'badge-error';
      default:         return '';
    }
  }

  private loadUserFullName() {
    try {
      const userJson = localStorage.getItem('current_user');
      if (userJson) {
        const user: UserResponse = JSON.parse(userJson);
        this.userFullName = `${user.firstName} ${user.lastName}`;
      }
    } catch (err) {
      console.error('Failed to parse user data', err);
    }
  }
}
