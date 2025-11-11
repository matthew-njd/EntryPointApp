import { Component, inject, OnInit } from '@angular/core';
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
        this.timesheets = result.items;
        this.currentPage = result.pageNumber;
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
}
