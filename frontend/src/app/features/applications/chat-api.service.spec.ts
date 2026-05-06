import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../environments/environment';
import type { ChatMessageDto } from '../../core/models/application.models';
import { ChatApiService } from './chat-api.service';

describe('ChatApiService', () => {
  let service: ChatApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ChatApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ChatApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getMessages делает GET без query при отсутствии params', () => {
    service.getMessages('app-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/messages`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([] as ChatMessageDto[]);
  });

  it('getMessages передаёт afterId и limit', () => {
    service.getMessages('app-1', { afterId: 'cursor-id', limit: 25 }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/applications/app-1/messages` &&
        r.params.get('afterId') === 'cursor-id' &&
        r.params.get('limit') === '25',
    );
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('sendMessage отправляет POST с content', () => {
    service.sendMessage('app-1', 'Текст').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/messages`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ content: 'Текст' });
    req.flush({ id: 'm1' } as ChatMessageDto);
  });

  it('markAllAsRead отправляет PUT на read-all', () => {
    service.markAllAsRead('app-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/messages/read-all`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({});
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
