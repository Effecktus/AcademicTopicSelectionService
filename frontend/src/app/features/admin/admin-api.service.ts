import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { PagedResult } from '../../core/models/common.models';
import type {
  AdminAnalyticsDto,
  CreateUserBody,
  DepartmentDto,
  UserListItemDto,
  UserRoleDto,
} from '../../core/models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  // Users
  getUsers(params: {
    page: number;
    pageSize: number;
    roleId?: string | null;
    query?: string | null;
  }): Observable<PagedResult<UserListItemDto>> {
    let p = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);
    if (params.roleId) p = p.set('roleId', params.roleId);
    if (params.query?.trim()) p = p.set('query', params.query.trim());
    return this.http.get<PagedResult<UserListItemDto>>(`${this.apiUrl}/users`, { params: p });
  }

  createUser(body: CreateUserBody): Observable<{ userId: string; email: string; role: string }> {
    return this.http.post<{ userId: string; email: string; role: string }>(
      `${this.apiUrl}/users`,
      body,
    );
  }

  // Dictionaries
  getDepartments(): Observable<DepartmentDto[]> {
    return this.http.get<DepartmentDto[]>(`${this.apiUrl}/departments`);
  }

  getUserRoles(): Observable<UserRoleDto[]> {
    return this.http
      .get<PagedResult<UserRoleDto>>(`${this.apiUrl}/user-roles`)
      .pipe(map((result: PagedResult<UserRoleDto>) => result.items));
  }

  // Analytics
  getAnalytics(year?: number | null): Observable<AdminAnalyticsDto> {
    let params = new HttpParams();
    if (year != null) params = params.set('year', year);
    return this.http.get<AdminAnalyticsDto>(`${this.apiUrl}/admin/analytics`, { params });
  }

  // Export
  exportExcel(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/admin/export`, {
      params: new HttpParams().set('format', 'excel'),
      responseType: 'blob',
    });
  }

  exportCsv(dataset: 'graduate-works' | 'applications' | 'users'): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/admin/export`, {
      params: new HttpParams().set('format', 'csv').set('dataset', dataset),
      responseType: 'blob',
    });
  }
}
