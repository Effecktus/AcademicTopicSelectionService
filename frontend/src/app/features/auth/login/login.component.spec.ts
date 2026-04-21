import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  const authServiceMock = {
    login: jasmine.createSpy('login'),
  };

  const routerMock = {
    navigateByUrl: jasmine.createSpy('navigateByUrl').and.resolveTo(true),
  };

  beforeEach(async () => {
    authServiceMock.login.calls.reset();
    routerMock.navigateByUrl.calls.reset();

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    }).compileComponents();
  });

  it('marks controls as touched and does not call login for invalid form', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;
    const markSpy = spyOn(component.form, 'markAllAsTouched').and.callThrough();

    component.submit();

    expect(markSpy).toHaveBeenCalledTimes(1);
    expect(authServiceMock.login).not.toHaveBeenCalled();
  });

  it('submits valid form and redirects on success', () => {
    authServiceMock.login.and.returnValue(of(void 0));
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    component.form.setValue({
      email: 'student@kai.ru',
      password: 'password123',
    });

    component.submit();

    expect(authServiceMock.login).toHaveBeenCalledWith('student@kai.ru', 'password123');
    expect(component.isLoading()).toBeTrue();
    expect(component.errorMessage()).toBeNull();
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('shows mapped error message and resets loading on 401', () => {
    authServiceMock.login.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' })),
    );
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    component.form.setValue({
      email: 'student@kai.ru',
      password: 'password123',
    });

    component.submit();
    fixture.detectChanges();

    expect(component.isLoading()).toBeFalse();
    expect(component.errorMessage()).toBe('Неверный email или пароль.');
    const errorBanner = fixture.nativeElement.querySelector('.error-banner') as HTMLElement | null;
    expect(errorBanner?.textContent).toContain('Неверный email или пароль.');
  });

  it('maps 429 response to rate-limit message', () => {
    authServiceMock.login.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 429, statusText: 'Too Many Requests' })),
    );
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    component.form.setValue({
      email: 'student@kai.ru',
      password: 'password123',
    });

    component.submit();

    expect(component.errorMessage()).toBe('Слишком много попыток входа. Подождите несколько минут.');
  });

  it('maps 5xx response to generic service unavailable message', () => {
    authServiceMock.login.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 503, statusText: 'Service Unavailable' })),
    );
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    component.form.setValue({
      email: 'student@kai.ru',
      password: 'password123',
    });

    component.submit();

    expect(component.errorMessage()).toBe('Сервис временно недоступен. Попробуйте позже.');
  });

  it('maps 400 response to invalid credentials message', () => {
    authServiceMock.login.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 400, statusText: 'Bad Request' })),
    );
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    component.form.setValue({
      email: 'student@kai.ru',
      password: 'password123',
    });

    component.submit();

    expect(component.errorMessage()).toBe('Неверный email или пароль.');
  });

  it('resets error message on new submit attempt', () => {
    authServiceMock.login.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' })),
    );
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ email: 'student@kai.ru', password: 'password123' });

    component.submit();
    expect(component.errorMessage()).toBe('Неверный email или пароль.');

    authServiceMock.login.and.returnValue(of(void 0));
    component.submit();

    expect(component.errorMessage()).toBeNull();
  });

  it('displays validation error in DOM when email field is touched and empty', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    component.form.controls.email.markAsTouched();
    fixture.detectChanges();

    const errorHints = fixture.nativeElement.querySelectorAll('small.error') as NodeListOf<HTMLElement>;
    expect(errorHints.length).toBeGreaterThan(0);
    expect(errorHints[0].textContent).toContain('корректный email');
  });
});
