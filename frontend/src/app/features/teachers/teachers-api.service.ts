import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { PagedResult } from '../../core/models/common.models';
import type { TeacherDto } from '../../core/models/teacher.models';

export interface TeachersFilter {
  query?: string;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class TeachersApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/teachers`;

  getTeachers(params: TeachersFilter): Observable<PagedResult<TeacherDto>> {
    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize);

    if (params.query?.trim()) {
      httpParams = httpParams.set('query', params.query.trim());
    }

    return this.http.get<PagedResult<TeacherDto>>(this.baseUrl, { params: httpParams });
  }

  getTeacherById(id: string): Observable<TeacherDto> {
    return this.http.get<TeacherDto>(`${this.baseUrl}/${id}`);
  }
}
