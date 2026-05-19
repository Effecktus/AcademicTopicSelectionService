import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { DepartmentHeadAnalyticsDto } from '../../core/models/department-head.models';

@Injectable({ providedIn: 'root' })
export class DepartmentHeadApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/department-head`;

  getAnalytics(): Observable<DepartmentHeadAnalyticsDto> {
    return this.http.get<DepartmentHeadAnalyticsDto>(`${this.baseUrl}/analytics`);
  }
}
