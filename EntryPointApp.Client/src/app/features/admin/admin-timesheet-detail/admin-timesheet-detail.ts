import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../core/services/admin.service';
import { DailyLogService } from '../../core/services/dailylog.service';
import { ToastService } from '../../core/services/toast.service';
import { AdminTimesheetDetailResponse } from '../../core/models/admin.model';
import { Nav } from '../../shared/nav/nav';
import { Footer } from '../../shared/footer/footer';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-admin-timesheet-detail',
  imports: [CommonModule, Nav, Footer, TranslatePipe],
  templateUrl: './admin-timesheet-detail.html',
  styleUrl: './admin-timesheet-detail.css',
})
export class AdminTimesheetDetail {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminService = inject(AdminService);
  private dailyLogService = inject(DailyLogService);
  private toastService = inject(ToastService);
  private translateService = inject(TranslateService);
  params = toSignal(this.route.paramMap);
  isLoading = signal(true);
  timesheet = signal<AdminTimesheetDetailResponse | null>(null);

  loadEffect = effect(() => {
    const userId = this.params()?.get('id');
    const timesheetId = this.params()?.get('timesheetId');
    if (userId && timesheetId) {
      this.loadDetail(+userId, +timesheetId);
    }
  });

  loadDetail(userId: number, timesheetId: number): void {
    this.isLoading.set(true);

    this.adminService.getUserTimesheetDetail(userId, timesheetId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.timesheet.set(response.data);
        } else {
          this.toastService.error(response.message || this.translateService.instant('toast.failedLoadTimesheet'));
          this.goBack();
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.toastService.error(this.translateService.instant('toast.failedLoadTimesheet'));
        this.goBack();
      },
    });
  }

  downloadReceipt(weeklyLogId: number, dailyLogId: number, attachmentId: number, fileName: string): void {
    this.dailyLogService.downloadReceipt(weeklyLogId, dailyLogId, attachmentId).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.toastService.error(this.translateService.instant('toast.failedDownloadReceipt'));
      },
    });
  }

  goBack(): void {
    const id = this.params()?.get('id');
    if (id) {
      this.router.navigate(['/admin/users', id, 'timesheets']);
    } else {
      this.router.navigate(['/admin']);
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending': return 'badge-warning';
      case 'Approved': return 'badge-success';
      case 'Denied': return 'badge-error';
      case 'Draft': return 'badge-ghost';
      default: return '';
    }
  }

  private readonly DAY_KEYS = ['days.sunday', 'days.monday', 'days.tuesday', 'days.wednesday', 'days.thursday', 'days.friday', 'days.saturday'];
  private readonly MONTH_KEYS = ['months.january', 'months.february', 'months.march', 'months.april', 'months.may', 'months.june', 'months.july', 'months.august', 'months.september', 'months.october', 'months.november', 'months.december'];

  getDayKey(dateStr: string): string {
    const [y, m, d] = dateStr.split('-').map(Number);
    return this.DAY_KEYS[new Date(y, m - 1, d).getDay()];
  }

  getMonthKey(dateStr: string): string {
    const [y, m, d] = dateStr.split('-').map(Number);
    return this.MONTH_KEYS[new Date(y, m - 1, d).getMonth()];
  }
}
