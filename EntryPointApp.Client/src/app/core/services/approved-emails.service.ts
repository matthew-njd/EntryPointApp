import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddApprovedEmailRequest, ApprovedEmailDto } from '../models/approved-emails.model';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

@Injectable({ providedIn: 'root' })
export class ApprovedEmailsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin/approved-emails`;

  getAll(): Observable<ApiResponse<ApprovedEmailDto[]>> {
    return this.http.get<ApiResponse<ApprovedEmailDto[]>>(this.apiUrl);
  }

  add(request: AddApprovedEmailRequest): Observable<ApiResponse<ApprovedEmailDto>> {
    return this.http.post<ApiResponse<ApprovedEmailDto>>(this.apiUrl, request);
  }

  remove(id: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`);
  }
}
