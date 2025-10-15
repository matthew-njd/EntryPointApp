import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface TimesheetResponse {
  id: number;
  userId: number;
  date: string;
  hoursWorked: number;
  milage: number;
  tollCharges: number;
  parkingFee: number;
  otherCharges: number;
  status?: string;
  comment: string;
}

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
  ): Observable<PagedResult<TimesheetResponse>> {
    return this.http
      .get<ApiResponse<PagedResult<TimesheetResponse>>>(
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
