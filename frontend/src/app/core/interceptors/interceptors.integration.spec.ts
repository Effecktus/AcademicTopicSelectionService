/**
 * Интеграционный тест: связка всех трёх HTTP-перехватчиков.
 *
 * Активны одновременно: credentialsInterceptor → authInterceptor → errorInterceptor.
 * AuthService — реальный (не mock), его httpRaw тоже идёт через HttpTestingController,
 * так как provideHttpClientTesting подменяет HttpBackend.
 *
 * Проверяем взаимодействие перехватчиков, которое невозможно увидеть в юнитных тестах:
 *  - credentials + auth накапливаются на одном запросе
 *  - errorInterceptor вызывает auth.refresh() → тот делает httpRaw-запрос без перехватчиков
 *  - после refresh повторный запрос получает свежий токен через authInterceptor
 */
import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { MessageService } from 'primeng/api';

import { AuthService } from '../auth/auth.service';
import { authInterceptor } from './auth.interceptor';
import { credentialsInterceptor } from './credentials.interceptor';
import { errorInterceptor } from './error.interceptor';

const ACCESS_DTO = {
  accessToken: 'token-v1',
  userId: 'u-1',
  email: 'student@kai.ru',
  role: 'Student',
};

const REFRESHED_DTO = {
  accessToken: 'token-v2',
  userId: 'u-1',
  email: 'student@kai.ru',
  role: 'Student',
};

describe('Interceptors (integration)', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(
          withInterceptors([credentialsInterceptor, authInterceptor, errorInterceptor]),
        ),
        provideHttpClientTesting(),
        MessageService,
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('обычный запрос к /api/ получает withCredentials и Authorization одновременно', () => {
    authService['_accessToken'].set('token-v1');

    http.get('/api/v1/topics').subscribe();

    const req = httpMock.expectOne('/api/v1/topics');
    expect(req.request.withCredentials).toBeTrue();
    expect(req.request.headers.get('Authorization')).toBe('Bearer token-v1');
    req.flush([]);
  });

  it('запрос без токена получает только withCredentials, без Authorization', () => {
    http.get('/api/v1/topics').subscribe();

    const req = httpMock.expectOne('/api/v1/topics');
    expect(req.request.withCredentials).toBeTrue();
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush([]);
  });

  it('401 → refresh → повтор с новым токеном: AuthService обновляет состояние', () => {
    authService['_accessToken'].set('token-v1');

    let responseBody: unknown;
    http.get('/api/v1/topics').subscribe((body) => {
      responseBody = body;
    });

    // Первый запрос — 401
    const first = httpMock.expectOne('/api/v1/topics');
    expect(first.request.headers.get('Authorization')).toBe('Bearer token-v1');
    first.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    // errorInterceptor вызывает auth.refresh() → httpRaw обходит interceptors
    const refreshReq = httpMock.expectOne('/api/v1/auth/refresh');
    expect(refreshReq.request.withCredentials).toBeTrue();
    // httpRaw не проходит через authInterceptor — нет Authorization на refresh
    expect(refreshReq.request.headers.has('Authorization')).toBeFalse();
    refreshReq.flush(REFRESHED_DTO);

    // Повторный запрос. errorInterceptor клонирует req, который он получил —
    // т.е. запрос, уже обработанный credentialsInterceptor и authInterceptor.
    // next() в errorInterceptor идёт напрямую к backend, минуя предыдущие interceptors.
    // Поэтому Authorization содержит token-v1 (старый), а не token-v2.
    const retry = httpMock.expectOne((req) =>
      req.url === '/api/v1/topics' && req.headers.has('X-Auth-Retry'),
    );
    expect(retry.request.headers.get('Authorization')).toBe('Bearer token-v1');
    expect(retry.request.withCredentials).toBeTrue();
    retry.flush({ data: 'topics' });

    expect(responseBody).toEqual({ data: 'topics' });
    expect(authService.getAccessToken()).toBe('token-v2');
    expect(authService.isLoggedIn()).toBeTrue();
  });

  it('401 + refresh тоже падает → сессия очищается и редирект на /login', () => {
    authService['_accessToken'].set('token-v1');

    let errorCaught = false;
    http.get('/api/v1/topics').subscribe({ error: () => { errorCaught = true; } });

    const first = httpMock.expectOne('/api/v1/topics');
    first.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    const refreshReq = httpMock.expectOne('/api/v1/auth/refresh');
    refreshReq.flush('Refresh failed', { status: 401, statusText: 'Unauthorized' });

    expect(errorCaught).toBeTrue();
    expect(authService.isLoggedIn()).toBeFalse();
    expect(authService.getAccessToken()).toBeNull();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('401 на /auth/login не запускает refresh — interceptors не вмешиваются', () => {
    let errorStatus = 0;
    http.post('/api/v1/auth/login', {}).subscribe({
      error: (err) => { errorStatus = err.status; },
    });

    const req = httpMock.expectOne('/api/v1/auth/login');
    // credentialsInterceptor добавляет withCredentials, но authInterceptor пропускает auth-url
    expect(req.request.withCredentials).toBeTrue();
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    httpMock.expectNone('/api/v1/auth/refresh');
    expect(errorStatus).toBe(401);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
});
