import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { ChatMessageDto } from '../../core/models/application.models';

@Injectable({ providedIn: 'root' })
export class ChatApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/applications`;

  getMessages(applicationId: string, params?: { afterId?: string; limit?: number }): Observable<ChatMessageDto[]> {
    let httpParams = new HttpParams();
    if (params?.afterId) {
      httpParams = httpParams.set('afterId', params.afterId);
    }
    if (params?.limit !== undefined) {
      httpParams = httpParams.set('limit', params.limit);
    }
    return this.http.get<ChatMessageDto[]>(`${this.baseUrl}/${applicationId}/messages`, { params: httpParams });
  }

  sendMessage(applicationId: string, content: string): Observable<ChatMessageDto> {
    return this.http.post<ChatMessageDto>(`${this.baseUrl}/${applicationId}/messages`, { content });
  }

  markAllAsRead(applicationId: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${applicationId}/messages/read-all`, {});
  }
}
