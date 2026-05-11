import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import {
  DailyLog,
  DailyLogRequest,
  ReceiptResponse,
  UpdateDailyLogsRequest,
} from '../models/dailylog.model';

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

    const apiUrl = `${environment.apiUrl}/weeklylogs/${weeklyLogId}/dailylog`;

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
    const apiUrl = `${environment.apiUrl}/weeklylogs/${weeklyLogId}/dailylog/batch`;
    return this.http.post<ApiResponse<DailyLog[]>>(apiUrl, requests);
  }

  updateDailyLogs(weeklyLogId: number, request: UpdateDailyLogsRequest) {
    const apiUrl = `${environment.apiUrl}/weeklylogs/${weeklyLogId}/dailylog`;
    return this.http.put<ApiResponse<DailyLog[]>>(apiUrl, request);
  }

  uploadReceipt(weeklyLogId: number, dailyLogId: number, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    const url = `${environment.apiUrl}/weeklylogs/${weeklyLogId}/dailylog/${dailyLogId}/receipts`;
    return this.http.post<ApiResponse<ReceiptResponse>>(url, formData);
  }

  deleteReceipt(weeklyLogId: number, dailyLogId: number, attachmentId: number) {
    const url = `${environment.apiUrl}/weeklylogs/${weeklyLogId}/dailylog/${dailyLogId}/receipts/${attachmentId}`;
    return this.http.delete<ApiResponse<void>>(url);
  }

  downloadReceipt(weeklyLogId: number, dailyLogId: number, attachmentId: number) {
    const url = `${environment.apiUrl}/weeklylogs/${weeklyLogId}/dailylog/${dailyLogId}/receipts/${attachmentId}`;
    return this.http.get(url, { responseType: 'blob' });
  }

  clear() {
    this._dailyLogs.set([]);
    this._error.set(null);
  }
}
