import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  UserDto,
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

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5077/api/admin';

  private _users = signal<UserDto[]>([]);
  private _isLoading = signal(false);
  private _error = signal<string | null>(null);

  readonly users = this._users.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();

  readonly totalUsers = computed(
    () => this._users().filter((u) => u.role === 'User').length,
  );

  readonly totalManagers = computed(
    () => this._users().filter((u) => u.role === 'Manager').length,
  );

  readonly totalAdmins = computed(
    () => this._users().filter((u) => u.role === 'Admin').length,
  );

  readonly activeUsers = computed(
    () => this._users().filter((u) => u.isActive).length,
  );

  loadUsers() {
    this._isLoading.set(true);
    this._error.set(null);

    this.http.get<ApiResponse<UserDto[]>>(`${this.apiUrl}/users`).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this._users.set(response.data);
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

  clear() {
    this._users.set([]);
    this._error.set(null);
  }
}
