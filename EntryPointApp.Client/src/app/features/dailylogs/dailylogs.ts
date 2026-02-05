import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DailyLogService } from '../../core/services/dailylog.service';
import { Nav } from '../../shared/nav/nav';
import { DatePipe } from '@angular/common';
import { Card } from '../../shared/card/card';
import { Footer } from '../../shared/footer/footer';
import { WeeklyLogService } from '../../core/services/weeklog.service';
import { WeeklyLog } from '../../core/models/weeklylog.model';

@Component({
  selector: 'app-dailylogs',
  standalone: true,
  imports: [Nav, DatePipe, Card, Footer],
  templateUrl: './dailylogs.html',
  styleUrl: './dailylogs.css',
})
export class Dailylogs {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  readonly service = inject(DailyLogService);
  private weeklyLogService = inject(WeeklyLogService);

  weeklyLogId = toSignal(this.route.paramMap);
  weeklyLog = signal<WeeklyLog | null>(null);
  isLoadingWeeklyLog = signal(false);

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
          }
          this.isLoadingWeeklyLog.set(false);
        },
        error: (err) => {
          console.error('Failed to load weekly log', err);
          this.isLoadingWeeklyLog.set(false);
        },
      });
    }
  });

  totalHours = computed(() =>
    this.service.dailyLogs().reduce((sum, log) => sum + log.hours, 0),
  );

  totalMileage = computed(() =>
    this.service.dailyLogs().reduce((sum, log) => sum + log.mileage, 0),
  );

  totalCharges = computed(() =>
    this.service
      .dailyLogs()
      .reduce(
        (sum, log) => sum + log.tollCharge + log.parkingFee + log.otherCharges,
        0,
      ),
  );

  editTimesheet() {
    const id = this.weeklyLogId()?.get('id');
    if (id) {
      this.router.navigate(['/dashboard/week', id, 'edit']);
    }
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}
