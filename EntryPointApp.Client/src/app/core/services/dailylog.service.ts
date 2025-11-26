import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { DailyLog } from '../models/dailylog.model';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

@Injectable({
  providedIn: 'root',
})
export class DailyLogService {
  private http = inject(HttpClient);

  getDailyLogs(weeklyLogId: number): Observable<DailyLog[]> {
    const apiUrl = `http://localhost:5077/api/weeklylogs/${weeklyLogId}/dailylog`;

    return this.http.get<ApiResponse<DailyLog[]>>(apiUrl).pipe(
      map((response) => {
        if (response.success && response.data) {
          return response.data;
        }
        throw new Error(response.message || 'Failed to fetch DailyLogs');
      })
    );
  }
}
