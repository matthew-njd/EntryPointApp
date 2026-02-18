import { Component, computed, effect, inject, signal } from '@angular/core';
import { ManagerService } from '../../core/services/manager.service';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Footer } from '../../shared/footer/footer';
import { Card } from '../../shared/card/card';
import { Nav } from '../../shared/nav/nav';

@Component({
  selector: 'app-manager',
  imports: [CommonModule, FormsModule, Footer, Card, Nav],
  templateUrl: './manager.html',
  styleUrl: './manager.css',
})
export class Manager {
  readonly service = inject(ManagerService);
  private router = inject(Router);

  statusFilter = signal<string>('All');

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
    this.service.loadTimesheets(
      this.service.page(),
      this.service.pageSize(),
      this.statusFilter(),
    );
  });

  onPageChange(page: number) {
    this.service.loadTimesheets(
      page,
      this.service.pageSize(),
      this.statusFilter(),
    );
  }

  onStatusFilterChange() {
    this.service.loadTimesheets(
      1,
      this.service.pageSize(),
      this.statusFilter(),
    );
  }

  viewTimesheet(timesheetId: number) {
    this.router.navigate(['/manager/timesheets', timesheetId]);
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending':
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

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  }
}
