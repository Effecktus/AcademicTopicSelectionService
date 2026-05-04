import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { PagedResult } from '../../core/models/common.models';
import type {
  ApplicationsFilter,
  CreateApplicationCommand,
  StudentApplicationDetailDto,
  StudentApplicationDto,
} from '../../core/models/application.models';

@Injectable({ providedIn: 'root' })
export class ApplicationsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/applications`;

  getApplications(params: ApplicationsFilter): Observable<PagedResult<StudentApplicationDto>> {
    const httpParams = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);
    return this.http.get<PagedResult<StudentApplicationDto>>(this.baseUrl, { params: httpParams });
  }

  getById(id: string): Observable<StudentApplicationDetailDto> {
    return this.http.get<StudentApplicationDetailDto>(`${this.baseUrl}/${id}`);
  }

  create(command: CreateApplicationCommand): Observable<StudentApplicationDto> {
    return this.http.post<StudentApplicationDto>(this.baseUrl, command);
  }

  approve(id: string, comment?: string | null): Observable<StudentApplicationDto> {
    const trimmed = comment?.trim();
    const body = trimmed ? { comment: trimmed } : {};
    return this.http.put<StudentApplicationDto>(`${this.baseUrl}/${id}/approve`, body);
  }

  reject(id: string, comment: string): Observable<StudentApplicationDto> {
    return this.http.put<StudentApplicationDto>(`${this.baseUrl}/${id}/reject`, { comment });
  }

  departmentHeadApprove(id: string, comment?: string | null): Observable<StudentApplicationDto> {
    const trimmed = comment?.trim();
    const body = trimmed ? { comment: trimmed } : {};
    return this.http.put<StudentApplicationDto>(`${this.baseUrl}/${id}/department-head-approve`, body);
  }

  departmentHeadReject(id: string, comment: string): Observable<StudentApplicationDto> {
    return this.http.put<StudentApplicationDto>(`${this.baseUrl}/${id}/department-head-reject`, { comment });
  }

  cancel(id: string): Observable<StudentApplicationDto> {
    return this.http.put<StudentApplicationDto>(`${this.baseUrl}/${id}/cancel`, {});
  }
}
