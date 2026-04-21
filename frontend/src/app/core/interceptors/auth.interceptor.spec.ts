import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';

import { AuthService } from '../auth/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  const authServiceMock = {
    getAccessToken: jasmine.createSpy('getAccessToken'),
  };

  beforeEach(() => {
    authServiceMock.getAccessToken.calls.reset();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceMock },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('adds Authorization header when access token is present', () => {
    authServiceMock.getAccessToken.and.returnValue('access-token-abc');

    http.get('/api/v1/topics').subscribe();

    const req = httpMock.expectOne('/api/v1/topics');
    expect(req.request.headers.get('Authorization')).toBe('Bearer access-token-abc');
    req.flush([]);
  });

  it('does not add Authorization header when no token', () => {
    authServiceMock.getAccessToken.and.returnValue(null);

    http.get('/api/v1/topics').subscribe();

    const req = httpMock.expectOne('/api/v1/topics');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush([]);
  });

  it('skips Authorization header for /auth/login', () => {
    authServiceMock.getAccessToken.and.returnValue('token');

    http.post('/api/v1/auth/login', {}).subscribe();

    const req = httpMock.expectOne('/api/v1/auth/login');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('skips Authorization header for /auth/refresh', () => {
    authServiceMock.getAccessToken.and.returnValue('token');

    http.post('/api/v1/auth/refresh', {}).subscribe();

    const req = httpMock.expectOne('/api/v1/auth/refresh');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('skips Authorization header for /auth/logout', () => {
    authServiceMock.getAccessToken.and.returnValue('token');

    http.post('/api/v1/auth/logout', {}).subscribe();

    const req = httpMock.expectOne('/api/v1/auth/logout');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });
});
