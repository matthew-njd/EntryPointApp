import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { WeeklyLog, WeeklyLogRequest } from '../models/weeklylog.model';

export interface PagedResult<T> {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

@Injectable({ providedIn: 'root' })
export class WeeklyLogService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5077/api/weeklylog';

  private _weeklyLogs = signal<WeeklyLog[]>([]);
  private _isLoading = signal(false);
  private _error = signal<string | null>(null);
  private _totalCount = signal(0);
  private _page = signal(1);
  private _pageSize = signal(10);

  readonly weeklyLogs = this._weeklyLogs.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly page = this._page.asReadonly();
  readonly pageSize = this._pageSize.asReadonly();

  readonly totalPages = computed(() =>
    Math.ceil(this._totalCount() / this._pageSize()),
  );

  loadWeeklyLogs(page?: number, pageSize?: number) {
    this._isLoading.set(true);
    this._error.set(null);

    const p = page ?? this._page();
    const ps = pageSize ?? this._pageSize();

    let params = new HttpParams()
      .set('page', p.toString())
      .set('pageSize', ps.toString());

    this.http
      .get<ApiResponse<PagedResult<WeeklyLog>>>(this.apiUrl, { params })
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this._weeklyLogs.set(response.data.data);
            this._totalCount.set(response.data.totalCount);
            this._page.set(response.data.page);
            this._pageSize.set(response.data.pageSize);
          } else {
            this._weeklyLogs.set([]);
            this._error.set(response.message || 'Failed to fetch WeeklyLogs');
          }
          this._isLoading.set(false);
        },
        error: (err) => {
          console.error(err);
          this._weeklyLogs.set([]);
          this._error.set('Failed to fetch WeeklyLogs');
          this._isLoading.set(false);
        },
      });
  }

  createWeeklyLog(request: WeeklyLogRequest) {
    return this.http.post<ApiResponse<WeeklyLog>>(this.apiUrl, request);
  }

  clear() {
    this._weeklyLogs.set([]);
    this._error.set(null);
  }
}
