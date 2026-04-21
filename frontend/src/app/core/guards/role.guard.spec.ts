import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { roleGuard } from './role.guard';

describe('roleGuard', () => {
  const authServiceMock = {
    role: jasmine.createSpy('role'),
  };

  beforeEach(() => {
    authServiceMock.role.calls.reset();

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock },
      ],
    });
  });

  it('returns true when user role matches required role', () => {
    authServiceMock.role.and.returnValue('Admin');

    const route = { data: { role: 'Admin' } } as never;
    const result = TestBed.runInInjectionContext(() => roleGuard(route, {} as never));

    expect(result).toBeTrue();
  });

  it('returns / UrlTree when user role does not match', () => {
    authServiceMock.role.and.returnValue('Student');
    const router = TestBed.inject(Router);

    const route = { data: { role: 'Admin' } } as never;
    const result = TestBed.runInInjectionContext(() => roleGuard(route, {} as never));

    expect(router.serializeUrl(result as never)).toBe('/');
  });

  it('returns true when no role is required in route data', () => {
    authServiceMock.role.and.returnValue('Student');

    const route = { data: {} } as never;
    const result = TestBed.runInInjectionContext(() => roleGuard(route, {} as never));

    expect(result).toBeTrue();
  });
});
