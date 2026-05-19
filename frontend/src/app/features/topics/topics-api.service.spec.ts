import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../environments/environment';
import { TopicsApiService } from './topics-api.service';

describe('TopicsApiService', () => {
  let service: TopicsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TopicsApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TopicsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getTopics отправляет GET с page и pageSize', () => {
    service.getTopics({ page: 3, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/topics` &&
        r.params.get('page') === '3' &&
        r.params.get('pageSize') === '20',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 3, pageSize: 20 });
  });

  it('getTopics включает query когда он не пустой', () => {
    service.getTopics({ page: 1, pageSize: 10, query: 'машинное обучение' }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/topics` &&
        r.params.get('query') === 'машинное обучение',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
  });

  it('getTopics не добавляет query когда он пробелы', () => {
    service.getTopics({ page: 1, pageSize: 10, query: '   ' }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/topics` &&
        !r.params.has('query'),
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
  });

  it('getTopics включает все опциональные фильтры когда они заданы', () => {
    service.getTopics({
      page: 1,
      pageSize: 10,
      creatorQuery: 'Иванов',
      statusCodeName: 'Active',
      createdByUserId: 'user-1',
      creatorTypeCodeName: 'Teacher',
      sort: 'titleAsc',
    }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/topics` &&
        r.params.get('creatorQuery') === 'Иванов' &&
        r.params.get('statusCodeName') === 'Active' &&
        r.params.get('createdByUserId') === 'user-1' &&
        r.params.get('creatorTypeCodeName') === 'Teacher' &&
        r.params.get('sort') === 'titleAsc',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
  });

  it('getTopics не добавляет опциональные фильтры когда они не заданы', () => {
    service.getTopics({ page: 1, pageSize: 10 }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/topics` &&
        !r.params.has('query') &&
        !r.params.has('creatorQuery') &&
        !r.params.has('statusCodeName') &&
        !r.params.has('createdByUserId') &&
        !r.params.has('creatorTypeCodeName') &&
        !r.params.has('sort'),
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
  });

  it('getTopics включает createdFromUtc и createdToUtc когда они заданы', () => {
    service.getTopics({
      page: 1,
      pageSize: 10,
      createdFromUtc: '2025-01-01T00:00:00Z',
      createdToUtc: '2025-12-31T23:59:59Z',
    }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/topics` &&
        r.params.get('createdFromUtc') === '2025-01-01T00:00:00Z' &&
        r.params.get('createdToUtc') === '2025-12-31T23:59:59Z',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
  });

  it('getTopicById отправляет GET по id', () => {
    service.getTopicById('topic-55').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/topics/topic-55`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'topic-55' } as any);
  });

  it('createTopic отправляет POST с телом команды', () => {
    const command = { title: 'Новая тема', description: null, creatorTypeCodeName: 'Teacher' as const };
    service.createTopic(command).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/topics`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(command);
    req.flush({ id: 'new-topic' } as any);
  });

  it('patchTopic отправляет PATCH с id и телом', () => {
    const command = { title: 'Изменённая тема' };
    service.patchTopic('topic-7', command).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/topics/topic-7`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual(command);
    req.flush({ id: 'topic-7' } as any);
  });

  it('deleteTopic отправляет DELETE по id', () => {
    service.deleteTopic('topic-9').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/topics/topic-9`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
