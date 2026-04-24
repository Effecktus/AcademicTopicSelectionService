import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';

import { AuthService } from './auth.service';
import type { AccessTokenDto } from '../models/auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const accessDto: AccessTokenDto = {
    accessToken: 'token-123',
    userId: 'u-1',
    email: 'student@kai.ru',
    fullName: 'Иванов Иван',
    role: 'Student',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sets session on login success', () => {
    let completed = false;

    service.login('student@kai.ru', 'password123').subscribe(() => {
      completed = true;
    });

    const req = httpMock.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();
    expect(req.request.body).toEqual({
      email: 'student@kai.ru',
      password: 'password123',
    });

    req.flush(accessDto);

    expect(completed).toBeTrue();
    expect(service.getAccessToken()).toBe('token-123');
    expect(service.currentUser()).toEqual({
      userId: 'u-1',
      email: 'student@kai.ru',
      fullName: 'Иванов Иван',
      role: 'Student',
    });
    expect(service.isLoggedIn()).toBeTrue();
  });

  it('deduplicates parallel refresh calls', () => {
    let firstResult = '';
    let secondResult = '';

    service.refresh().subscribe((token) => {
      firstResult = token;
    });
    service.refresh().subscribe((token) => {
      secondResult = token;
    });

    const requests = httpMock.match('/api/v1/auth/refresh');
    expect(requests.length).toBe(1);
    expect(requests[0].request.withCredentials).toBeTrue();

    requests[0].flush(accessDto);

    expect(firstResult).toBe('token-123');
    expect(secondResult).toBe('token-123');
  });

  it('clears session when restoreSession fails', async () => {
    service.login('student@kai.ru', 'password123').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush(accessDto);
    expect(service.isLoggedIn()).toBeTrue();

    const restorePromise = service.restoreSession();

    const refreshReq = httpMock.expectOne('/api/v1/auth/refresh');
    refreshReq.flush('unauthorized', {
      status: 401,
      statusText: 'Unauthorized',
    });

    await restorePromise;

    expect(service.isLoggedIn()).toBeFalse();
    expect(service.getAccessToken()).toBeNull();
    expect(service.currentUser()).toBeNull();
  });

  it('clears session on logout even when backend returns error', () => {
    service.login('student@kai.ru', 'password123').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush(accessDto);
    expect(service.isLoggedIn()).toBeTrue();

    let resultError: unknown = null;

    service.logout().subscribe({
      error: (err) => {
        resultError = err;
      },
    });

    const req = httpMock.expectOne('/api/v1/auth/logout');
    req.flush('server down', { status: 500, statusText: 'Server Error' });

    expect(resultError).toBeNull();
    expect(service.isLoggedIn()).toBeFalse();
    expect(service.currentUser()).toBeNull();
    expect(service.getAccessToken()).toBeNull();
  });

  it('clears session when login fails', () => {
    let capturedError: HttpErrorResponse | null = null;

    service.login('wrong@kai.ru', 'badpassword').subscribe({
      error: (err: HttpErrorResponse) => {
        capturedError = err;
      },
    });

    const req = httpMock.expectOne('/api/v1/auth/login');
    req.flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(capturedError).toEqual(jasmine.objectContaining({ status: 401 }));
    expect(service.isLoggedIn()).toBeFalse();
    expect(service.getAccessToken()).toBeNull();
    expect(service.currentUser()).toBeNull();
  });

  it('restores session successfully when refresh cookie is valid', async () => {
    const restorePromise = service.restoreSession();

    const req = httpMock.expectOne('/api/v1/auth/refresh');
    expect(req.request.withCredentials).toBeTrue();
    req.flush(accessDto);

    await restorePromise;

    expect(service.isLoggedIn()).toBeTrue();
    expect(service.getAccessToken()).toBe('token-123');
    expect(service.currentUser()).toEqual({
      userId: 'u-1',
      email: 'student@kai.ru',
      fullName: 'Иванов Иван',
      role: 'Student',
    });
  });

  it('returns access token from refresh as Observable string', () => {
    let refreshedToken = '';

    service.refresh().subscribe((token) => {
      refreshedToken = token;
    });

    const req = httpMock.expectOne('/api/v1/auth/refresh');
    req.flush(accessDto);

    expect(refreshedToken).toBe('token-123');
    expect(service.isLoggedIn()).toBeTrue();
  });
});
