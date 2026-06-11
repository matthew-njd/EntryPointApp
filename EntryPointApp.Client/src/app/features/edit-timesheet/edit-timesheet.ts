import {
  ChangeDetectorRef,
  Component,
  inject,
  signal,
  effect,
  computed,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { WeeklyLogService } from '../../core/services/weeklog.service';
import { DailyLogService } from '../../core/services/dailylog.service';
import { WeeklyLog } from '../../core/models/weeklylog.model';
import {
  DailyLogUpdateItem,
  ReceiptResponse,
  UpdateDailyLogsRequest,
} from '../../core/models/dailylog.model';
import { ToastService } from '../../core/services/toast.service';
import { Footer } from '../../shared/footer/footer';
import { Nav } from '../../shared/nav/nav';
import { Modal } from '../../shared/modal/modal';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { PayrollScheduleService } from '../../core/services/payroll-schedule.service';

interface DayForm {
  date: string;
  formGroup: FormGroup;
  existingId?: number;
  receipts: ReceiptResponse[];
  pendingFiles: File[];
  isUploadingReceipt: boolean;
  canAddReceipt: boolean;
  isFuture: boolean;
}

interface DayErrors {
  timeInError: string | null;
  timeOutError: string | null;
}

@Component({
  selector: 'app-edit-timesheet',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    Footer,
    Nav,
    Modal,
    TranslatePipe,
  ],
  templateUrl: './edit-timesheet.html',
  styleUrl: './edit-timesheet.css',
})
export class EditTimesheet {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private weeklyLogService = inject(WeeklyLogService);
  private dailyLogService = inject(DailyLogService);
  private toastService = inject(ToastService);
  private translateService = inject(TranslateService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private payrollScheduleService = inject(PayrollScheduleService);
  private submissionConfirmed = false;
  timesheetForm: FormGroup;
  weeklyLogId = toSignal(this.route.paramMap);
  isLoading = signal(false);
  submissionAttempted = signal(false);
  isLoadingData = signal(true);
  weeklyLog = signal<WeeklyLog | null>(null);
  payrollDate = signal<string | null>(null);
  dayForms = signal<DayForm[]>([]);
  formChangeTrigger = signal(0);
  confirmModal = viewChild<Modal>('confirmModal');

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

  filledDaysCount = computed(() => {
    this.formChangeTrigger();
    return this.dayForms().filter((day) => {
      const values = day.formGroup.getRawValue();
      return (
        (values.timeIn && values.timeOut) ||
        values.comment?.toLowerCase() === 'day off'
      );
    }).length;
  });

  allDaysFilled = computed(() => {
    return this.filledDaysCount() === 7;
  });

  hasAnyDayErrors = computed(() => {
    this.formChangeTrigger();
    return this.dayForms().some((day) => {
      const e = this.getDayErrors(day);
      return (
        e.timeInError !== null ||
        e.timeOutError !== null ||
        day.formGroup.invalid
      );
    });
  });

  constructor() {
    this.timesheetForm = this.fb.group({});
  }

  loadEffect = effect(() => {
    const id = this.weeklyLogId()?.get('id');
    if (id) {
      this.loadTimesheetData(+id);
    }
  });

  loadTimesheetData(weeklyLogId: number): void {
    this.isLoadingData.set(true);

    this.weeklyLogService.getWeeklyLogById(weeklyLogId).subscribe({
      next: (weeklyResponse) => {
        if (weeklyResponse.success && weeklyResponse.data) {
          const weeklyLog = weeklyResponse.data;
          this.weeklyLog.set(weeklyLog);

          this.payrollScheduleService.lookup(weeklyLog.dateFrom).subscribe({
            next: (res) => {
              this.payrollDate.set(res.data?.payrollDate ?? null);
            },
            error: () => {},
          });

          if (weeklyLog.status !== 'Draft' && weeklyLog.status !== 'Denied') {
            this.toastService.error(
              this.translateService.instant('toast.onlyDraftEditable'),
            );
            this.router.navigate(['/dashboard/week', weeklyLogId]);
            return;
          }

          this.dailyLogService.loadDailyLogs(weeklyLogId);

          setTimeout(() => {
            this.generateDayForms(weeklyLog.dateFrom, weeklyLog.dateTo);
            this.isLoadingData.set(false);
          }, 500);
        } else {
          this.toastService.error(
            this.translateService.instant('toast.failedLoadTimesheet'),
          );
          this.router.navigate(['/dashboard']);
        }
      },
      error: (error) => {
        this.toastService.error(
          error.error?.message ||
            this.translateService.instant('toast.failedLoadTimesheet'),
        );
        this.router.navigate(['/dashboard']);
      },
    });
  }

  generateDayForms(dateFromStr: string, dateToStr: string): void {
    const forms: DayForm[] = [];
    const [sy, sm, sd] = dateFromStr.split('-').map(Number);
    const startDate = new Date(sy, sm - 1, sd);
    const [ey, em, ed] = dateToStr.split('-').map(Number);
    const endDate = new Date(ey, em - 1, ed);
    const existingLogs = this.dailyLogService.dailyLogs();

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const daysDiff =
      Math.round(
        (endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24),
      ) + 1;

    for (let i = 0; i < daysDiff; i++) {
      const currentDate = new Date(startDate);
      currentDate.setDate(startDate.getDate() + i);
      const dateStr = `${currentDate.getFullYear()}-${String(currentDate.getMonth() + 1).padStart(2, '0')}-${String(currentDate.getDate()).padStart(2, '0')}`;
      const isFuture = currentDate > today;

      const existingLog = existingLogs.find((log) => log.date === dateStr);

      const isDayOff = existingLog?.comment?.toLowerCase() === 'day off';
      const isEmptyTime =
        !!existingLog &&
        existingLog.timeIn === '00:00:00' &&
        existingLog.timeOut === '00:00:00' &&
        !isDayOff;

      const hasRealTimes =
        !!existingLog && !isEmptyTime && !isDayOff && !isFuture;

      const formGroup = this.fb.group({
        isDayOff: [isDayOff],
        timeIn: [
          {
            value: isEmptyTime
              ? ''
              : (existingLog?.timeIn?.substring(0, 5) ?? ''),
            disabled: isDayOff || isFuture,
          },
        ],
        timeOut: [
          {
            value: isEmptyTime
              ? ''
              : (existingLog?.timeOut?.substring(0, 5) ?? ''),
            disabled: isDayOff || isFuture,
          },
        ],
        mileage: [
          { value: existingLog?.mileage || 0, disabled: !hasRealTimes },
          [Validators.min(0), Validators.max(500)],
        ],
        tollCharge: [
          { value: existingLog?.tollCharge || 0, disabled: !hasRealTimes },
          [Validators.min(0), Validators.max(999.99)],
        ],
        parkingFee: [
          { value: existingLog?.parkingFee || 0, disabled: !hasRealTimes },
          [Validators.min(0), Validators.max(999.99)],
        ],
        otherCharges: [
          { value: existingLog?.otherCharges || 0, disabled: !hasRealTimes },
          [Validators.min(0), Validators.max(999.99)],
        ],
        comment: [
          { value: existingLog?.comment || '', disabled: !hasRealTimes },
          [Validators.maxLength(500)],
        ],
      });

      if (isFuture) {
        formGroup.get('isDayOff')?.disable();
      }

      forms.push({
        date: dateStr,
        existingId: existingLog?.id,
        receipts: existingLog?.receipts ?? [],
        pendingFiles: [],
        isUploadingReceipt: false,
        canAddReceipt: hasRealTimes,
        isFuture,
        formGroup,
      });

      const lastForm = forms[forms.length - 1];

      if (!isFuture) {
        lastForm.formGroup
          .get('isDayOff')
          ?.valueChanges.subscribe((isDayOff) => {
            this.handleDayOffChange(lastForm, isDayOff ?? false);
          });

        this.subscribeToTimeChanges(lastForm);
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
        ['mileage', 'tollCharge', 'parkingFee', 'otherCharges'].forEach(
          (field) => {
            formGroup.get(field)?.setValue(0);
            formGroup.get(field)?.disable();
          },
        );
        formGroup.get('comment')?.setValue('');
        formGroup.get('comment')?.disable();
        day.canAddReceipt = false;
      }
      this.dayForms.update((days) => [...days]);
    };

    formGroup.get('timeIn')?.valueChanges.subscribe(onTimeChange);
    formGroup.get('timeOut')?.valueChanges.subscribe(onTimeChange);
  }

  onFileSelected(day: DayForm, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const allowedTypes = [
      'image/jpeg',
      'image/jpg',
      'image/png',
      'application/pdf',
    ];
    if (!allowedTypes.includes(file.type)) {
      this.toastService.error(
        this.translateService.instant('toast.invalidFileType'),
      );
      input.value = '';
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      this.toastService.error(
        this.translateService.instant('toast.fileTooLarge'),
      );
      input.value = '';
      return;
    }

    if (!day.existingId) {
      day.pendingFiles = [...day.pendingFiles, file];
      input.value = '';
      this.dayForms.update((days) => [...days]);
      return;
    }

    const weeklyLogId = this.weeklyLog()?.id;
    if (!weeklyLogId) return;

    day.isUploadingReceipt = true;
    this.dayForms.update((days) => [...days]);

    this.dailyLogService
      .uploadReceipt(weeklyLogId, day.existingId, file)
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            day.receipts = [...day.receipts, response.data];
            this.toastService.success(
              this.translateService.instant('toast.receiptUploaded'),
            );
          } else {
            this.toastService.error(
              response.message ||
                this.translateService.instant('toast.failedUploadReceipt'),
            );
          }
          day.isUploadingReceipt = false;
          input.value = '';
          this.dayForms.update((days) => [...days]);
        },
        error: (err) => {
          this.toastService.error(
            err.error?.message ||
              this.translateService.instant('toast.failedUploadReceipt'),
          );
          day.isUploadingReceipt = false;
          input.value = '';
          this.dayForms.update((days) => [...days]);
        },
      });
  }

  removeQueuedFile(day: DayForm, index: number): void {
    day.pendingFiles = day.pendingFiles.filter((_, i) => i !== index);
    this.dayForms.update((days) => [...days]);
  }

  deleteReceipt(day: DayForm, attachmentId: number): void {
    const weeklyLogId = this.weeklyLog()?.id;
    if (!weeklyLogId || !day.existingId) return;

    this.dailyLogService
      .deleteReceipt(weeklyLogId, day.existingId, attachmentId)
      .subscribe({
        next: (response) => {
          if (response.success) {
            day.receipts = day.receipts.filter((r) => r.id !== attachmentId);
            this.toastService.success(
              this.translateService.instant('toast.receiptDeleted'),
            );
            this.dayForms.update((days) => [...days]);
          } else {
            this.toastService.error(
              response.message ||
                this.translateService.instant('toast.failedDeleteReceipt'),
            );
          }
        },
        error: (err) => {
          this.toastService.error(
            err.error?.message ||
              this.translateService.instant('toast.failedDeleteReceipt'),
          );
        },
      });
  }

  downloadReceipt(
    dailyLogId: number,
    attachmentId: number,
    fileName: string,
  ): void {
    const weeklyLogId = this.weeklyLog()?.id;
    if (!weeklyLogId) return;

    this.dailyLogService
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
    return (
      this.submissionAttempted() ||
      !!day.formGroup.get('timeIn')?.touched ||
      !!day.formGroup.get('timeOut')?.touched ||
      day.formGroup.dirty
    );
  }

  onSubmit(): void {
    const weeklyLog = this.weeklyLog();
    if (!weeklyLog) return;

    this.submissionAttempted.set(true);
    this.dayForms().forEach((day) => day.formGroup.markAllAsTouched());

    if (this.hasAnyDayErrors()) {
      const errorCount = this.dayForms().filter((day) => {
        if (day.isFuture) return false;
        const e = this.getDayErrors(day);
        return (
          e.timeInError !== null ||
          e.timeOutError !== null ||
          day.formGroup.invalid
        );
      }).length;
      this.toastService.error(
        this.translateService.instant('toast.dayErrorsOnSubmit', {
          count: errorCount,
        }),
      );
      return;
    }

    if (!this.submissionConfirmed) {
      this.confirmModal()?.open();
      return;
    }

    this.submissionConfirmed = false;
    this.isLoading.set(true);
    this.cdr.detectChanges();

    const dailyLogs: DailyLogUpdateItem[] = this.dayForms()
      .filter((day) => {
        const isDayOff = day.formGroup.get('isDayOff')?.value;
        const timeIn = day.formGroup.get('timeIn')?.value;
        const timeOut = day.formGroup.get('timeOut')?.value;
        return isDayOff || (timeIn && timeOut);
      })
      .map((day) => {
        const formGroup = day.formGroup;
        const isDayOff = formGroup.get('isDayOff')?.value;

        return {
          id: day.existingId || null,
          date: day.date,
          timeIn: isDayOff
            ? '00:00'
            : formGroup.get('timeIn')?.value || '00:00',
          timeOut: isDayOff
            ? '00:00'
            : formGroup.get('timeOut')?.value || '00:00',
          mileage: isDayOff ? 0 : formGroup.get('mileage')?.value || 0,
          tollCharge: isDayOff ? 0 : formGroup.get('tollCharge')?.value || 0,
          parkingFee: isDayOff ? 0 : formGroup.get('parkingFee')?.value || 0,
          otherCharges: isDayOff
            ? 0
            : formGroup.get('otherCharges')?.value || 0,
          comment: isDayOff ? 'Day off' : formGroup.get('comment')?.value || '',
        };
      });

    const request: UpdateDailyLogsRequest = { dailyLogs };

    this.dailyLogService.updateDailyLogs(weeklyLog.id, request).subscribe({
      next: (response) => {
        if (response.success) {
          const updatedLogs = response.data ?? [];
          const uploadTasks = this.dayForms().flatMap((dayForm) => {
            if (!dayForm.pendingFiles.length) return [];
            const savedLog = updatedLogs.find(
              (log) => log.date === dayForm.date,
            );
            if (!savedLog) return [];
            return dayForm.pendingFiles.map((file) =>
              this.dailyLogService.uploadReceipt(
                weeklyLog.id,
                savedLog.id,
                file,
              ),
            );
          });

          const navigate = () => {
            this.isLoading.set(false);
            this.toastService.success(
              this.translateService.instant('toast.timesheetUpdated'),
            );
            this.router.navigate(['/dashboard/week', weeklyLog.id]);
          };

          if (uploadTasks.length === 0) {
            navigate();
          } else {
            forkJoin(uploadTasks).subscribe({
              next: () => navigate(),
              error: () => {
                this.isLoading.set(false);
                this.toastService.success(
                  this.translateService.instant('toast.timesheetUpdated'),
                );
                this.toastService.error(
                  this.translateService.instant('toast.receiptUploadFailed'),
                );
                this.router.navigate(['/dashboard/week', weeklyLog.id]);
              },
            });
          }
        } else {
          this.isLoading.set(false);
          this.toastService.error(
            this.errorMessage(
              response,
              this.translateService.instant('toast.failedUpdateTimesheet'),
            ),
          );
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        this.cdr.detectChanges();
        this.toastService.error(
          this.errorMessage(
            error.error,
            this.translateService.instant('toast.failedUpdateTimesheet'),
          ),
        );
      },
    });
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
      value =
        value.substring(0, dotIndex + 1) +
        value.substring(dotIndex + 1).replace(/\./g, '');
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

  onSubmitConfirmed(): void {
    this.submissionConfirmed = true;
    this.onSubmit();
  }

  private errorMessage(source: any, fallback: string): string {
    const message: string = source?.message || fallback;
    const first: string | undefined = (source?.errors as string[])?.[0];
    return first ? `${message}: ${first}` : message;
  }

  getDayHours(day: DayForm): number {
    const timeIn = day.formGroup.get('timeIn')?.value as string;
    const timeOut = day.formGroup.get('timeOut')?.value as string;
    if (!timeIn || !timeOut) return 0;
    const [inH, inM] = timeIn.split(':').map(Number);
    const [outH, outM] = timeOut.split(':').map(Number);
    return (outH * 60 + outM - (inH * 60 + inM)) / 60;
  }

  goBack(): void {
    const id = this.weeklyLogId()?.get('id');
    if (id) {
      this.router.navigate(['/dashboard/week', id]);
    } else {
      this.router.navigate(['/dashboard']);
    }
  }
}
