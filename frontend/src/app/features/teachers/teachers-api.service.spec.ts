import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../environments/environment';
import { TeachersApiService } from './teachers-api.service';

describe('TeachersApiService', () => {
  let service: TeachersApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TeachersApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TeachersApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getTeachers отправляет GET с page и pageSize', () => {
    service.getTeachers({ page: 2, pageSize: 25 }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/teachers` &&
        r.params.get('page') === '2' &&
        r.params.get('pageSize') === '25',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 2, pageSize: 25 });
  });

  it('getTeachers включает query когда он указан и не пустой', () => {
    service.getTeachers({ page: 1, pageSize: 10, query: 'Иванов' }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/teachers` &&
        r.params.get('query') === 'Иванов',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
  });

  it('getTeachers не добавляет query когда он пустая строка или пробелы', () => {
    service.getTeachers({ page: 1, pageSize: 10, query: '   ' }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/teachers` &&
        !r.params.has('query'),
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
  });

  it('getTeachers добавляет sort когда он указан', () => {
    service.getTeachers({ page: 1, pageSize: 10, sort: 'nameAsc' }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/teachers` &&
        r.params.get('sort') === 'nameAsc',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
  });

  it('getTeachers не добавляет sort когда он не указан', () => {
    service.getTeachers({ page: 1, pageSize: 10 }).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/teachers` &&
        !r.params.has('sort'),
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
  });

  it('getTeacherById отправляет GET по id', () => {
    service.getTeacherById('teacher-99').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/teachers/teacher-99`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'teacher-99' } as any);
  });

  it('getTeacherGraduateWorks отправляет GET по id преподавателя', () => {
    service.getTeacherGraduateWorks('teacher-42').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/teachers/teacher-42/graduate-works`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
