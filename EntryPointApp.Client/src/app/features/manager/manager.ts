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

  filteredTimesheets = computed(() => {
    let timesheets = this.service.timesheets();

    if (this.statusFilter() !== 'All') {
      timesheets = timesheets.filter((t) => t.status === this.statusFilter());
    }

    return timesheets;
  });

  loadEffect = effect(() => {
    this.service.loadTimesheets(this.statusFilter());
  });

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
