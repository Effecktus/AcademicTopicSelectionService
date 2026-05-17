import { HttpClient, HttpHeaders, HttpParams, HttpRequest, HttpEvent } from '@angular/common/http';
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
import type { CreateGwBody } from '../../core/models/admin.models';

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

  // Admin-only methods
  create(body: CreateGwBody): Observable<GraduateWorkDto> {
    return this.http.post<GraduateWorkDto>(this.baseUrl, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getUploadUrl(id: string, fileType: GraduateWorkFileType): Observable<FileUrlDto> {
    return this.http.post<FileUrlDto>(`${this.baseUrl}/${id}/upload-url/${fileType}`, {});
  }

  uploadToStorage(url: string, file: File): Observable<HttpEvent<unknown>> {
    const req = new HttpRequest('PUT', url, file, {
      reportProgress: true,
      headers: new HttpHeaders({ 'Content-Type': 'application/octet-stream' }),
    });
    return this.http.request(req);
  }

  confirmUpload(id: string, fileType: GraduateWorkFileType, fileName: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/confirm-upload/${fileType}`, { fileName });
  }
}
