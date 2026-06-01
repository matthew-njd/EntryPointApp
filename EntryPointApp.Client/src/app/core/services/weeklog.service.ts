import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { WeeklyLog, WeeklyLogRequest, WeeklyLogSummary } from '../models/weeklylog.model';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PagedResult<T> {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface WeeklyLogPagedResult extends PagedResult<WeeklyLog> {
  summary: WeeklyLogSummary;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

const defaultSummary: WeeklyLogSummary = {
  totalApproved: 0,
  totalPending: 0,
  totalDenied: 0,
  totalDraft: 0,
};

@Injectable({ providedIn: 'root' })
export class WeeklyLogService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/weeklylog`;

  private _weeklyLogs = signal<WeeklyLog[]>([]);
  private _isLoading = signal(false);
  private _error = signal<string | null>(null);
  private _totalCount = signal(0);
  private _page = signal(1);
  private _pageSize = signal(10);
  private _summary = signal<WeeklyLogSummary>({ ...defaultSummary });

  readonly weeklyLogs = this._weeklyLogs.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly page = this._page.asReadonly();
  readonly pageSize = this._pageSize.asReadonly();

  readonly totalPages = computed(() =>
    Math.ceil(this._totalCount() / this._pageSize()),
  );

  readonly totalApproved = computed(() => this._summary().totalApproved);
  readonly totalPending = computed(() => this._summary().totalPending);
  readonly totalDenied = computed(() => this._summary().totalDenied);
  readonly totalDraft = computed(() => this._summary().totalDraft);

  loadWeeklyLogs(page?: number, pageSize?: number) {
    this._isLoading.set(true);
    this._error.set(null);

    const p = page ?? this._page();
    const ps = pageSize ?? this._pageSize();

    let params = new HttpParams()
      .set('page', p.toString())
      .set('pageSize', ps.toString());

    this.http
      .get<ApiResponse<WeeklyLogPagedResult>>(this.apiUrl, { params })
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this._weeklyLogs.set(response.data.data);
            this._totalCount.set(response.data.totalCount);
            this._page.set(response.data.page);
            this._pageSize.set(response.data.pageSize);
            this._summary.set(response.data.summary ?? { ...defaultSummary });
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

  getWeeklyLogById(id: number): Observable<ApiResponse<WeeklyLog>> {
    return this.http.get<ApiResponse<WeeklyLog>>(`${this.apiUrl}/${id}`);
  }

  getDateRanges(): Observable<ApiResponse<WeeklyLogPagedResult>> {
    const params = new HttpParams()
      .set('page', '1')
      .set('pageSize', '52');
    return this.http.get<ApiResponse<WeeklyLogPagedResult>>(this.apiUrl, { params });
  }

  createWeeklyLog(request: WeeklyLogRequest) {
    return this.http.post<ApiResponse<WeeklyLog>>(this.apiUrl, request);
  }

  clear() {
    this._weeklyLogs.set([]);
    this._error.set(null);
  }
}
