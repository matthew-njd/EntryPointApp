import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { WeeklyLog } from '../models/weeklylog.model';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

@Injectable({
  providedIn: 'root',
})
export class TimesheetService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5077/api/Timesheet';

  getTimesheets(
    page: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<WeeklyLog>> {
    return this.http
      .get<ApiResponse<PagedResult<WeeklyLog>>>(
        `${this.apiUrl}?page=${page}&pageSize=${pageSize}`
      )
      .pipe(
        map((response) => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || 'Failed to fetch timesheets');
        })
      );
  }
}
