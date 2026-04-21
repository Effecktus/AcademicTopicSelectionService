import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  const authServiceMock = {
    isLoggedIn: jasmine.createSpy('isLoggedIn'),
  };

  beforeEach(() => {
    authServiceMock.isLoggedIn.calls.reset();

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock },
      ],
    });
  });

  it('returns true when user is logged in', () => {
    authServiceMock.isLoggedIn.and.returnValue(true);

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(result).toBeTrue();
  });

  it('returns /login UrlTree when user is not logged in', () => {
    authServiceMock.isLoggedIn.and.returnValue(false);
    const router = TestBed.inject(Router);

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(router.serializeUrl(result as never)).toBe('/login');
  });
});
