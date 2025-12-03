import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Nav } from '../../shared/nav/nav';
import { WeeklyLog } from '../../core/models/weeklylog.model';
import {
  WeeklyLogService,
  PagedResult,
} from '../../core/services/weeklog.service';
import { UserResponse } from '../../core/models/auth.model';
import { Router } from '@angular/router';
import { Card } from '../../shared/card/card';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, Nav, Card],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private weeklyLogService = inject(WeeklyLogService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);

  weeklyLogs: WeeklyLog[] = [];
  pagedResult: PagedResult<WeeklyLog> | null = null;

  userFullName: string = '';

  get currentPage(): number {
    return this.pagedResult?.page ?? 1;
  }
  get pageSize(): number {
    return this.pagedResult?.pageSize ?? 10;
  }

  ngOnInit(): void {
    this.loadWeeklyLogs();
    this.loadUserFullName();
  }

  loadWeeklyLogs(page?: number, pageSize?: number): void {
    this.weeklyLogService.getWeeklyLogs(page, pageSize).subscribe({
      next: (result) => {
        this.pagedResult = result;
        this.weeklyLogs = result.data;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('WeeklyLogs fetch failed:', error);
      },
    });
  }

  loadUserFullName() {
    try {
      const userJson = localStorage.getItem('current_user');
      if (userJson) {
        const currentUser: UserResponse = JSON.parse(userJson);
        this.userFullName = `${currentUser.firstName} ${currentUser.lastName}`;
      }
    } catch (error) {
      console.error('Failed to parse user data:', error);
    }
  }

  viewDailyLogs(weeklyLog: WeeklyLog): void {
    this.router.navigate(['/dashboard/week', weeklyLog.id]);
  }

  getStatusClass(status: string | undefined): string {
    switch (status) {
      case 'Draft':
        return 'text-info';
      case 'Pending':
        return 'text-warning';
      case 'Approved':
        return 'text-success';
      case 'Denied':
        return 'text-error';
      default:
        return '';
    }
  }

  //TODO: Only displays totals for current page. Need to add summary to API
  getTotalApproved(): number {
    return this.weeklyLogs.filter((log) => log.status === 'Approved').length;
  }

  getTotalPending(): number {
    return this.weeklyLogs.filter((log) => log.status === 'Pending').length;
  }

  getTotalDenied(): number {
    return this.weeklyLogs.filter((log) => log.status === 'Denied').length;
  }

  getTotalDrafts(): number {
    return this.weeklyLogs.filter((log) => log.status === 'Draft').length;
  }

  onPageChange(page: number): void {
    this.loadWeeklyLogs(page, this.pageSize);
  }

  getPageNumbers(): number[] {
    if (!this.pagedResult) return [];

    const totalPages = this.pagedResult.totalPages;
    const current = this.currentPage;
    const pages: number[] = [];

    const maxButtons = 5;
    let startPage = Math.max(1, current - Math.floor(maxButtons / 2));
    let endPage = Math.min(totalPages, startPage + maxButtons - 1);

    if (endPage - startPage < maxButtons - 1) {
      startPage = Math.max(1, endPage - maxButtons + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }

    return pages;
  }
}
