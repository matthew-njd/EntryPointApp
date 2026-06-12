import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdminTimesheetDetailResponse,
  AdminTimesheetResponse,
  UserDto,
  UserPagedResponse,
  UserSummary,
  UserRateDto,
  SetUserRateRequest,
  UpdateUserRoleRequest,
  AssignManagerRequest,
  UserRole,
} from '../models/admin.model';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

const defaultSummary: UserSummary = {
  totalUsers: 0,
  totalManagers: 0,
  totalAdmins: 0,
  activeUsers: 0,
};

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin`;

  private _users = signal<UserDto[]>([]);
  private _isLoading = signal(false);
  private _error = signal<string | null>(null);
  private _summary = signal<UserSummary>({ ...defaultSummary });
  private _totalCount = signal(0);
  private _page = signal(1);
  private _pageSize = signal(20);

  readonly users = this._users.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly page = this._page.asReadonly();
  readonly pageSize = this._pageSize.asReadonly();

  readonly totalPages = computed(() =>
    Math.ceil(this._totalCount() / this._pageSize()),
  );

  readonly totalUsers = computed(() => this._summary().totalUsers);
  readonly totalManagers = computed(() => this._summary().totalManagers);
  readonly totalAdmins = computed(() => this._summary().totalAdmins);
  readonly activeUsers = computed(() => this._summary().activeUsers);

  loadUsers(
    page?: number,
    pageSize?: number,
    roleFilter: string = 'All',
    statusFilter: string = 'All',
    search: string = '',
  ) {
    this._isLoading.set(true);
    this._error.set(null);

    const p = page ?? this._page();
    const ps = pageSize ?? this._pageSize();

    let params = new HttpParams()
      .set('page', p.toString())
      .set('pageSize', ps.toString());

    if (roleFilter && roleFilter !== 'All') {
      params = params.set('role', roleFilter);
    }

    if (statusFilter && statusFilter !== 'All') {
      params = params.set('status', statusFilter);
    }

    if (search) {
      params = params.set('search', search);
    }

    this.http.get<ApiResponse<UserPagedResponse>>(`${this.apiUrl}/users`, { params }).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this._users.set(response.data.data);
          this._totalCount.set(response.data.totalCount);
          this._page.set(response.data.page);
          this._pageSize.set(response.data.pageSize);
          this._summary.set(response.data.summary ?? { ...defaultSummary });
        } else {
          this._error.set(response.message || 'Failed to fetch users');
          this._users.set([]);
        }
        this._isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this._error.set('Failed to fetch users');
        this._users.set([]);
        this._isLoading.set(false);
      },
    });
  }

  getUserById(userId: number): Observable<ApiResponse<UserDto>> {
    return this.http.get<ApiResponse<UserDto>>(
      `${this.apiUrl}/users/${userId}`,
    );
  }

  updateUserRole(
    userId: number,
    role: UserRole,
  ): Observable<ApiResponse<UserDto>> {
    const request: UpdateUserRoleRequest = { role };
    return this.http.put<ApiResponse<UserDto>>(
      `${this.apiUrl}/users/${userId}/role`,
      request,
    );
  }

  assignManager(
    userId: number,
    managerId: number,
  ): Observable<ApiResponse<UserDto>> {
    const request: AssignManagerRequest = { managerId };
    return this.http.put<ApiResponse<UserDto>>(
      `${this.apiUrl}/users/${userId}/manager`,
      request,
    );
  }

  removeManager(userId: number): Observable<ApiResponse<UserDto>> {
    return this.http.delete<ApiResponse<UserDto>>(
      `${this.apiUrl}/users/${userId}/manager`,
    );
  }

  deactivateUser(userId: number): Observable<ApiResponse<UserDto>> {
    return this.http.put<ApiResponse<UserDto>>(
      `${this.apiUrl}/users/${userId}/deactivate`,
      {},
    );
  }

  activateUser(userId: number): Observable<ApiResponse<UserDto>> {
    return this.http.put<ApiResponse<UserDto>>(
      `${this.apiUrl}/users/${userId}/activate`,
      {},
    );
  }

  getUserRates(userId: number): Observable<ApiResponse<UserRateDto[]>> {
    return this.http.get<ApiResponse<UserRateDto[]>>(
      `${this.apiUrl}/users/${userId}/rates`,
    );
  }

  getCurrentUserRate(userId: number): Observable<ApiResponse<UserRateDto>> {
    return this.http.get<ApiResponse<UserRateDto>>(
      `${this.apiUrl}/users/${userId}/rates/current`,
    );
  }

  setUserRate(
    userId: number,
    request: SetUserRateRequest,
  ): Observable<ApiResponse<UserRateDto>> {
    return this.http.post<ApiResponse<UserRateDto>>(
      `${this.apiUrl}/users/${userId}/rates`,
      request,
    );
  }

  getUserTimesheets(userId: number): Observable<ApiResponse<AdminTimesheetResponse[]>> {
    return this.http.get<ApiResponse<AdminTimesheetResponse[]>>(
      `${this.apiUrl}/users/${userId}/timesheets`,
    );
  }

  getUserTimesheetDetail(userId: number, timesheetId: number): Observable<ApiResponse<AdminTimesheetDetailResponse>> {
    return this.http.get<ApiResponse<AdminTimesheetDetailResponse>>(
      `${this.apiUrl}/users/${userId}/timesheets/${timesheetId}`,
    );
  }

  clear() {
    this._users.set([]);
    this._error.set(null);
  }
}
