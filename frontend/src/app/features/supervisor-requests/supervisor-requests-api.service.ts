import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { PagedResult } from '../../core/models/common.models';
import type {
  SupervisorRequestDetailDto,
  SupervisorRequestDto,
  SupervisorRequestsFilter,
} from '../../core/models/supervisor-request.models';

@Injectable({ providedIn: 'root' })
export class SupervisorRequestsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/supervisor-requests`;

  getRequests(params: SupervisorRequestsFilter): Observable<PagedResult<SupervisorRequestDto>> {
    let httpParams = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);
    if (params.sort) {
      httpParams = httpParams.set('sort', params.sort);
    }
    if (params.createdFromUtc) {
      httpParams = httpParams.set('createdFromUtc', params.createdFromUtc);
    }
    if (params.createdToUtc) {
      httpParams = httpParams.set('createdToUtc', params.createdToUtc);
    }
    return this.http.get<PagedResult<SupervisorRequestDto>>(this.baseUrl, { params: httpParams });
  }

  getById(id: string): Observable<SupervisorRequestDetailDto> {
    return this.http.get<SupervisorRequestDetailDto>(`${this.baseUrl}/${id}`);
  }

  create(teacherUserId: string, comment?: string): Observable<SupervisorRequestDto> {
    return this.http.post<SupervisorRequestDto>(this.baseUrl, { teacherUserId, comment });
  }

  approve(id: string): Observable<SupervisorRequestDto> {
    return this.http.put<SupervisorRequestDto>(`${this.baseUrl}/${id}/approve`, {});
  }

  reject(id: string, comment: string): Observable<SupervisorRequestDto> {
    return this.http.put<SupervisorRequestDto>(`${this.baseUrl}/${id}/reject`, { comment });
  }

  cancel(id: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/cancel`, {});
  }
}
