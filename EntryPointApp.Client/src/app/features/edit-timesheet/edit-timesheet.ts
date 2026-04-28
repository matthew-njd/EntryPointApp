import {
  ChangeDetectorRef,
  Component,
  inject,
  signal,
  effect,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { WeeklyLogService } from '../../core/services/weeklog.service';
import { DailyLogService } from '../../core/services/dailylog.service';
import { WeeklyLog } from '../../core/models/weeklylog.model';
import {
  DailyLogUpdateItem,
  UpdateDailyLogsRequest,
} from '../../core/models/dailylog.model';
import { ToastService } from '../../core/services/toast.service';
import { Footer } from '../../shared/footer/footer';
import { Nav } from '../../shared/nav/nav';

interface DayForm {
  dayName: string;
  date: string;
  formGroup: FormGroup;
  existingId?: number;
}

@Component({
  selector: 'app-edit-timesheet',
  imports: [CommonModule, ReactiveFormsModule, Footer, Nav],
  templateUrl: './edit-timesheet.html',
  styleUrl: './edit-timesheet.css',
})
export class EditTimesheet {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private weeklyLogService = inject(WeeklyLogService);
  private dailyLogService = inject(DailyLogService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  timesheetForm: FormGroup;
  weeklyLogId = toSignal(this.route.paramMap);
  isLoading = signal(false);
  isLoadingData = signal(true);
  weeklyLog = signal<WeeklyLog | null>(null);
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

          if (weeklyLog.status !== 'Draft') {
            this.toastService.error('Only Draft timesheets can be edited');
            this.router.navigate(['/dashboard/week', weeklyLogId]);
            return;
          }

          this.dailyLogService.loadDailyLogs(weeklyLogId);

          setTimeout(() => {
            this.generateDayForms(weeklyLog.dateFrom, weeklyLog.dateTo);
            this.isLoadingData.set(false);
          }, 500);
        } else {
          this.toastService.error('Failed to load timesheet');
          this.router.navigate(['/dashboard']);
        }
      },
      error: (error) => {
        this.toastService.error(
          error.error?.message || 'Failed to load timesheet',
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
    const dayNames = [
      'Sunday',
      'Monday',
      'Tuesday',
      'Wednesday',
      'Thursday',
      'Friday',
      'Saturday',
    ];
    const existingLogs = this.dailyLogService.dailyLogs();

    const daysDiff =
      Math.round(
        (endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24),
      ) + 1;

    for (let i = 0; i < daysDiff; i++) {
      const currentDate = new Date(startDate);
      currentDate.setDate(startDate.getDate() + i);
      const dateStr = `${currentDate.getFullYear()}-${String(currentDate.getMonth() + 1).padStart(2, '0')}-${String(currentDate.getDate()).padStart(2, '0')}`;
      const dayName = dayNames[currentDate.getDay()];

      const existingLog = existingLogs.find((log) => log.date === dateStr);

      const isDayOff = existingLog?.comment?.toLowerCase() === 'day off';
      const isEmptyTime =
        !!existingLog &&
        existingLog.timeIn === '00:00:00' &&
        existingLog.timeOut === '00:00:00' &&
        !isDayOff;

      const hasRealTimes = !!existingLog && !isEmptyTime && !isDayOff;

      forms.push({
        dayName,
        date: dateStr,
        existingId: existingLog?.id,
        formGroup: this.fb.group({
          isDayOff: [isDayOff],
          timeIn: [{ value: isEmptyTime ? '' : (existingLog?.timeIn?.substring(0, 5) ?? ''), disabled: isDayOff }],
          timeOut: [{ value: isEmptyTime ? '' : (existingLog?.timeOut?.substring(0, 5) ?? ''), disabled: isDayOff }],
          mileage: [
            { value: existingLog?.mileage || 0, disabled: !hasRealTimes },
            [Validators.min(0)],
          ],
          tollCharge: [
            { value: existingLog?.tollCharge || 0, disabled: !hasRealTimes },
            [Validators.min(0)],
          ],
          parkingFee: [
            { value: existingLog?.parkingFee || 0, disabled: !hasRealTimes },
            [Validators.min(0)],
          ],
          otherCharges: [
            { value: existingLog?.otherCharges || 0, disabled: !hasRealTimes },
            [Validators.min(0)],
          ],
          comment: [
            { value: existingLog?.comment || '', disabled: !hasRealTimes },
            [Validators.maxLength(500)],
          ],
        }),
      });

      const lastForm = forms[forms.length - 1];
      lastForm.formGroup.get('isDayOff')?.valueChanges.subscribe((isDayOff) => {
        this.handleDayOffChange(lastForm.formGroup, isDayOff ?? false);
      });

      this.subscribeToTimeChanges(lastForm.formGroup);
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
    } else {
      formGroup.get('timeIn')?.enable();
      formGroup.get('timeOut')?.enable();
      formGroup.get('comment')?.setValue('');
      // Other fields stay disabled until Time In and Time Out are entered
    }
  }

  private subscribeToTimeChanges(formGroup: FormGroup): void {
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
      } else {
        ['mileage', 'tollCharge', 'parkingFee', 'otherCharges'].forEach((field) => {
          formGroup.get(field)?.setValue(0);
          formGroup.get(field)?.disable();
        });
        formGroup.get('comment')?.setValue('');
        formGroup.get('comment')?.disable();
      }
    };

    formGroup.get('timeIn')?.valueChanges.subscribe(onTimeChange);
    formGroup.get('timeOut')?.valueChanges.subscribe(onTimeChange);
  }

  onSubmit(): void {
    const weeklyLog = this.weeklyLog();
    if (!weeklyLog) return;

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
          timeIn: isDayOff ? '00:00' : formGroup.get('timeIn')?.value || '00:00',
          timeOut: isDayOff ? '00:00' : formGroup.get('timeOut')?.value || '00:00',
          mileage: isDayOff ? 0 : formGroup.get('mileage')?.value || 0,
          tollCharge: isDayOff ? 0 : formGroup.get('tollCharge')?.value || 0,
          parkingFee: isDayOff ? 0 : formGroup.get('parkingFee')?.value || 0,
          otherCharges: isDayOff ? 0 : formGroup.get('otherCharges')?.value || 0,
          comment: isDayOff ? 'Day off' : formGroup.get('comment')?.value || '',
        };
      });

    const request: UpdateDailyLogsRequest = { dailyLogs };

    this.dailyLogService.updateDailyLogs(weeklyLog.id, request).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        if (response.success) {
          this.toastService.success('Timesheet updated successfully!');
          this.router.navigate(['/dashboard/week', weeklyLog.id]);
        } else {
          this.toastService.error(
            response.message || 'Failed to update timesheet',
          );
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        this.cdr.detectChanges();
        this.toastService.error(
          error.error?.message || 'Failed to update timesheet',
        );
      },
    });
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
