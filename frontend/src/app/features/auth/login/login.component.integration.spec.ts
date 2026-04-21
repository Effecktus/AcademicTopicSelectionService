/**
 * Интеграционный тест: LoginComponent + реальный AuthService.
 *
 * Отличие от юнитного теста:
 *  - AuthService не мокируется — используется реальный экземпляр.
 *  - HTTP-слой подменяется HttpTestingController (через provideHttpClientTesting),
 *    который заменяет HttpBackend. Так как AuthService создаёт свой httpRaw
 *    из HttpBackend, оба потока (httpRaw и HttpClient) перехватываются одним
 *    HttpTestingController.
 *  - Проверяем полный путь: DOM → форма → сервис → HTTP → состояние → редирект.
 */
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { LoginComponent } from './login.component';

const ACCESS_DTO = {
  accessToken: 'real-token-xyz',
  userId: 'u-42',
  email: 'student@kai.ru',
  role: 'Student',
};

describe('LoginComponent (integration)', () => {
  let httpMock: HttpTestingController;
  let authService: AuthService;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('успешный вход: заполняет форму, отправляет HTTP, устанавливает сессию и редиректит', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({ email: 'student@kai.ru', password: 'password123' });
    component.submit();

    const req = httpMock.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'student@kai.ru', password: 'password123' });
    expect(req.request.withCredentials).toBeTrue();

    req.flush(ACCESS_DTO);
    fixture.detectChanges();

    expect(authService.isLoggedIn()).toBeTrue();
    expect(authService.getAccessToken()).toBe('real-token-xyz');
    expect(authService.currentUser()).toEqual({
      userId: 'u-42',
      email: 'student@kai.ru',
      role: 'Student',
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
    // isLoading остаётся true после успешного входа намеренно:
    // кнопка заблокирована до завершения навигации (UX-решение).
    expect(component.isLoading()).toBeTrue();
    expect(component.errorMessage()).toBeNull();
  });

  it('ошибка 401: сессия не устанавливается, в DOM появляется сообщение об ошибке', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({ email: 'wrong@kai.ru', password: 'wrongpass' });
    component.submit();

    const req = httpMock.expectOne('/api/v1/auth/login');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    expect(authService.isLoggedIn()).toBeFalse();
    expect(authService.getAccessToken()).toBeNull();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(component.isLoading()).toBeFalse();
    expect(component.errorMessage()).toBe('Неверный email или пароль.');

    const banner = fixture.nativeElement.querySelector('.error-banner') as HTMLElement | null;
    expect(banner?.textContent?.trim()).toBe('Неверный email или пароль.');
  });

  it('повторный успешный вход после ошибки: сессия устанавливается, ошибка очищается', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({ email: 'student@kai.ru', password: 'wrongpass' });
    component.submit();
    httpMock
      .expectOne('/api/v1/auth/login')
      .flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();
    expect(component.errorMessage()).not.toBeNull();

    component.form.setValue({ email: 'student@kai.ru', password: 'correctpass' });
    component.submit();
    httpMock.expectOne('/api/v1/auth/login').flush(ACCESS_DTO);
    fixture.detectChanges();

    expect(component.errorMessage()).toBeNull();
    expect(authService.isLoggedIn()).toBeTrue();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('невалидная форма: HTTP не отправляется, сессия не устанавливается', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.submit();
    httpMock.expectNone('/api/v1/auth/login');

    expect(authService.isLoggedIn()).toBeFalse();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(component.form.controls.email.touched).toBeTrue();
    expect(component.form.controls.password.touched).toBeTrue();
  });
});
