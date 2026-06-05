import {
  ChangeDetectorRef,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ValidatorFn,
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
  date: string;
  formGroup: FormGroup;
  pendingFiles: File[];
  canAddReceipt: boolean;
  isFuture: boolean;
}

interface DayErrors {
  timeInError: string | null;
  timeOutError: string | null;
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
  submissionAttempted = signal(false);
  calculatedDateTo = signal<string>('');
  payrollDate = signal<string | null>(null);
  dayForms = signal<DayForm[]>([]);
  formChangeTrigger = signal(0);
  existingWeeks = signal<{ dateFrom: string; dateTo: string }[]>([]);
  overlappingWeek = signal<{ dateFrom: string; dateTo: string } | null>(null);

  readonly maxDateStr: string = (() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const day = today.getDay();
    const diff = day === 0 ? -6 : 1 - day;
    const monday = new Date(today);
    monday.setDate(today.getDate() + diff);
    return `${monday.getFullYear()}-${String(monday.getMonth() + 1).padStart(2, '0')}-${String(monday.getDate()).padStart(2, '0')}`;
  })();

  readonly minDateStr: string = (() => {
    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - 56);
    return `${cutoff.getFullYear()}-${String(cutoff.getMonth() + 1).padStart(2, '0')}-${String(cutoff.getDate()).padStart(2, '0')}`;
  })();

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

  hasAnyDayErrors = computed(() => {
    this.formChangeTrigger();
    return this.dayForms().some(day => {
      const e = this.getDayErrors(day);
      return e.timeInError !== null || e.timeOutError !== null || day.formGroup.invalid;
    });
  });

  constructor() {
    this.timesheetForm = this.fb.group({
      dateFrom: ['', [Validators.required, this.dateFromValidator()]],
    });

    this.weeklyLogService.getDateRanges().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.existingWeeks.set(
            response.data.data.map(w => ({ dateFrom: w.dateFrom, dateTo: w.dateTo }))
          );
        }
      },
      error: () => {},
    });

    this.timesheetForm.get('dateFrom')?.valueChanges.subscribe((dateFrom) => {
      if (dateFrom) {
        const [y, m, d] = dateFrom.split('-').map(Number);
        const endDate = new Date(y, m - 1, d + 6);
        const dateTo = `${endDate.getFullYear()}-${String(endDate.getMonth() + 1).padStart(2, '0')}-${String(endDate.getDate()).padStart(2, '0')}`;
        this.calculatedDateTo.set(dateTo);

        const dateErrors = this.timesheetForm.get('dateFrom')?.errors;

        if (dateErrors) {
          this.overlappingWeek.set(null);
          this.dayForms.set([]);
          this.payrollDate.set(null);
        } else {
          const overlap = this.existingWeeks().find(w =>
            dateFrom <= w.dateTo && dateTo >= w.dateFrom
          ) ?? null;
          this.overlappingWeek.set(overlap);

          if (!overlap) {
            this.generateDayForms(dateFrom);
            this.payrollScheduleService.lookup(dateFrom).subscribe({
              next: (res) => { this.payrollDate.set(res.data?.payrollDate ?? null); },
              error: () => {},
            });
          } else {
            this.dayForms.set([]);
            this.payrollDate.set(null);
          }
        }
      } else {
        this.calculatedDateTo.set('');
        this.dayForms.set([]);
        this.payrollDate.set(null);
        this.overlappingWeek.set(null);
      }
    });
  }

  generateDayForms(startDateStr: string): void {
    const forms: DayForm[] = [];
    const [y, m, d] = startDateStr.split('-').map(Number);
    const startDate = new Date(y, m - 1, d);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    for (let i = 0; i < 7; i++) {
      const currentDate = new Date(startDate);
      currentDate.setDate(startDate.getDate() + i);
      const dateStr = `${currentDate.getFullYear()}-${String(currentDate.getMonth() + 1).padStart(2, '0')}-${String(currentDate.getDate()).padStart(2, '0')}`;
      const isFuture = currentDate > today;

      const formGroup = this.fb.group({
        isDayOff: [false],
        timeIn: [''],
        timeOut: [''],
        mileage: [{ value: 0, disabled: true }, [Validators.min(0), Validators.max(500)]],
        tollCharge: [{ value: 0, disabled: true }, [Validators.min(0), Validators.max(999.99)]],
        parkingFee: [{ value: 0, disabled: true }, [Validators.min(0), Validators.max(999.99)]],
        otherCharges: [{ value: 0, disabled: true }, [Validators.min(0), Validators.max(999.99)]],
        comment: [{ value: '', disabled: true }, [Validators.maxLength(500)]],
      });

      if (isFuture) {
        formGroup.get('isDayOff')?.disable();
        formGroup.get('timeIn')?.disable();
        formGroup.get('timeOut')?.disable();
      }

      const dayForm: DayForm = {
        date: dateStr,
        formGroup,
        pendingFiles: [],
        canAddReceipt: false,
        isFuture,
      };
      forms.push(dayForm);

      if (!isFuture) {
        formGroup.get('isDayOff')?.valueChanges.subscribe((isDayOff) => {
          this.handleDayOffChange(dayForm, isDayOff ?? false);
        });

        this.subscribeToTimeChanges(dayForm);
      }
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

  getDayErrors(day: DayForm): DayErrors {
    if (day.formGroup.get('isDayOff')?.value || day.isFuture) {
      return { timeInError: null, timeOutError: null };
    }
    const timeIn = day.formGroup.get('timeIn')?.value;
    const timeOut = day.formGroup.get('timeOut')?.value;

    let timeInError: string | null = null;
    let timeOutError: string | null = null;

    if (!timeIn && timeOut) {
      timeInError = this.translateService.instant('errors.timeInRequired');
    }
    if (timeIn && !timeOut) {
      timeOutError = this.translateService.instant('errors.timeOutRequired');
    }
    if (timeIn && timeOut && timeOut <= timeIn) {
      timeOutError = this.translateService.instant('errors.timeOutAfterTimeIn');
    }
    return { timeInError, timeOutError };
  }

  shouldShowDayErrors(day: DayForm): boolean {
    return this.submissionAttempted() ||
      !!(day.formGroup.get('timeIn')?.touched) ||
      !!(day.formGroup.get('timeOut')?.touched) ||
      day.formGroup.dirty;
  }

  onSubmit(): void {
    this.submissionAttempted.set(true);
    this.dayForms().forEach(day => day.formGroup.markAllAsTouched());

    if (this.timesheetForm.invalid) {
      this.timesheetForm.markAllAsTouched();
      return;
    }

    if (this.overlappingWeek()) {
      return;
    }

    if (this.hasAnyDayErrors()) {
      const errorCount = this.dayForms().filter(day => {
        if (day.isFuture) return false;
        const e = this.getDayErrors(day);
        return e.timeInError !== null || e.timeOutError !== null || day.formGroup.invalid;
      }).length;
      this.toastService.error(
        this.translateService.instant('toast.dayErrorsOnSubmit', { count: errorCount })
      );
      return;
    }

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
          mileage: isDayOff ? 0 : day.formGroup.get('mileage')?.value || 0,
          tollCharge: isDayOff ? 0 : day.formGroup.get('tollCharge')?.value || 0,
          parkingFee: isDayOff ? 0 : day.formGroup.get('parkingFee')?.value || 0,
          otherCharges: isDayOff ? 0 : day.formGroup.get('otherCharges')?.value || 0,
          comment: isDayOff ? 'Day off' : day.formGroup.get('comment')?.value || '',
        };
      });

    if (dailyLogRequests.length === 0) {
      this.toastService.error(this.translateService.instant('toast.atLeastOneDayRequired'));
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
                    this.errorMessage(dailyLogResponse, this.translateService.instant('toast.failedSaveDailyLogs'))
                  );
                }
              },
              error: (error) => {
                this.isLoading.set(false);
                this.cdr.detectChanges();
                this.toastService.error(
                  this.errorMessage(error.error, this.translateService.instant('toast.failedSaveDailyLogs'))
                );
              },
            });
        } else {
          this.isLoading.set(false);
          this.toastService.error(
            this.errorMessage(weeklyLogResponse, this.translateService.instant('toast.failedCreateTimesheet'))
          );
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        this.cdr.detectChanges();
        this.toastService.error(
          this.errorMessage(error.error, this.translateService.instant('toast.failedCreateTimesheet'))
        );
      },
    });
  }

  onFileSelected(day: DayForm, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'application/pdf'];
    if (!allowedTypes.includes(file.type)) {
      this.toastService.error(this.translateService.instant('toast.invalidFileType'));
      input.value = '';
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      this.toastService.error(this.translateService.instant('toast.fileTooLarge'));
      input.value = '';
      return;
    }

    day.pendingFiles = [...day.pendingFiles, file];
    input.value = '';
    this.dayForms.update((days) => [...days]);
  }

  removeQueuedFile(day: DayForm, index: number): void {
    day.pendingFiles = day.pendingFiles.filter((_, i) => i !== index);
    this.dayForms.update((days) => [...days]);
  }

  blockNumericSpecialChars(event: KeyboardEvent): void {
    if (['-', '+', 'e', 'E'].includes(event.key)) {
      event.preventDefault();
    }
  }

  onNumericInput(event: Event, control: AbstractControl | null): void {
    const input = event.target as HTMLInputElement;
    const raw = input.value;
    let value = raw.replace(/[^\d.]/g, '');
    const dotIndex = value.indexOf('.');
    if (dotIndex !== -1) {
      value = value.substring(0, dotIndex + 1) + value.substring(dotIndex + 1).replace(/\./g, '');
    }
    const finalDot = value.indexOf('.');
    if (finalDot !== -1 && value.length - finalDot - 1 > 2) {
      value = value.substring(0, finalDot + 3);
    }
    if (raw !== value) {
      input.value = value;
      if (value !== '') {
        control?.setValue(parseFloat(value), { emitEvent: false });
      }
    }
  }

  getDayHours(day: DayForm): number {
    const timeIn = day.formGroup.get('timeIn')?.value as string;
    const timeOut = day.formGroup.get('timeOut')?.value as string;
    if (!timeIn || !timeOut) return 0;
    const [inH, inM] = timeIn.split(':').map(Number);
    const [outH, outM] = timeOut.split(':').map(Number);
    return (outH * 60 + outM - (inH * 60 + inM)) / 60;
  }

  private dateFromValidator(): ValidatorFn {
    return (control: AbstractControl) => {
      const value = control.value as string;
      if (!value) return null;

      const [y, m, d] = value.split('-').map(Number);
      const selected = new Date(y, m - 1, d);

      if (selected.getDay() !== 1) {
        return { notMonday: true };
      }

      const today = new Date();
      today.setHours(0, 0, 0, 0);

      if (selected > today) {
        return { futureWeek: true };
      }

      const cutoff = new Date(today);
      cutoff.setDate(today.getDate() - 56);
      if (selected < cutoff) {
        return { tooFarPast: true };
      }

      return null;
    };
  }

  get dateFrom() {
    return this.timesheetForm.get('dateFrom')!;
  }

  private errorMessage(source: any, fallback: string): string {
    const message: string = source?.message || fallback;
    const first: string | undefined = (source?.errors as string[])?.[0];
    return first ? `${message}: ${first}` : message;
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
