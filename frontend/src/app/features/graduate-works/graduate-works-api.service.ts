import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { PagedResult } from '../../core/models/common.models';
import type {
  FileUrlDto,
  GraduateWorkDto,
  GraduateWorksFilter,
  GraduateWorkFileType,
} from '../../core/models/graduate-work.models';

@Injectable({ providedIn: 'root' })
export class GraduateWorksApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/graduate-works`;

  getAll(params: GraduateWorksFilter): Observable<PagedResult<GraduateWorkDto>> {
    let httpParams = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);

    if (params.year != null && params.year !== undefined) {
      httpParams = httpParams.set('year', params.year);
    }
    if (params.titleQuery?.trim()) {
      httpParams = httpParams.set('titleQuery', params.titleQuery.trim());
    }
    if (params.teacherId?.trim()) {
      httpParams = httpParams.set('teacherId', params.teacherId.trim());
    }
    if (params.teacherQuery?.trim()) {
      httpParams = httpParams.set('teacherQuery', params.teacherQuery.trim());
    }

    return this.http.get<PagedResult<GraduateWorkDto>>(this.baseUrl, { params: httpParams });
  }

  getById(id: string): Observable<GraduateWorkDto> {
    return this.http.get<GraduateWorkDto>(`${this.baseUrl}/${id}`);
  }

  getDownloadUrl(id: string, fileType: GraduateWorkFileType): Observable<FileUrlDto> {
    return this.http.get<FileUrlDto>(`${this.baseUrl}/${id}/download-url/${fileType}`);
  }
}
