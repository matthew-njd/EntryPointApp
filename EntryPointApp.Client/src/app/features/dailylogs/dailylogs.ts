import { Component, computed, effect, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DailyLogService } from '../../core/services/dailylog.service';
import { Nav } from '../../shared/nav/nav';
import { DatePipe } from '@angular/common';
import { Card } from '../../shared/card/card';

@Component({
  selector: 'app-dailylogs',
  standalone: true,
  imports: [Nav, DatePipe, Card],
  templateUrl: './dailylogs.html',
  styleUrl: './dailylogs.css',
})
export class Dailylogs {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  readonly service = inject(DailyLogService);

  weeklyLogId = toSignal(this.route.paramMap);

  loadEffect = effect(() => {
    const id = this.weeklyLogId()?.get('id');
    if (id) {
      this.service.loadDailyLogs(+id);
    }
  });

  totalHours = computed(() =>
    this.service.dailyLogs().reduce((sum, log) => sum + log.hours, 0)
  );

  totalMileage = computed(() =>
    this.service.dailyLogs().reduce((sum, log) => sum + log.mileage, 0)
  );

  totalCharges = computed(() =>
    this.service
      .dailyLogs()
      .reduce(
        (sum, log) => sum + log.tollCharge + log.parkingFee + log.otherCharges,
        0
      )
  );

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}
