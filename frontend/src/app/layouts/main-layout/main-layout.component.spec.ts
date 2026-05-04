import { computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import type { UserInfo } from '../../core/models/auth.models';
import { NotificationBadgeService } from '../../core/notifications/notification-badge.service';
import { MainLayoutComponent } from './main-layout.component';
import { NAV_ITEMS } from './nav-items';

describe('MainLayoutComponent', () => {
  const userSignal = signal<UserInfo | null>({
    fullName: 'Тестов Пользователь',
    userId: 'u-1',
    email: 't@test.com',
    role: 'Student',
  });
  const authMock = {
    currentUser: userSignal.asReadonly(),
    role: computed(() => userSignal()?.role ?? null),
    logout: jasmine.createSpy('logout').and.returnValue(of(void 0)),
  } as unknown as AuthService;

  const unreadSignal = signal(0);
  const notificationBadgeMock = {
    unreadCount: unreadSignal.asReadonly(),
    startPolling: jasmine.createSpy('startPolling'),
    reset: jasmine.createSpy('reset'),
  };

  beforeEach(() => {
    unreadSignal.set(0);
    userSignal.set({
      fullName: 'Тестов Пользователь',
      userId: 'u-1',
      email: 't@test.com',
      role: 'Student',
    });
    notificationBadgeMock.startPolling.calls.reset();
    notificationBadgeMock.reset.calls.reset();
    authMock.logout = jasmine.createSpy('logout').and.returnValue(of(void 0));

    TestBed.configureTestingModule({
      imports: [MainLayoutComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authMock },
        { provide: NotificationBadgeService, useValue: notificationBadgeMock },
      ],
    });
  });

  it('ngOnInit запускает polling с текущей ролью', () => {
    const fixture = TestBed.createComponent(MainLayoutComponent);
    fixture.detectChanges();
    expect(notificationBadgeMock.startPolling).toHaveBeenCalledWith('Student');
  });

  it('navItems фильтрует пункты меню по роли студента', () => {
    const fixture = TestBed.createComponent(MainLayoutComponent);
    fixture.detectChanges();
    const routes = fixture.componentInstance.navItems().map((i) => i.route);
    expect(routes).toContain('/applications');
    expect(routes).not.toContain('/admin/users');
    expect(fixture.componentInstance.navItems().length).toBe(
      NAV_ITEMS.filter((i) => i.roles.includes('Student')).length,
    );
  });

  it('для Admin скрывает блок уведомлений в шапке', () => {
    userSignal.set({
      fullName: 'Админ',
      userId: 'a-1',
      email: 'a@test.com',
      role: 'Admin',
    });
    const fixture = TestBed.createComponent(MainLayoutComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.canSeeNotifications()).toBeFalse();
    expect(notificationBadgeMock.startPolling).toHaveBeenCalledWith('Admin');
  });

  it('logout сбрасывает badge и ведёт на /login', () => {
    const router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);

    const fixture = TestBed.createComponent(MainLayoutComponent);
    fixture.detectChanges();
    fixture.componentInstance.logout();

    expect(authMock.logout).toHaveBeenCalled();
    expect(notificationBadgeMock.reset).toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });
});
