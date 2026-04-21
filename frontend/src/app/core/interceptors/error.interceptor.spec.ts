import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: Router;
  let messageService: MessageService;

  const authServiceMock = {
    refresh: jasmine.createSpy('refresh'),
    clearSession: jasmine.createSpy('clearSession'),
  };

  beforeEach(() => {
    authServiceMock.refresh.calls.reset();
    authServiceMock.clearSession.calls.reset();

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceMock },
        MessageService,
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    messageService = TestBed.inject(MessageService);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);
    spyOn(messageService, 'add');
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('refreshes token and retries original request on 401', () => {
    authServiceMock.refresh.and.returnValue(of('new-token'));

    let responseBody: unknown;

    http.get('/api/v1/topics').subscribe((body) => {
      responseBody = body;
    });

    const firstReq = httpMock.expectOne('/api/v1/topics');
    firstReq.flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    const retryReq = httpMock.expectOne((req) =>
      req.url === '/api/v1/topics' && req.headers.get('X-Auth-Retry') === '1',
    );
    retryReq.flush({ ok: true });

    expect(authServiceMock.refresh).toHaveBeenCalledTimes(1);
    expect(authServiceMock.clearSession).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(responseBody).toEqual({ ok: true });
  });

  it('clears session and redirects to /login when refresh fails', () => {
    authServiceMock.refresh.and.returnValue(throwError(() => new Error('refresh failed')));

    let capturedError: HttpErrorResponse | null = null;

    http.get('/api/v1/topics').subscribe({
      error: (err: HttpErrorResponse) => {
        capturedError = err;
      },
    });

    const req = httpMock.expectOne('/api/v1/topics');
    req.flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(authServiceMock.refresh).toHaveBeenCalledTimes(1);
    expect(authServiceMock.clearSession).toHaveBeenCalledTimes(1);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
    expect(capturedError).toEqual(jasmine.objectContaining({ status: 401 }));
  });

  it('does not try refresh for auth public urls', () => {
    let capturedError: HttpErrorResponse | null = null;

    http.post('/api/v1/auth/login', {}).subscribe({
      error: (err: HttpErrorResponse) => {
        capturedError = err;
      },
    });

    const req = httpMock.expectOne('/api/v1/auth/login');
    req.flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(authServiceMock.refresh).not.toHaveBeenCalled();
    expect(authServiceMock.clearSession).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(capturedError).toEqual(jasmine.objectContaining({ status: 401 }));
  });

  it('shows toast on 403 Forbidden', () => {
    let capturedError: HttpErrorResponse | null = null;

    http.get('/api/v1/admin/users').subscribe({
      error: (err: HttpErrorResponse) => {
        capturedError = err;
      },
    });

    const req = httpMock.expectOne('/api/v1/admin/users');
    req.flush('Forbidden', { status: 403, statusText: 'Forbidden' });

    expect(authServiceMock.refresh).not.toHaveBeenCalled();
    expect(messageService.add).toHaveBeenCalledWith(
      jasmine.objectContaining({ severity: 'error', summary: 'Доступ запрещён' }),
    );
    expect(capturedError).toEqual(jasmine.objectContaining({ status: 403 }));
  });

  it('shows toast on 429 Too Many Requests', () => {
    let capturedError: HttpErrorResponse | null = null;

    http.get('/api/v1/topics').subscribe({
      error: (err: HttpErrorResponse) => {
        capturedError = err;
      },
    });

    const req = httpMock.expectOne('/api/v1/topics');
    req.flush('Too Many Requests', { status: 429, statusText: 'Too Many Requests' });

    expect(messageService.add).toHaveBeenCalledWith(
      jasmine.objectContaining({ severity: 'warn', summary: 'Подождите' }),
    );
    expect(capturedError).toEqual(jasmine.objectContaining({ status: 429 }));
  });

  it('shows toast on 5xx server errors', () => {
    let capturedError: HttpErrorResponse | null = null;

    http.get('/api/v1/topics').subscribe({
      error: (err: HttpErrorResponse) => {
        capturedError = err;
      },
    });

    const req = httpMock.expectOne('/api/v1/topics');
    req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });

    expect(authServiceMock.refresh).not.toHaveBeenCalled();
    expect(authServiceMock.clearSession).not.toHaveBeenCalled();
    expect(messageService.add).toHaveBeenCalledWith(
      jasmine.objectContaining({ severity: 'error', summary: 'Ошибка сервера' }),
    );
    expect(capturedError).toEqual(jasmine.objectContaining({ status: 500 }));
  });

  it('does not show toast for auth public URL errors', () => {
    http.post('/api/v1/auth/login', {}).subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/v1/auth/login');
    req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });

    expect(messageService.add).not.toHaveBeenCalled();
  });

  it('does not enter refresh loop when request already has X-Auth-Retry header', () => {
    let capturedError: HttpErrorResponse | null = null;

    http
      .get('/api/v1/topics', { headers: { 'X-Auth-Retry': '1' } })
      .subscribe({
        error: (err: HttpErrorResponse) => {
          capturedError = err;
        },
      });

    const req = httpMock.expectOne('/api/v1/topics');
    req.flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(authServiceMock.refresh).not.toHaveBeenCalled();
    expect(capturedError).toEqual(jasmine.objectContaining({ status: 401 }));
  });
});
