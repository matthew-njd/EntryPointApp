import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  TeamTimesheetResponse,
  TeamTimesheetDetailResponse,
  ApproveTimesheetRequest,
  DenyTimesheetRequest,
  TimesheetStatus,
} from '../models/manager.model';

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
export class ManagerService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5077/api/manager';

  private _timesheets = signal<TeamTimesheetResponse[]>([]);
  private _isLoading = signal(false);
  private _error = signal<string | null>(null);
  private _totalCount = signal(0);
  private _page = signal(1);
  private _pageSize = signal(10);

  readonly timesheets = this._timesheets.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly page = this._page.asReadonly();
  readonly pageSize = this._pageSize.asReadonly();

  readonly totalPages = computed(() =>
    Math.ceil(this._totalCount() / this._pageSize()),
  );

  readonly totalPending = computed(
    () =>
      this._timesheets().filter((t) => t.status === TimesheetStatus.Pending)
        .length,
  );

  readonly totalApproved = computed(
    () =>
      this._timesheets().filter((t) => t.status === TimesheetStatus.Approved)
        .length,
  );

  readonly totalDenied = computed(
    () =>
      this._timesheets().filter((t) => t.status === TimesheetStatus.Denied)
        .length,
  );

  loadTimesheets(
    page?: number,
    pageSize?: number,
    statusFilter: string = 'All',
  ) {
    this._isLoading.set(true);
    this._error.set(null);

    const p = page ?? this._page();
    const ps = pageSize ?? this._pageSize();

    let params = new HttpParams()
      .set('page', p.toString())
      .set('pageSize', ps.toString());

    if (statusFilter && statusFilter !== 'All') {
      params = params.set('status', statusFilter);
    }

    this.http
      .get<ApiResponse<PagedResult<TeamTimesheetResponse>>>(
        `${this.apiUrl}/timesheets`,
        {
          params,
        },
      )
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this._timesheets.set(response.data.data);
            this._totalCount.set(response.data.totalCount);
            this._page.set(response.data.page);
            this._pageSize.set(response.data.pageSize);
          } else {
            this._error.set(response.message || 'Failed to fetch timesheets');
            this._timesheets.set([]);
          }
          this._isLoading.set(false);
        },
        error: (err) => {
          console.error(err);
          this._error.set('Failed to fetch timesheets');
          this._timesheets.set([]);
          this._isLoading.set(false);
        },
      });
  }

  loadPendingTimesheets() {
    this._isLoading.set(true);
    this._error.set(null);

    this.http
      .get<
        ApiResponse<TeamTimesheetResponse[]>
      >(`${this.apiUrl}/timesheets/pending`)
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this._timesheets.set(response.data);
          } else {
            this._error.set(response.message || 'Failed to fetch timesheets');
            this._timesheets.set([]);
          }
          this._isLoading.set(false);
        },
        error: (err) => {
          console.error(err);
          this._error.set('Failed to fetch timesheets');
          this._timesheets.set([]);
          this._isLoading.set(false);
        },
      });
  }

  getTimesheetDetail(
    timesheetId: number,
  ): Observable<ApiResponse<TeamTimesheetDetailResponse>> {
    return this.http.get<ApiResponse<TeamTimesheetDetailResponse>>(
      `${this.apiUrl}/timesheets/${timesheetId}`,
    );
  }

  approveTimesheet(
    timesheetId: number,
    request: ApproveTimesheetRequest,
  ): Observable<ApiResponse<TeamTimesheetResponse>> {
    return this.http.put<ApiResponse<TeamTimesheetResponse>>(
      `${this.apiUrl}/timesheets/${timesheetId}/approve`,
      request,
    );
  }

  denyTimesheet(
    timesheetId: number,
    request: DenyTimesheetRequest,
  ): Observable<ApiResponse<TeamTimesheetResponse>> {
    return this.http.put<ApiResponse<TeamTimesheetResponse>>(
      `${this.apiUrl}/timesheets/${timesheetId}/deny`,
      request,
    );
  }

  clear() {
    this._timesheets.set([]);
    this._error.set(null);
  }
}
