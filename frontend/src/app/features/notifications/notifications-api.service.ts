import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { PagedResult } from '../../core/models/common.models';
import type { NotificationDto, NotificationsFilter } from '../../core/models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/notifications`;

  getNotifications(params: NotificationsFilter): Observable<PagedResult<NotificationDto>> {
    let httpParams = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);
    if (params.isRead !== undefined) {
      httpParams = httpParams.set('isRead', params.isRead);
    }
    return this.http.get<PagedResult<NotificationDto>>(this.baseUrl, { params: httpParams });
  }

  markAsRead(id: string): Observable<void> {
    return this.http
      .put<HttpResponse<void>>(`${this.baseUrl}/${id}/read`, {}, { observe: 'response' })
      .pipe(map(() => undefined));
  }

  markAllAsRead(): Observable<void> {
    return this.http
      .put<HttpResponse<void>>(`${this.baseUrl}/read-all`, {}, { observe: 'response' })
      .pipe(map(() => undefined));
  }
}
