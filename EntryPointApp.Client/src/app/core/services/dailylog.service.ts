import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { DailyLog, DailyLogRequest } from '../models/dailylog.model';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

@Injectable({ providedIn: 'root' })
export class DailyLogService {
  private http = inject(HttpClient);

  private _dailyLogs = signal<DailyLog[]>([]);
  private _isLoading = signal(false);
  private _error = signal<string | null>(null);

  readonly dailyLogs = this._dailyLogs.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();

  loadDailyLogs(weeklyLogId: number) {
    this._isLoading.set(true);
    this._error.set(null);

    const apiUrl = `http://localhost:5077/api/weeklylogs/${weeklyLogId}/dailylog`;

    this.http.get<ApiResponse<DailyLog[]>>(apiUrl).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this._dailyLogs.set(response.data);
        } else {
          this._error.set(response.message || 'Failed to fetch DailyLogs');
          this._dailyLogs.set([]);
        }
        this._isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this._error.set('Failed to fetch DailyLogs');
        this._dailyLogs.set([]);
        this._isLoading.set(false);
      },
    });
  }

  createDailyLogsBatch(weeklyLogId: number, requests: DailyLogRequest[]) {
    const apiUrl = `http://localhost:5077/api/weeklylogs/${weeklyLogId}/dailylog/batch`;
    return this.http.post<ApiResponse<DailyLog[]>>(apiUrl, requests);
  }

  clear() {
    this._dailyLogs.set([]);
    this._error.set(null);
  }
}
