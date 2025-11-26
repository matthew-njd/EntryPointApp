import { HttpClient, HttpParams } from '@angular/common/http';
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
  private apiUrl = 'http://localhost:5077/api/weeklylog/{id}/dailylog';

  getDailyLogs(): Observable<DailyLog> {
    let params = new HttpParams();

    //params = params.set('id', 1);

    return this.http.get<ApiResponse<DailyLog>>(this.apiUrl, { params }).pipe(
      map((response) => {
        if (response.success && response.data) {
          return response.data;
        }
        throw new Error(response.message || 'Failed to fetch DailyLogs');
      })
    );
  }
}
