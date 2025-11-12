import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Nav } from '../nav/nav';
import {
  TimesheetService,
  PagedResult,
} from '../../core/services/timesheet.service';
import { WeeklyLog } from '../../core/models/weeklylog.model';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, Nav],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private timesheetService = inject(TimesheetService);
  private cdr = inject(ChangeDetectorRef);
  timesheets: WeeklyLog[] = [];
  pagedResult: PagedResult<WeeklyLog> | null = null;
  currentPage: number = 1;
  pageSize: number = 10;

  ngOnInit(): void {
    this.loadTimesheets(this.currentPage, this.pageSize);
  }

  loadTimesheets(page: number, pageSize: number): void {
    this.timesheetService.getTimesheets(page, pageSize).subscribe({
      next: (result) => {
        console.log('Timesheets fetched:', result);
        this.pagedResult = result;
        this.timesheets = result.data;
        this.currentPage = result.page;

        this.cdr.detectChanges(); //TODO: check why this needs to be manually used
      },
      error: (error) => {
        console.error('Timesheets fetch failed:', error);
      },
    });
  }

  getStatusClass(status: string | undefined): string {
    switch (status) {
      case 'Pending':
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
    this.loadTimesheets(page, this.pageSize);
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
