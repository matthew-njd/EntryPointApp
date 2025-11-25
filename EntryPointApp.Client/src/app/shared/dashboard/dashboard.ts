import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Nav } from '../nav/nav';
import { WeeklyLog } from '../../core/models/weeklylog.model';
import {
  WeeklyLogService,
  PagedResult,
} from '../../core/services/weeklog.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, Nav],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private weeklyLogService = inject(WeeklyLogService);
  private cdr = inject(ChangeDetectorRef);

  weeklyLogs: WeeklyLog[] = [];
  pagedResult: PagedResult<WeeklyLog> | null = null;

  get currentPage(): number {
    return this.pagedResult?.page ?? 1;
  }
  get pageSize(): number {
    return this.pagedResult?.pageSize ?? 10;
  }

  ngOnInit(): void {
    this.loadWeeklyLogs();
  }

  loadWeeklyLogs(page?: number, pageSize?: number): void {
    this.weeklyLogService.getWeeklyLogs(page, pageSize).subscribe({
      next: (result) => {
        console.log('WeeklyLogs fetched:', result);
        this.pagedResult = result;
        this.weeklyLogs = result.data;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('WeeklyLogs fetch failed:', error);
      },
    });
  }

  getStatusClass(status: string | undefined): string {
    switch (status) {
      case 'Draft':
        return 'text-warning';
      case 'Submitted':
        return 'text-info';
      case 'Approved':
        return 'text-success';
      case 'Denied':
        return 'text-error';
      default:
        return '';
    }
  }

  onPageChange(page: number): void {
    this.loadWeeklyLogs(page, this.pageSize);
  }

  getPageNumbers(): number[] {
    if (!this.pagedResult) return [];

    const totalPages = this.pagedResult.totalPages;
    const current = this.currentPage;
    const pages: number[] = [];

    // Show max 5 page buttons at a time
    const maxButtons = 5;
    let startPage = Math.max(1, current - Math.floor(maxButtons / 2));
    let endPage = Math.min(totalPages, startPage + maxButtons - 1);

    // Adjust start page if we're near the end
    if (endPage - startPage < maxButtons - 1) {
      startPage = Math.max(1, endPage - maxButtons + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }

    return pages;
  }
}
