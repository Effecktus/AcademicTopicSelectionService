import { signal } from '@angular/core';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { By } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import type { ApplicationStatusCode, ChatMessageDto } from '../../../../core/models/application.models';
import { ChatApiService } from '../../chat-api.service';
import { ChatWindowComponent } from './chat-window.component';

describe('ChatWindowComponent', () => {
  const chatApiMock = jasmine.createSpyObj<ChatApiService>('ChatApiService', [
    'getMessages',
    'sendMessage',
    'markAllAsRead',
  ]);

  const currentUserSignal = signal<{ userId: string; fullName: string; email: string; role: 'Student' } | null>(
    null,
  );

  const authMock = {
    currentUser: currentUserSignal.asReadonly(),
  } as unknown as AuthService;

  const msgFromTeacher: ChatMessageDto = {
    id: 'm-t',
    senderId: 'teacher-user',
    senderFullName: 'Петров П.',
    content: 'Здравствуйте',
    sentAt: '2026-01-01T12:00:00Z',
  };

  const msgFromStudent: ChatMessageDto = {
    id: 'm-s',
    senderId: 'student-user',
    senderFullName: 'Иванов И.',
    content: 'Ответ',
    sentAt: '2026-01-01T12:05:00Z',
  };

  beforeEach(() => {
    chatApiMock.getMessages.calls.reset();
    chatApiMock.sendMessage.calls.reset();
    chatApiMock.markAllAsRead.calls.reset();
    chatApiMock.getMessages.and.returnValue(of([]));
    chatApiMock.sendMessage.and.returnValue(of({ ...msgFromStudent, id: 'm-new' }));
    chatApiMock.markAllAsRead.and.returnValue(of(void 0));
    currentUserSignal.set({
      userId: 'student-user',
      fullName: 'Иванов И.',
      email: 's@test.com',
      role: 'Student',
    });

    TestBed.configureTestingModule({
      imports: [ChatWindowComponent],
      providers: [
        { provide: ChatApiService, useValue: chatApiMock },
        { provide: AuthService, useValue: authMock },
      ],
    });
  });

  function createFixture(applicationId = 'app-1', status: ApplicationStatusCode = 'Pending') {
    const fixture = TestBed.createComponent(ChatWindowComponent);
    fixture.componentRef.setInput('applicationId', applicationId);
    fixture.componentRef.setInput('applicationStatus', status);
    fixture.detectChanges();
    return fixture;
  }

  it('при инициализации загружает сообщения', () => {
    createFixture();
    expect(chatApiMock.getMessages).toHaveBeenCalledWith('app-1');
  });

  it('через 5 с повторяет getMessages (polling)', fakeAsync(() => {
    createFixture();
    const initialCalls = chatApiMock.getMessages.calls.count();
    expect(initialCalls).toBeGreaterThanOrEqual(1);
    tick(5_000);
    expect(chatApiMock.getMessages.calls.count()).toBeGreaterThan(initialCalls);
  }));

  it('при активном чате и входящем без readAt вызывает markAllAsRead и обновляет список', fakeAsync(() => {
    let getN = 0;
    chatApiMock.getMessages.and.callFake(() => {
      getN++;
      if (getN === 1) {
        return of([{ ...msgFromTeacher, readAt: undefined }]);
      }
      return of([{ ...msgFromTeacher, readAt: '2026-01-01T13:00:00Z' }]);
    });
    const fixture = createFixture();
    expect(chatApiMock.markAllAsRead).toHaveBeenCalledWith('app-1');
    tick(0);
    fixture.detectChanges();
    expect(fixture.componentInstance.messages()[0].readAt).toBe('2026-01-01T13:00:00Z');
  }));

  it('в терминальном статусе не вызывает markAllAsRead при загрузке', () => {
    chatApiMock.getMessages.and.returnValue(of([{ ...msgFromTeacher, readAt: undefined }]));
    createFixture('app-1', 'RejectedBySupervisor');
    expect(chatApiMock.markAllAsRead).not.toHaveBeenCalled();
  });

  it('в терминальном статусе скрывает поле ввода', () => {
    chatApiMock.getMessages.and.returnValue(of([]));
    const fixture = createFixture('app-1', 'RejectedBySupervisor');
    fixture.detectChanges();
    const textarea = fixture.debugElement.query(By.css('textarea'));
    expect(textarea).toBeNull();
    const badge = fixture.nativeElement.textContent;
    expect(badge).toContain('Чат закрыт');
  });

  it('sendMessage вызывает API и перезагружает список', fakeAsync(() => {
    const fresh: ChatMessageDto[] = [msgFromStudent];
    chatApiMock.getMessages.and.returnValue(of([]));
    const fixture = createFixture();
    chatApiMock.getMessages.and.returnValue(of(fresh));
    const c = fixture.componentInstance;
    c.messageControl.setValue('Привет');
    fixture.detectChanges();
    c.sendMessage();
    tick(0);
    expect(chatApiMock.sendMessage).toHaveBeenCalledWith('app-1', 'Привет');
    expect(chatApiMock.getMessages).toHaveBeenCalledWith('app-1');
    expect(c.messages()).toEqual(fresh);
    expect(c.isSending()).toBeFalse();
  }));

  it('при ошибке загрузки выставляет errorMessage', () => {
    chatApiMock.getMessages.and.returnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 403,
            error: { detail: 'Нет доступа' },
          }),
      ),
    );
    const fixture = createFixture();
    expect(fixture.componentInstance.errorMessage()).toBe('Нет доступа');
    expect(fixture.componentInstance.isLoading()).toBeFalse();
  });

  it('при ошибке отправки выставляет errorMessage и сбрасывает isSending', () => {
    chatApiMock.getMessages.and.returnValue(of([]));
    chatApiMock.sendMessage.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: {} })),
    );
    const fixture = createFixture();
    const c = fixture.componentInstance;
    c.messageControl.setValue('x');
    fixture.detectChanges();
    c.sendMessage();
    expect(c.errorMessage()).toContain('Не удалось отправить');
    expect(c.isSending()).toBeFalse();
  });

  it('isOwnMessage учитывает currentUserId', () => {
    chatApiMock.getMessages.and.returnValue(of([]));
    const fixture = createFixture();
    const c = fixture.componentInstance;
    expect(c.isOwnMessage(msgFromStudent)).toBeTrue();
    expect(c.isOwnMessage(msgFromTeacher)).toBeFalse();
  });

  it('charsUsed и canSend обновляются при вводе', () => {
    chatApiMock.getMessages.and.returnValue(of([]));
    const fixture = createFixture();
    const c = fixture.componentInstance;
    expect(c.charsUsed()).toBe(0);
    expect(c.canSend()).toBeFalse();
    c.messageControl.setValue('ab');
    fixture.detectChanges();
    expect(c.charsUsed()).toBe(2);
    expect(c.canSend()).toBeTrue();
  });
});
