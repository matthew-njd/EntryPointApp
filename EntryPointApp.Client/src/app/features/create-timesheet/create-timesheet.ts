import {
  ChangeDetectorRef,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { WeeklyLogService } from '../../core/services/weeklog.service';
import { DailyLogService } from '../../core/services/dailylog.service';
import { WeeklyLogRequest } from '../../core/models/weeklylog.model';
import { DailyLogRequest } from '../../core/models/dailylog.model';
import { ToastService } from '../../core/services/toast.service';
import { Footer } from '../../shared/footer/footer';
import { Nav } from '../../shared/nav/nav';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { PayrollScheduleService } from '../../core/services/payroll-schedule.service';

interface DayForm {
  dayName: string;
  date: string;
  formGroup: FormGroup;
  pendingFiles: File[];
  canAddReceipt: boolean;
}

@Component({
  selector: 'app-create-timesheet',
  imports: [CommonModule, DatePipe, ReactiveFormsModule, Footer, Nav, TranslatePipe],
  templateUrl: './create-timesheet.html',
  styleUrl: './create-timesheet.css',
})
export class CreateTimesheet {
  private fb = inject(FormBuilder);
  private weeklyLogService = inject(WeeklyLogService);
  private dailyLogService = inject(DailyLogService);
  private toastService = inject(ToastService);
  private translateService = inject(TranslateService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private payrollScheduleService = inject(PayrollScheduleService);

  timesheetForm: FormGroup;
  isLoading = signal(false);
  calculatedDateTo = signal<string>('');
  payrollDate = signal<string | null>(null);
  dayForms = signal<DayForm[]>([]);
  formChangeTrigger = signal(0);

  filledDaysCount = computed(() => {
    this.formChangeTrigger();
    return this.dayForms().filter((day) => {
      const values = day.formGroup.getRawValue();
      return (values.timeIn && values.timeOut) || values.comment?.toLowerCase() === 'day off';
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
        const [y, m, d] = dateFrom.split('-').map(Number);
        const endDate = new Date(y, m - 1, d + 6);
        this.calculatedDateTo.set(
          `${endDate.getFullYear()}-${String(endDate.getMonth() + 1).padStart(2, '0')}-${String(endDate.getDate()).padStart(2, '0')}`
        );

        this.generateDayForms(dateFrom);

        this.payrollScheduleService.lookup(dateFrom).subscribe({
          next: (res) => { this.payrollDate.set(res.data?.payrollDate ?? null); },
          error: () => {},
        });
      } else {
        this.calculatedDateTo.set('');
        this.dayForms.set([]);
        this.payrollDate.set(null);
      }
    });
  }

  generateDayForms(startDateStr: string): void {
    const forms: DayForm[] = [];
    const [y, m, d] = startDateStr.split('-').map(Number);
    const startDate = new Date(y, m - 1, d);
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
      const dateStr = `${currentDate.getFullYear()}-${String(currentDate.getMonth() + 1).padStart(2, '0')}-${String(currentDate.getDate()).padStart(2, '0')}`;
      const dayName = dayNames[currentDate.getDay()];

      const formGroup = this.fb.group({
        isDayOff: [false],
        timeIn: [''],
        timeOut: [''],
        mileage: [{ value: 0, disabled: true }, [Validators.min(0)]],
        tollCharge: [{ value: 0, disabled: true }, [Validators.min(0)]],
        parkingFee: [{ value: 0, disabled: true }, [Validators.min(0)]],
        otherCharges: [{ value: 0, disabled: true }, [Validators.min(0)]],
        comment: [{ value: '', disabled: true }, [Validators.maxLength(500)]],
      });

      const dayForm: DayForm = {
        dayName,
        date: dateStr,
        formGroup,
        pendingFiles: [],
        canAddReceipt: false,
      };
      forms.push(dayForm);

      formGroup.get('isDayOff')?.valueChanges.subscribe((isDayOff) => {
        this.handleDayOffChange(dayForm, isDayOff ?? false);
      });

      this.subscribeToTimeChanges(dayForm);
    }

    forms.forEach((form) => {
      form.formGroup.valueChanges.subscribe(() => {
        this.formChangeTrigger.update((v) => v + 1);
      });
    });

    this.dayForms.set(forms);
  }

  handleDayOffChange(day: DayForm, isDayOff: boolean): void {
    const formGroup = day.formGroup;
    if (isDayOff) {
      formGroup.get('timeIn')?.setValue('');
      formGroup.get('timeIn')?.disable();
      formGroup.get('timeOut')?.setValue('');
      formGroup.get('timeOut')?.disable();
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
      day.canAddReceipt = false;
    } else {
      formGroup.get('timeIn')?.enable();
      formGroup.get('timeOut')?.enable();
      formGroup.get('comment')?.setValue('');
      // canAddReceipt stays false until Time In and Time Out are entered
    }
    this.dayForms.update((days) => [...days]);
  }

  private subscribeToTimeChanges(day: DayForm): void {
    const formGroup = day.formGroup;
    const onTimeChange = () => {
      if (formGroup.get('isDayOff')?.value) return;
      const timeIn = formGroup.get('timeIn')?.value;
      const timeOut = formGroup.get('timeOut')?.value;

      if (timeIn && timeOut) {
        formGroup.get('mileage')?.enable();
        formGroup.get('tollCharge')?.enable();
        formGroup.get('parkingFee')?.enable();
        formGroup.get('otherCharges')?.enable();
        formGroup.get('comment')?.enable();
        day.canAddReceipt = true;
      } else {
        ['mileage', 'tollCharge', 'parkingFee', 'otherCharges'].forEach((field) => {
          formGroup.get(field)?.setValue(0);
          formGroup.get(field)?.disable();
        });
        formGroup.get('comment')?.setValue('');
        formGroup.get('comment')?.disable();
        day.canAddReceipt = false;
      }
      this.dayForms.update((days) => [...days]);
    };

    formGroup.get('timeIn')?.valueChanges.subscribe(onTimeChange);
    formGroup.get('timeOut')?.valueChanges.subscribe(onTimeChange);
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

          const dailyLogRequests: DailyLogRequest[] = this.dayForms()
            .filter((day) => {
              const isDayOff = day.formGroup.get('isDayOff')?.value;
              const timeIn = day.formGroup.get('timeIn')?.value;
              const timeOut = day.formGroup.get('timeOut')?.value;
              return isDayOff || (timeIn && timeOut);
            })
            .map((day) => {
              const isDayOff = day.formGroup.get('isDayOff')?.value;
              return {
                date: day.date,
                timeIn: isDayOff ? '00:00' : day.formGroup.get('timeIn')?.value || '00:00',
                timeOut: isDayOff ? '00:00' : day.formGroup.get('timeOut')?.value || '00:00',
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
                if (dailyLogResponse.success && dailyLogResponse.data) {
                  const createdLogs = dailyLogResponse.data;
                  const uploadTasks = createdLogs.flatMap((createdLog) => {
                    const dayForm = this.dayForms().find((d) => d.date === createdLog.date);
                    return (dayForm?.pendingFiles ?? []).map((file) =>
                      this.dailyLogService.uploadReceipt(weeklyLogId, createdLog.id, file)
                    );
                  });

                  const navigate = () => {
                    this.isLoading.set(false);
                    this.toastService.success(dailyLogResponse.message);
                    this.router.navigate(['/dashboard/week', weeklyLogId]);
                  };

                  if (uploadTasks.length === 0) {
                    navigate();
                  } else {
                    forkJoin(uploadTasks).subscribe({
                      next: () => navigate(),
                      error: () => {
                        this.isLoading.set(false);
                        this.toastService.success(dailyLogResponse.message);
                        this.toastService.error(this.translateService.instant('toast.receiptUploadFailed'));
                        this.router.navigate(['/dashboard/week', weeklyLogId]);
                      },
                    });
                  }
                } else {
                  this.isLoading.set(false);
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

  onFileSelected(day: DayForm, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    day.pendingFiles = [...day.pendingFiles, file];
    input.value = '';
    this.dayForms.update((days) => [...days]);
  }

  removeQueuedFile(day: DayForm, index: number): void {
    day.pendingFiles = day.pendingFiles.filter((_, i) => i !== index);
    this.dayForms.update((days) => [...days]);
  }

  get dateFrom() {
    return this.timesheetForm.get('dateFrom');
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
