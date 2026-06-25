import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Nav } from '../../../shared/nav/nav';
import { Footer } from '../../../shared/footer/footer';
import { ToastService } from '../../../core/services/toast.service';
import { PayrollScheduleService } from '../../../core/services/payroll-schedule.service';
import { AdminSummaryService } from '../../../core/services/admin-summary.service';
import { PayrollSummaryResponse } from '../../../core/models/admin.model';

interface WeeklyPeriod {
  dateFrom: string;
  dateTo: string;
  label: string;
}

interface WeeklyPeriodGroup {
  year: number;
  periods: WeeklyPeriod[];
}

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

  private scheduleEntries = signal<{ dateFrom: string; dateTo: string }[]>([]);
  selectedYear = signal<number | null>(null);
  selectedWeekDateFrom = signal<string | null>(null);
  summary = signal<PayrollSummaryResponse | null>(null);
  isLoading = signal(false);
  isDownloading = signal(false);

  private weeklyPeriodsByYear = computed<WeeklyPeriodGroup[]>(() => {
    const allPeriods: WeeklyPeriod[] = [];

    for (const entry of this.scheduleEntries()) {
      const start = new Date(entry.dateFrom + 'T00:00:00');

      const week1End = new Date(start);
      week1End.setDate(week1End.getDate() + 6);

      const week2Start = new Date(start);
      week2Start.setDate(week2Start.getDate() + 7);

      const fmt = (d: Date) => d.toISOString().slice(0, 10);

      allPeriods.push({
        dateFrom: entry.dateFrom,
        dateTo: fmt(week1End),
        label: `${this.formatDate(entry.dateFrom)} – ${this.formatDate(fmt(week1End))}`,
      });

      allPeriods.push({
        dateFrom: fmt(week2Start),
        dateTo: entry.dateTo,
        label: `${this.formatDate(fmt(week2Start))} – ${this.formatDate(entry.dateTo)}`,
      });
    }

    allPeriods.sort((a, b) => b.dateFrom.localeCompare(a.dateFrom));

    const byYear = new Map<number, WeeklyPeriod[]>();
    for (const p of allPeriods) {
      const year = new Date(p.dateFrom + 'T00:00:00').getFullYear();
      if (!byYear.has(year)) byYear.set(year, []);
      byYear.get(year)!.push(p);
    }

    return Array.from(byYear.entries())
      .sort((a, b) => b[0] - a[0])
      .map(([year, periods]) => ({ year, periods }));
  });

  availableYears = computed(() => this.weeklyPeriodsByYear().map((g) => g.year));

  weeksForSelectedYear = computed<WeeklyPeriod[]>(() => {
    const year = this.selectedYear();
    if (year === null) return [];
    return this.weeklyPeriodsByYear().find((g) => g.year === year)?.periods ?? [];
  });

  selectedWeeklyPeriod = computed<WeeklyPeriod | null>(() => {
    const dateFrom = this.selectedWeekDateFrom();
    if (!dateFrom) return null;
    return this.weeksForSelectedYear().find((p) => p.dateFrom === dateFrom) ?? null;
  });

  totalGrossPay = computed(() =>
    (this.summary()?.items ?? []).reduce((sum, i) => sum + i.grossPay, 0)
  );

  totalMileageReimbursement = computed(() =>
    (this.summary()?.items ?? []).reduce((sum, i) => sum + i.mileageReimbursement, 0)
  );

  totalTollCharges = computed(() =>
    (this.summary()?.items ?? []).reduce((sum, i) => sum + i.totalTollCharges, 0)
  );

  totalParkingFees = computed(() =>
    (this.summary()?.items ?? []).reduce((sum, i) => sum + i.totalParkingFees, 0)
  );

  totalOtherCharges = computed(() =>
    (this.summary()?.items ?? []).reduce((sum, i) => sum + i.totalOtherCharges, 0)
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

  onYearChange(): void {
    this.selectedWeekDateFrom.set(null);
    this.summary.set(null);
  }

  onWeekChange(): void {
    const period = this.selectedWeeklyPeriod();
    if (!period) {
      this.summary.set(null);
      return;
    }

    this.isLoading.set(true);
    this.summary.set(null);

    this.summaryService.getSummary(period.dateFrom, period.dateTo).subscribe({
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
    const period = this.selectedWeeklyPeriod();
    if (!period) return;

    this.isDownloading.set(true);
    this.summaryService.downloadSummaryExcel(period.dateFrom, period.dateTo).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `PayrollSummary_${period.dateFrom}_${period.dateTo}.xlsx`;
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
