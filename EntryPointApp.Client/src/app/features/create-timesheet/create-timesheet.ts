import {
  ChangeDetectorRef,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
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
  formChangeTrigger = signal(0);

  filledDaysCount = computed(() => {
    this.formChangeTrigger();
    return this.dayForms().filter((day) => {
      const values = day.formGroup.getRawValue();
      return values.hours > 0 || values.comment?.toLowerCase() === 'day off';
    }).length;
  });

  allDaysFilled = computed(() => {
    return this.filledDaysCount() === 7;
  });

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

      const formGroup = this.fb.group({
        isDayOff: [false],
        hours: [0, [Validators.min(0), Validators.max(24)]],
        mileage: [0, [Validators.min(0)]],
        tollCharge: [0, [Validators.min(0)]],
        parkingFee: [0, [Validators.min(0)]],
        otherCharges: [0, [Validators.min(0)]],
        comment: ['', [Validators.maxLength(500)]],
      });

      forms.push({
        dayName,
        date: dateStr,
        formGroup,
      });

      formGroup.get('isDayOff')?.valueChanges.subscribe((isDayOff) => {
        this.handleDayOffChange(formGroup, isDayOff ?? false);
      });
    }

    forms.forEach((form) => {
      form.formGroup.valueChanges.subscribe(() => {
        this.formChangeTrigger.update((v) => v + 1);
      });
    });

    this.dayForms.set(forms);
  }

  handleDayOffChange(formGroup: FormGroup, isDayOff: boolean): void {
    if (isDayOff) {
      formGroup.get('hours')?.setValue(0);
      formGroup.get('hours')?.disable();
      formGroup.get('mileage')?.setValue(0);
      formGroup.get('mileage')?.disable();
      formGroup.get('tollCharge')?.setValue(0);
      formGroup.get('tollCharge')?.disable();
      formGroup.get('parkingFee')?.setValue(0);
      formGroup.get('parkingFee')?.disable();
      formGroup.get('otherCharges')?.setValue(0);
      formGroup.get('otherCharges')?.disable();
      formGroup.get('comment')?.setValue('Day off');
      formGroup.get('comment')?.disable();
    } else {
      formGroup.get('hours')?.enable();
      formGroup.get('mileage')?.enable();
      formGroup.get('tollCharge')?.enable();
      formGroup.get('parkingFee')?.enable();
      formGroup.get('otherCharges')?.enable();
      formGroup.get('comment')?.setValue('');
      formGroup.get('comment')?.enable();
    }
  }

  onSubmit(): void {
    if (this.timesheetForm.invalid) {
      this.timesheetForm.markAllAsTouched();
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

          const dailyLogRequests: DailyLogRequest[] = this.dayForms().map(
            (day) => {
              const isDayOff = day.formGroup.get('isDayOff')?.value;
              return {
                date: day.date,
                hours: isDayOff ? 0 : day.formGroup.get('hours')?.value || 0,
                mileage: isDayOff
                  ? 0
                  : day.formGroup.get('mileage')?.value || 0,
                tollCharge: isDayOff
                  ? 0
                  : day.formGroup.get('tollCharge')?.value || 0,
                parkingFee: isDayOff
                  ? 0
                  : day.formGroup.get('parkingFee')?.value || 0,
                otherCharges: isDayOff
                  ? 0
                  : day.formGroup.get('otherCharges')?.value || 0,
                comment: isDayOff
                  ? 'Day off'
                  : day.formGroup.get('comment')?.value || '',
              };
            },
          );

          this.dailyLogService
            .createDailyLogsBatch(weeklyLogId, dailyLogRequests)
            .subscribe({
              next: (dailyLogResponse) => {
                this.isLoading.set(false);
                if (dailyLogResponse.success) {
                  this.toastService.success(dailyLogResponse.message);
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

  get dateFrom() {
    return this.timesheetForm.get('dateFrom');
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
