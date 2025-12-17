import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import {
  LoginRequest,
  LoginResponse,
  ApiResponse,
  UserResponse,
  RegisterRequest,
  RegisterResponse,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from '../models/auth.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);
  private apiUrl = 'http://localhost:5077/api/Auth';

  private isBrowser: boolean;

  // track current user state
  private currentUserSubject = new BehaviorSubject<UserResponse | null>(
    this.getUserFromStorage()
  );
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor() {
    this.isBrowser = isPlatformBrowser(this.platformId);
  }

  login(loginRequest: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http
      .post<ApiResponse<LoginResponse>>(`${this.apiUrl}/login`, loginRequest)
      .pipe(
        map((response) => {
          if (response.success && response.data) {
            this.storeAuthData(response.data);
            this.currentUserSubject.next(response.data.user);
            return response;
          }
          throw new Error(response.message || 'Login failed');
        }),
        catchError(this.handleError)
      );
  }

  forgotPassword(email: string): Observable<string> {
    const request: ForgotPasswordRequest = { email };
    return this.http
      .post<ApiResponse<void>>(`${this.apiUrl}/forgot-password`, request)
      .pipe(
        map((response) => {
          if (response.success) {
            return response.message;
          }
          throw new Error(response.message || 'Failed to send reset email');
        }),
        catchError(this.handleError)
      );
  }

  resetPassword(resetRequest: ResetPasswordRequest): Observable<string> {
    return this.http
      .post<ApiResponse<void>>(`${this.apiUrl}/reset-password`, resetRequest)
      .pipe(
        map((response) => {
          if (response.success) {
            return response.message;
          }
          throw new Error(response.message || 'Failed to reset password');
        }),
        catchError(this.handleError)
      );
  }

  register(
    registerRequest: RegisterRequest
  ): Observable<ApiResponse<RegisterResponse>> {
    return this.http
      .post<ApiResponse<RegisterResponse>>(
        `${this.apiUrl}/register`,
        registerRequest
      )
      .pipe(
        map((response) => {
          if (response.success && response.data) {
            this.storeAuthData(response.data);
            this.currentUserSubject.next(response.data.user);
            return response;
          }
          throw new Error(response.message || 'Registration failed');
        }),
        catchError(this.handleError)
      );
  }

  logout(): void {
    if (this.isBrowser) {
      localStorage.removeItem('access_token');
      localStorage.removeItem('refresh_token');
      localStorage.removeItem('token_expiry');
      localStorage.removeItem('current_user');
    }
    this.currentUserSubject.next(null);
  }

  isAuthenticated(): boolean {
    if (!this.isBrowser) {
      return false;
    }

    const token = this.getAccessToken();
    const expiry = localStorage.getItem('token_expiry');

    if (!token || !expiry) {
      return false;
    }

    return new Date(expiry) > new Date();
  }

  getAccessToken(): string | null {
    if (!this.isBrowser) {
      return null;
    }
    return localStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    if (!this.isBrowser) {
      return null;
    }
    return localStorage.getItem('refresh_token');
  }

  getCurrentUser(): UserResponse | null {
    return this.currentUserSubject.value;
  }

  private storeAuthData(loginResponse: LoginResponse): void {
    if (!this.isBrowser) {
      return;
    }

    localStorage.setItem('access_token', loginResponse.accessToken);
    localStorage.setItem('refresh_token', loginResponse.refreshToken);
    localStorage.setItem('token_expiry', loginResponse.expiresAt);
    localStorage.setItem('current_user', JSON.stringify(loginResponse.user));
  }

  private getUserFromStorage(): UserResponse | null {
    if (!this.isBrowser) {
      return null;
    }

    const userJson = localStorage.getItem('current_user');
    if (userJson) {
      try {
        return JSON.parse(userJson);
      } catch {
        return null;
      }
    }
    return null;
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unknown error occurred';

    if (error.error instanceof ErrorEvent) {
      errorMessage = `Error: ${error.error.message}`;
    } else {
      if (error.error?.message) {
        errorMessage = error.error.message;
      } else if (error.error?.errors && error.error.errors.length > 0) {
        errorMessage = error.error.errors.join(', ');
      } else {
        errorMessage = `Server error: ${error.status} - ${error.message}`;
      }
    }

    return throwError(() => new Error(errorMessage));
  }
}
