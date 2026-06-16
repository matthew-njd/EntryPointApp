import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Nav } from '../../shared/nav/nav';
import { Footer } from '../../shared/footer/footer';
import { ToastService } from '../../core/services/toast.service';
import {
  PayrollScheduleService,
  PayrollScheduleEntry,
} from '../../core/services/payroll-schedule.service';
import { AdminSummaryService } from '../../core/services/admin-summary.service';
import { PayrollSummaryItem, PayrollSummaryResponse } from '../../core/models/admin.model';

@Component({
  selector: 'app-admin-payroll-summary',
  imports: [CommonModule, FormsModule, Nav, Footer],
  templateUrl: './admin-payroll-summary.html',
})
export class AdminPayrollSummary implements OnInit {
  private scheduleService = inject(PayrollScheduleService);
  private summaryService = inject(AdminSummaryService);
  private toastService = inject(ToastService);
  private router = inject(Router);

  scheduleEntries = signal<PayrollScheduleEntry[]>([]);
  selectedEntryId = signal<number | null>(null);
  summary = signal<PayrollSummaryResponse | null>(null);
  isLoading = signal(false);
  isDownloading = signal(false);

  selectedEntry = computed(() => {
    const id = this.selectedEntryId();
    if (id === null) return null;
    return this.scheduleEntries().find((e) => e.id === id) ?? null;
  });

  totalGrossPay = computed(() =>
    (this.summary()?.items ?? []).reduce((sum, i) => sum + i.grossPay, 0)
  );

  totalMileageReimbursement = computed(() =>
    (this.summary()?.items ?? []).reduce((sum, i) => sum + i.mileageReimbursement, 0)
  );

  ngOnInit(): void {
    this.loadScheduleEntries();
  }

  loadScheduleEntries(): void {
    this.scheduleService.getAll().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          const sorted = [...res.data].sort(
            (a, b) => new Date(b.dateFrom).getTime() - new Date(a.dateFrom).getTime()
          );
          this.scheduleEntries.set(sorted);
        }
      },
      error: () => this.toastService.error('Failed to load payroll schedule'),
    });
  }

  onPeriodChange(): void {
    const entry = this.selectedEntry();
    if (!entry) {
      this.summary.set(null);
      return;
    }

    this.isLoading.set(true);
    this.summary.set(null);

    this.summaryService.getSummary(entry.dateFrom, entry.dateTo).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.summary.set(res.data);
        } else {
          this.toastService.error(res.message || 'Failed to load payroll summary');
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load payroll summary');
        this.isLoading.set(false);
      },
    });
  }

  downloadExcel(): void {
    const entry = this.selectedEntry();
    if (!entry) return;

    this.isDownloading.set(true);
    this.summaryService.downloadSummaryExcel(entry.dateFrom, entry.dateTo).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `PayrollSummary_${entry.dateFrom}_${entry.dateTo}.xlsx`;
        a.click();
        URL.revokeObjectURL(url);
        this.isDownloading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to download Excel file');
        this.isDownloading.set(false);
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin']);
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr + 'T00:00:00');
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  formatCurrency(value: number): string {
    return value.toLocaleString('en-US', { style: 'currency', currency: 'USD' });
  }

  formatRate(value: number, decimals = 2): string {
    return '$' + value.toFixed(decimals);
  }
}
