import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { PagedResult } from '../../core/models/common.models';
import type {
  CreateTopicCommand,
  TopicDto,
  TopicsFilter,
  UpdateTopicCommand,
} from '../../core/models/topic.models';

@Injectable({ providedIn: 'root' })
export class TopicsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/topics`;

  getTopics(params: TopicsFilter): Observable<PagedResult<TopicDto>> {
    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize);

    if (params.query?.trim()) {
      httpParams = httpParams.set('query', params.query.trim());
    }
    if (params.statusCodeName) {
      httpParams = httpParams.set('statusCodeName', params.statusCodeName);
    }
    if (params.createdByUserId) {
      httpParams = httpParams.set('createdByUserId', params.createdByUserId);
    }
    if (params.creatorTypeCodeName) {
      httpParams = httpParams.set('creatorTypeCodeName', params.creatorTypeCodeName);
    }
    if (params.sort) {
      httpParams = httpParams.set('sort', params.sort);
    }

    return this.http.get<PagedResult<TopicDto>>(this.baseUrl, { params: httpParams });
  }

  getTopicById(id: string): Observable<TopicDto> {
    return this.http.get<TopicDto>(`${this.baseUrl}/${id}`);
  }

  createTopic(command: CreateTopicCommand): Observable<TopicDto> {
    return this.http.post<TopicDto>(this.baseUrl, command);
  }

  patchTopic(id: string, command: UpdateTopicCommand): Observable<TopicDto> {
    return this.http.patch<TopicDto>(`${this.baseUrl}/${id}`, command);
  }

  deleteTopic(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
