import { Component, computed, effect, inject, signal } from '@angular/core';
import { SalesRepService } from '../../core/services/sales-rep.service';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Footer } from '../../shared/footer/footer';
import { Card } from '../../shared/card/card';
import { Nav } from '../../shared/nav/nav';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-sales-rep',
  imports: [CommonModule, FormsModule, Footer, Card, Nav, TranslatePipe],
  templateUrl: './sales-rep.html',
  styleUrl: './sales-rep.css',
})
export class SalesRep {
  readonly salesRepService = inject(SalesRepService);
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
    const totalPages = this.salesRepService.totalPages();
    const current = this.salesRepService.page();
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
    this.salesRepService.loadTimesheets(
      this.salesRepService.page(),
      this.salesRepService.pageSize(),
      this.statusFilter(),
      this.searchQuery(),
    );
  });

  onPageChange(page: number) {
    this.salesRepService.loadTimesheets(
      page,
      this.salesRepService.pageSize(),
      this.statusFilter(),
      this.searchQuery(),
    );
  }

  onStatusFilterChange() {
    this.salesRepService.loadTimesheets(
      1,
      this.salesRepService.pageSize(),
      this.statusFilter(),
      this.searchQuery(),
    );
  }

  onSearchChange() {
    this.salesRepService.loadTimesheets(
      1,
      this.salesRepService.pageSize(),
      this.statusFilter(),
      this.searchQuery(),
    );
  }

  viewTimesheet(timesheetId: number) {
    this.router.navigate(['/sales-rep/timesheets', timesheetId]);
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'PendingSalesRep':
        return 'badge-warning';
      case 'PendingManager':
        return 'badge-info';
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
