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

  it('getById запрашивает GET по id', () => {
    service.getById('app-99').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-99`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'app-99' } as any);
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

  it('approve без комментария отправляет пустое тело', () => {
    service.approve('app-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/approve`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({});
    req.flush({} as any);
  });

  it('approve с комментарием отправляет comment в теле', () => {
    service.approve('app-1', '  заметка  ').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/approve`);
    expect(req.request.body).toEqual({ comment: 'заметка' });
    req.flush({} as any);
  });

  it('departmentHeadApprove без комментария отправляет пустое тело', () => {
    service.departmentHeadApprove('app-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/department-head-approve`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({});
    req.flush({} as any);
  });

  it('departmentHeadApprove с комментарием отправляет comment', () => {
    service.departmentHeadApprove('app-1', '  замечание  ').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/department-head-approve`);
    expect(req.request.body).toEqual({ comment: 'замечание' });
    req.flush({} as any);
  });

  it('departmentHeadReject отправляет comment', () => {
    service.departmentHeadReject('app-1', 'отказ').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/department-head-reject`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ comment: 'отказ' });
    req.flush({} as any);
  });

  it('cancel отправляет PUT с пустым объектом и обрабатывает 204', () => {
    service.cancel('app-1').subscribe((v) => {
      expect(v.id).toBe('app-1');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/cancel`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({});
    req.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('submitToSupervisor отправляет PUT с пустым телом', () => {
    service.submitToSupervisor('app-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/submit-to-supervisor`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({});
    req.flush({} as any);
  });

  it('updateTopic отправляет PATCH с title и description', () => {
    service
      .updateTopic('app-1', { title: 'Заголовок', description: 'Текст' })
      .subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/topic`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ title: 'Заголовок', description: 'Текст' });
    req.flush({} as any);
  });

  it('returnForEditing отправляет comment в теле', () => {
    service.returnForEditing('app-1', 'Доработать').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/applications/app-1/return-for-editing`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ comment: 'Доработать' });
    req.flush({} as any);
  });

  it('departmentHeadReturnForEditing отправляет comment в теле', () => {
    service.departmentHeadReturnForEditing('app-1', 'Уточнить').subscribe();

    const req = httpMock.expectOne(
      `${environment.apiUrl}/applications/app-1/department-head-return-for-editing`,
    );
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ comment: 'Уточнить' });
    req.flush({} as any);
  });
});
