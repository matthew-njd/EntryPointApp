import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ManagerService } from '../../core/services/manager.service';
import { ToastService } from '../../core/services/toast.service';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  ApproveTimesheetRequest,
  DenyTimesheetRequest,
  TeamTimesheetDetailResponse,
} from '../../core/models/manager.model';
import { Footer } from '../../shared/footer/footer';
import { Nav } from '../../shared/nav/nav';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-review-timsheet',
  imports: [CommonModule, ReactiveFormsModule, Footer, Nav],
  templateUrl: './review-timsheet.html',
  styleUrl: './review-timsheet.css',
})
export class ReviewTimsheet {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private managerService = inject(ManagerService);
  private toastService = inject(ToastService);
  private fb = inject(FormBuilder);

  timesheetId = toSignal(this.route.paramMap);
  isLoadingData = signal(true);
  isSubmitting = signal(false);
  timesheet = signal<TeamTimesheetDetailResponse | null>(null);
  showDenyForm = signal(false);

  denyForm: FormGroup;

  totalCharges = computed(() => {
    const ts = this.timesheet();
    if (!ts) return 0;
    return ts.dailyLogs.reduce(
      (sum, log) =>
        sum + log.tollCharge + log.parkingFee + log.otherCharges + log.mileage,
      0,
    );
  });

  constructor() {
    this.denyForm = this.fb.group({
      reason: ['', [Validators.required, Validators.maxLength(500)]],
    });
  }

  loadEffect = effect(() => {
    const id = this.timesheetId()?.get('id');
    if (id) {
      this.loadTimesheetDetail(+id);
    }
  });

  loadTimesheetDetail(timesheetId: number): void {
    this.isLoadingData.set(true);

    this.managerService.getTimesheetDetail(timesheetId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.timesheet.set(response.data);
          this.isLoadingData.set(false);
        } else {
          this.toastService.error('Failed to load timesheet');
          this.router.navigate(['/manager']);
        }
      },
      error: (err) => {
        this.toastService.error(err.message || 'Failed to load timesheet');
        this.router.navigate(['/manager']);
      },
    });
  }

  approveTimesheet(): void {
    const ts = this.timesheet();
    if (!ts || ts.status !== 'Pending') {
      this.toastService.error('Only pending timesheets can be approved');
      return;
    }

    if (
      !confirm(
        `Approve timesheet for ${ts.userFullName} (${ts.totalHours} hours)?`,
      )
    ) {
      return;
    }

    this.isSubmitting.set(true);

    const request: ApproveTimesheetRequest = { comment: null };

    this.managerService.approveTimesheet(ts.id, request).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.success('Timesheet approved successfully!');
          this.router.navigate(['/manager']);
        } else {
          this.toastService.error(response.message);
          this.isSubmitting.set(false);
        }
      },
      error: (err) => {
        this.toastService.error(err.message || 'Failed to approve timesheet');
        this.isSubmitting.set(false);
      },
    });
  }

  toggleDenyForm(): void {
    const ts = this.timesheet();
    if (!ts || ts.status !== 'Pending') {
      this.toastService.error('Only pending timesheets can be denied');
      return;
    }

    this.showDenyForm.set(!this.showDenyForm());
    if (!this.showDenyForm()) {
      this.denyForm.reset();
    }
  }

  submitDeny(): void {
    if (this.denyForm.invalid) {
      this.denyForm.markAllAsTouched();
      return;
    }

    const ts = this.timesheet();
    if (!ts) return;

    this.isSubmitting.set(true);

    const request: DenyTimesheetRequest = {
      reason: this.denyForm.value.reason,
    };

    this.managerService.denyTimesheet(ts.id, request).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.success('Timesheet denied');
          this.router.navigate(['/manager']);
        } else {
          this.toastService.error(response.message);
          this.isSubmitting.set(false);
        }
      },
      error: (err) => {
        this.toastService.error(err.message || 'Failed to deny timesheet');
        this.isSubmitting.set(false);
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/manager']);
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
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  }

  get reasonControl() {
    return this.denyForm.get('reason');
  }
}
