import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../environments/environment';
import { ApplicationsApiService } from './applications-api.service';

describe('ApplicationsApiService', () => {
  let service: ApplicationsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ApplicationsApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ApplicationsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getApplications запрашивает список с page и pageSize', () => {
    service.getApplications({ page: 2, pageSize: 15 }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/applications` &&
        r.params.get('page') === '2' &&
        r.params.get('pageSize') === '15',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 2, pageSize: 15 });
  });

  it('create отправляет POST с телом команды', () => {
    const body = { supervisorRequestId: 'sr-1', topicId: 't-1' };
    service.create(body).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ id: 'new' } as any);
  });

  it('reject отправляет comment в теле', () => {
    service.reject('app-1', 'причина').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/reject`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ comment: 'причина' });
    req.flush({} as any);
  });

  it('cancel отправляет PUT с пустым объектом', () => {
    service.cancel('app-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/cancel`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({});
    req.flush({} as any);
  });
});
