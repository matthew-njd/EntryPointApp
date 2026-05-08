import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PayrollScheduleEntry {
  id: number;
  dateFrom: string;
  dateTo: string;
  payrollDate: string;
}

export interface PayrollScheduleRequest {
  dateFrom: string;
  dateTo: string;
  payrollDate: string;
}

export interface PayrollScheduleLookup {
  payrollDate: string | null;
}

export interface PayrollScheduleImportStats {
  imported: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

@Injectable({ providedIn: 'root' })
export class PayrollScheduleService {
  private http = inject(HttpClient);
  private adminUrl = 'http://localhost:5077/api/admin/payroll-schedule';
  private lookupUrl = 'http://localhost:5077/api/payroll-schedule/lookup';

  getAll(): Observable<ApiResponse<PayrollScheduleEntry[]>> {
    return this.http.get<ApiResponse<PayrollScheduleEntry[]>>(this.adminUrl);
  }

  create(request: PayrollScheduleRequest): Observable<ApiResponse<PayrollScheduleEntry>> {
    return this.http.post<ApiResponse<PayrollScheduleEntry>>(this.adminUrl, request);
  }

  update(id: number, request: PayrollScheduleRequest): Observable<ApiResponse<PayrollScheduleEntry>> {
    return this.http.put<ApiResponse<PayrollScheduleEntry>>(`${this.adminUrl}/${id}`, request);
  }

  delete(id: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.adminUrl}/${id}`);
  }

  importCsv(file: File, replace: boolean): Observable<ApiResponse<PayrollScheduleImportStats>> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ApiResponse<PayrollScheduleImportStats>>(
      `${this.adminUrl}/import?replace=${replace}`,
      form
    );
  }

  lookup(dateFrom: string): Observable<ApiResponse<PayrollScheduleLookup>> {
    return this.http.get<ApiResponse<PayrollScheduleLookup>>(`${this.lookupUrl}?dateFrom=${dateFrom}`);
  }
}
