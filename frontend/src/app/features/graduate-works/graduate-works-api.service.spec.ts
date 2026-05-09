import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { GraduateWorksApiService } from './graduate-works-api.service';

describe('GraduateWorksApiService', () => {
  let service: GraduateWorksApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [GraduateWorksApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(GraduateWorksApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getById запрашивает GET по id', () => {
    service.getById('gw-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/graduate-works/gw-1`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'gw-1' } as any);
  });

  it('getDownloadUrl запрашивает presigned URL', () => {
    service.getDownloadUrl('gw-1', 'thesis').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/graduate-works/gw-1/download-url/thesis`);
    expect(req.request.method).toBe('GET');
    req.flush({ url: 'https://example', expiresAt: '2026-01-01T00:00:00Z' });
  });

  it('getAll передаёт фильтры в query', () => {
    service
      .getAll({
        page: 2,
        pageSize: 15,
        year: 2024,
        titleQuery: 'тема',
        teacherId: 'tid-1',
        teacherQuery: 'Иванов',
      })
      .subscribe();

    const req = httpMock.expectOne((r) => {
      if (r.url !== `${environment.apiUrl}/graduate-works`) return false;
      return (
        r.params.get('page') === '2' &&
        r.params.get('pageSize') === '15' &&
        r.params.get('year') === '2024' &&
        r.params.get('titleQuery') === 'тема' &&
        r.params.get('teacherId') === 'tid-1' &&
        r.params.get('teacherQuery') === 'Иванов'
      );
    });
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 2, pageSize: 15 });
  });

  it('getAll не добавляет пустые строки фильтров', () => {
    service.getAll({ page: 1, pageSize: 10, titleQuery: '   ', teacherQuery: null }).subscribe();

    const req = httpMock.expectOne((r) => {
      if (r.url !== `${environment.apiUrl}/graduate-works`) return false;
      return (
        r.params.get('page') === '1' &&
        r.params.get('pageSize') === '10' &&
        !r.params.has('titleQuery') &&
        !r.params.has('teacherQuery') &&
        !r.params.has('year')
      );
    });
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
  });
});
