import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../environments/environment';
import { SupervisorRequestsApiService } from './supervisor-requests-api.service';

describe('SupervisorRequestsApiService', () => {
  let service: SupervisorRequestsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SupervisorRequestsApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SupervisorRequestsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getRequests передаёт page, pageSize и опциональные query', () => {
    service
      .getRequests({
        page: 1,
        pageSize: 25,
        sort: 'createdAtDesc',
        createdFromUtc: '2026-01-01T00:00:00.000Z',
        createdToUtc: '2026-12-31T23:59:59.000Z',
      })
      .subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/supervisor-requests` &&
        r.params.get('page') === '1' &&
        r.params.get('pageSize') === '25' &&
        r.params.get('sort') === 'createdAtDesc' &&
        r.params.get('createdFromUtc') === '2026-01-01T00:00:00.000Z' &&
        r.params.get('createdToUtc') === '2026-12-31T23:59:59.000Z',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 25 });
  });

  it('getById запрашивает GET по id', () => {
    service.getById('sr-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/supervisor-requests/sr-1`);
    expect(req.request.method).toBe('GET');
    req.flush({} as any);
  });

  it('create отправляет POST с teacherUserId и comment', () => {
    service.create('teacher-uuid', 'Комментарий студента').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/supervisor-requests`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ teacherUserId: 'teacher-uuid', comment: 'Комментарий студента' });
    req.flush({} as any);
  });

  it('approve без комментария отправляет пустое тело', () => {
    service.approve('sr-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/supervisor-requests/sr-1/approve`);
    expect(req.request.body).toEqual({});
    req.flush({} as any);
  });

  it('approve с комментарием отправляет обрезанный comment', () => {
    service.approve('sr-1', '  текст  ').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/supervisor-requests/sr-1/approve`);
    expect(req.request.body).toEqual({ comment: 'текст' });
    req.flush({} as any);
  });

  it('reject отправляет comment', () => {
    service.reject('sr-1', 'причина').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/supervisor-requests/sr-1/reject`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ comment: 'причина' });
    req.flush({} as any);
  });

  it('cancel отправляет PUT с пустым объектом', () => {
    service.cancel('sr-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/supervisor-requests/sr-1/cancel`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({});
    req.flush({});
  });
});
