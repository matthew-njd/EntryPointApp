import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DailyLogService } from '../../core/services/dailylog.service';
import { DailyLog } from '../../core/models/dailylog.model';
import { Nav } from '../../shared/nav/nav';
import { DatePipe } from '@angular/common';
import { Card } from '../../shared/card/card';

@Component({
  selector: 'app-dailylogs',
  imports: [Nav, DatePipe, Card],
  templateUrl: './dailylogs.html',
  styleUrl: './dailylogs.css',
})
export class Dailylogs implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dailyLogService = inject(DailyLogService);
  private cdr = inject(ChangeDetectorRef);

  dailyLogs: DailyLog[] = [];
  weeklyLogId: number | null = null;
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.weeklyLogId = Number(id);
        this.loadDailyLogs(this.weeklyLogId);
      } else {
        this.errorMessage = 'Invalid weeklylog ID';
        this.isLoading = false;
      }
    });
  }

  loadDailyLogs(weeklyLogId: number): void {
    this.isLoading = true;
    this.dailyLogService.getDailyLogs(weeklyLogId).subscribe({
      next: (dailyLogs) => {
        console.log('Dailylogs loaded:', dailyLogs);
        this.dailyLogs = dailyLogs;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load dailylogs:', error);
        this.errorMessage = 'Failed to load dailylogs. Please try again.';
        this.isLoading = false;
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  getTotalHours(): number {
    return this.dailyLogs.reduce((sum, log) => sum + log.hours, 0);
  }

  getTotalCharges(): number {
    return this.dailyLogs.reduce(
      (sum, log) => sum + log.tollCharge + log.parkingFee + log.otherCharges,
      0
    );
  }

  getTotalMileage(): number {
    return this.dailyLogs.reduce((sum, log) => sum + log.mileage, 0);
  }
}
