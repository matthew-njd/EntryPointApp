import { ChangeDetectorRef, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { Router } from '@angular/router';
import { WeeklyLogService } from '../../core/services/weeklog.service';
import { WeeklyLogRequest } from '../../core/models/weeklylog.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-create-timesheet',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-timesheet.html',
  styleUrl: './create-timesheet.css',
})
export class CreateTimesheet {
  private fb = inject(FormBuilder);
  private weeklyLogService = inject(WeeklyLogService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  timesheetForm: FormGroup;
  isLoading = signal(false);
  calculatedDateTo = signal<string>('');

  constructor() {
    this.timesheetForm = this.fb.group({
      dateFrom: ['', [Validators.required]],
    });

    this.timesheetForm.get('dateFrom')?.valueChanges.subscribe((dateFrom) => {
      if (dateFrom) {
        const startDate = new Date(dateFrom);
        const endDate = new Date(startDate);
        endDate.setDate(startDate.getDate() + 6);
        this.calculatedDateTo.set(endDate.toISOString().split('T')[0]);
      } else {
        this.calculatedDateTo.set('');
      }
    });
  }

  onSubmit(): void {
    if (this.timesheetForm.invalid) {
      this.timesheetForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.cdr.detectChanges();

    const request: WeeklyLogRequest = {
      dateFrom: this.timesheetForm.value.dateFrom,
      dateTo: this.calculatedDateTo(),
    };

    this.weeklyLogService.createWeeklyLog(request).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        if (response.success && response.data) {
          this.toastService.success(response.message);
          this.router.navigate(['/dashboard/week', response.data.id]);
        } else {
          this.toastService.error(
            response.message || 'Failed to create timesheet',
          );
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        this.cdr.detectChanges();
        this.toastService.error(
          error.error?.message || 'Failed to create timesheet',
        );
      },
    });
  }

  get dateFrom() {
    return this.timesheetForm.get('dateFrom');
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
