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
import { DailyLogService } from '../../core/services/dailylog.service';
import { WeeklyLogRequest } from '../../core/models/weeklylog.model';
import { DailyLogRequest } from '../../core/models/dailylog.model';
import { ToastService } from '../../core/services/toast.service';
import { Footer } from '../../shared/footer/footer';

interface DayForm {
  dayName: string;
  date: string;
  formGroup: FormGroup;
}

@Component({
  selector: 'app-create-timesheet',
  imports: [CommonModule, ReactiveFormsModule, Footer],
  templateUrl: './create-timesheet.html',
  styleUrl: './create-timesheet.css',
})
export class CreateTimesheet {
  private fb = inject(FormBuilder);
  private weeklyLogService = inject(WeeklyLogService);
  private dailyLogService = inject(DailyLogService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  timesheetForm: FormGroup;
  isLoading = signal(false);
  calculatedDateTo = signal<string>('');
  dayForms = signal<DayForm[]>([]);

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

        this.generateDayForms(dateFrom);
      } else {
        this.calculatedDateTo.set('');
        this.dayForms.set([]);
      }
    });
  }

  generateDayForms(startDateStr: string): void {
    const forms: DayForm[] = [];
    const startDate = new Date(startDateStr);
    const dayNames = [
      'Sunday',
      'Monday',
      'Tuesday',
      'Wednesday',
      'Thursday',
      'Friday',
      'Saturday',
    ];

    for (let i = 0; i < 7; i++) {
      const currentDate = new Date(startDate);
      currentDate.setDate(startDate.getDate() + i);
      const dateStr = currentDate.toISOString().split('T')[0];
      const dayName = dayNames[currentDate.getDay()];

      forms.push({
        dayName,
        date: dateStr,
        formGroup: this.fb.group({
          hours: [0, [Validators.min(0), Validators.max(24)]],
          mileage: [0, [Validators.min(0)]],
          tollCharge: [0, [Validators.min(0)]],
          parkingFee: [0, [Validators.min(0)]],
          otherCharges: [0, [Validators.min(0)]],
          comment: ['', [Validators.maxLength(500)]],
        }),
      });
    }

    this.dayForms.set(forms);
  }

  onSubmit(): void {
    if (this.timesheetForm.invalid) {
      this.timesheetForm.markAllAsTouched();
      return;
    }

    const filledDays = this.getFilledDays();
    if (filledDays.length === 0) {
      this.toastService.error('Please enter data for at least one day');
      return;
    }

    this.isLoading.set(true);
    this.cdr.detectChanges();

    const weeklyLogRequest: WeeklyLogRequest = {
      dateFrom: this.timesheetForm.value.dateFrom,
      dateTo: this.calculatedDateTo(),
    };

    this.weeklyLogService.createWeeklyLog(weeklyLogRequest).subscribe({
      next: (weeklyLogResponse) => {
        if (weeklyLogResponse.success && weeklyLogResponse.data) {
          const weeklyLogId = weeklyLogResponse.data.id;

          const dailyLogRequests: DailyLogRequest[] = filledDays.map((day) => ({
            date: day.date,
            hours: day.formGroup.value.hours,
            mileage: day.formGroup.value.mileage,
            tollCharge: day.formGroup.value.tollCharge,
            parkingFee: day.formGroup.value.parkingFee,
            otherCharges: day.formGroup.value.otherCharges,
            comment: day.formGroup.value.comment || '',
          }));

          this.dailyLogService
            .createDailyLogsBatch(weeklyLogId, dailyLogRequests)
            .subscribe({
              next: (dailyLogResponse) => {
                this.isLoading.set(false);
                if (dailyLogResponse.success) {
                  this.toastService.success('Timesheet created successfully!');
                  this.router.navigate(['/dashboard/week', weeklyLogId]);
                } else {
                  this.toastService.error(
                    dailyLogResponse.message || 'Failed to create daily logs',
                  );
                }
              },
              error: (error) => {
                this.isLoading.set(false);
                this.cdr.detectChanges();
                this.toastService.error(
                  error.error?.message || 'Failed to create daily logs',
                );
              },
            });
        } else {
          this.isLoading.set(false);
          this.toastService.error(
            weeklyLogResponse.message || 'Failed to create timesheet',
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

  getFilledDays(): DayForm[] {
    return this.dayForms().filter((day) => {
      const values = day.formGroup.value;
      return (
        values.hours > 0 ||
        values.mileage > 0 ||
        values.tollCharge > 0 ||
        values.parkingFee > 0 ||
        values.otherCharges > 0 ||
        values.comment.trim() !== ''
      );
    });
  }

  get dateFrom() {
    return this.timesheetForm.get('dateFrom');
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
