import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PayrollSummaryResponse } from '../models/admin.model';

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

@Injectable({ providedIn: 'root' })
export class AdminSummaryService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/admin/payroll-summary`;

  getSummary(dateFrom: string, dateTo: string): Observable<ApiResponse<PayrollSummaryResponse>> {
    return this.http.get<ApiResponse<PayrollSummaryResponse>>(this.baseUrl, {
      params: { dateFrom, dateTo },
    });
  }

  downloadSummaryExcel(dateFrom: string, dateTo: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/export`, {
      params: { dateFrom, dateTo },
      responseType: 'blob',
    });
  }
}
