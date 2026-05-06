import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';

import { NotificationBadgeService } from './notification-badge.service';

describe('NotificationBadgeService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [NotificationBadgeService, provideHttpClient()],
    });
  });

  afterEach(() => {
    TestBed.inject(NotificationBadgeService).reset();
  });

  it('startPolling(null) не дергает API и обнуляет счётчик', () => {
    const service = TestBed.inject(NotificationBadgeService);
    service.startPolling(null);
    expect(service.unreadCount()).toBe(0);
  });

  it('startPolling(Admin) останавливает polling и обнуляет счётчик', () => {
    const service = TestBed.inject(NotificationBadgeService);
    service.startPolling('Admin');
    expect(service.unreadCount()).toBe(0);
  });

  it('decrement не уводит счётчик ниже нуля', () => {
    const service = TestBed.inject(NotificationBadgeService);
    service.startPolling(null);
    service.unreadCount.set(1);
    service.decrement();
    service.decrement();
    expect(service.unreadCount()).toBe(0);
  });

  it('reset обнуляет счётчик', () => {
    const service = TestBed.inject(NotificationBadgeService);
    service.unreadCount.set(5);
    service.reset();
    expect(service.unreadCount()).toBe(0);
  });
});
