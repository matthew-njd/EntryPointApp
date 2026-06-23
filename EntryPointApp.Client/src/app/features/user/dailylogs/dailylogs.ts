import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DailyLogService } from '../../core/services/dailylog.service';
import { Nav } from '../../shared/nav/nav';
import { DatePipe, NgClass } from '@angular/common';
import { Card } from '../../shared/card/card';
import { Footer } from '../../shared/footer/footer';
import { WeeklyLogService } from '../../core/services/weeklog.service';
import { WeeklyLog } from '../../core/models/weeklylog.model';
import { ToastService } from '../../core/services/toast.service';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { PayrollScheduleService } from '../../core/services/payroll-schedule.service';

@Component({
  selector: 'app-dailylogs',
  standalone: true,
  imports: [Nav, DatePipe, Card, Footer, TranslatePipe, NgClass],
  templateUrl: './dailylogs.html',
  styleUrl: './dailylogs.css',
})
export class Dailylogs {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  readonly service = inject(DailyLogService);
  private weeklyLogService = inject(WeeklyLogService);
  private toastService = inject(ToastService);
  private translateService = inject(TranslateService);
  private payrollScheduleService = inject(PayrollScheduleService);

  weeklyLogId = toSignal(this.route.paramMap);
  weeklyLog = signal<WeeklyLog | null>(null);
  isLoadingWeeklyLog = signal(false);
  payrollDate = signal<string | null>(null);
  deadlineDate = signal<string | null>(null);

  loadEffect = effect(() => {
    const id = this.weeklyLogId()?.get('id');
    if (id) {
      const weeklyLogId = +id;

      this.service.loadDailyLogs(weeklyLogId);

      this.isLoadingWeeklyLog.set(true);
      this.weeklyLogService.getWeeklyLogById(weeklyLogId).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.weeklyLog.set(response.data);
            this.payrollScheduleService
              .lookup(response.data.dateFrom)
              .subscribe({
                next: (res) => {
                  this.payrollDate.set(res.data?.payrollDate ?? null);
                  this.deadlineDate.set(res.data?.deadlineDate ?? null);
                },
                error: () => {},
              });
          }
          this.isLoadingWeeklyLog.set(false);
        },
        error: (err) => {
          console.error('Failed to load weeklylog', err);
          this.isLoadingWeeklyLog.set(false);
        },
      });
    }
  });

  totalHours = computed(() =>
    this.service
      .dailyLogs()
      .reduce((sum, log) => sum + log.hours, 0)
      .toFixed(2),
  );

  totalMileage = computed(() =>
    this.service
      .dailyLogs()
      .reduce((sum, log) => sum + log.mileage, 0)
      .toFixed(2),
  );

  totalCharges = computed(() =>
    this.service
      .dailyLogs()
      .reduce(
        (sum, log) => sum + log.tollCharge + log.parkingFee + log.otherCharges,
        0,
      )
      .toFixed(2),
  );

  downloadReceipt(
    weeklyLogId: number,
    dailyLogId: number,
    attachmentId: number,
    fileName: string,
  ): void {
    this.service
      .downloadReceipt(weeklyLogId, dailyLogId, attachmentId)
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = fileName;
          a.click();
          URL.revokeObjectURL(url);
        },
        error: () => {
          this.toastService.error(
            this.translateService.instant('toast.failedDownloadReceipt'),
          );
        },
      });
  }

  editTimesheet() {
    const id = this.weeklyLogId()?.get('id');
    if (id) {
      this.router.navigate(['/dashboard/week', id, 'edit']);
    }
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }

  private readonly DAY_KEYS = [
    'days.sunday',
    'days.monday',
    'days.tuesday',
    'days.wednesday',
    'days.thursday',
    'days.friday',
    'days.saturday',
  ];
  private readonly MONTH_KEYS = [
    'months.january',
    'months.february',
    'months.march',
    'months.april',
    'months.may',
    'months.june',
    'months.july',
    'months.august',
    'months.september',
    'months.october',
    'months.november',
    'months.december',
  ];

  getDayKey(dateStr: string): string {
    const [y, m, d] = dateStr.split('-').map(Number);
    return this.DAY_KEYS[new Date(y, m - 1, d).getDay()];
  }

  getMonthKey(dateStr: string): string {
    const [y, m, d] = dateStr.split('-').map(Number);
    return this.MONTH_KEYS[new Date(y, m - 1, d).getMonth()];
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'PendingSalesRep':
      case 'PendingManager':
        return 'badge-warning';
      case 'Approved':
        return 'badge-success';
      case 'Denied':
        return 'badge-error';
      case 'Draft':
        return 'badge-info';
      default:
        return '';
    }
  }
}
