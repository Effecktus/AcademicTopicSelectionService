import { signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import type { SupervisorRequestDetailDto } from '../../../core/models/supervisor-request.models';
import { SupervisorRequestsApiService } from '../supervisor-requests-api.service';
import { SupervisorRequestDetailComponent } from './supervisor-request-detail.component';

describe('SupervisorRequestDetailComponent', () => {
  const apiMock = jasmine.createSpyObj<SupervisorRequestsApiService>('SupervisorRequestsApiService', [
    'getById',
    'approve',
    'reject',
    'cancel',
  ]);
  const confirmationMock = {
    confirm: jasmine.createSpy('confirm').and.callFake((opts: { accept?: () => void }) => {
      opts.accept?.();
    }),
  };
  const roleSignal = signal<'Student' | 'Teacher' | 'DepartmentHead' | 'Admin' | null>('Teacher');

  const authMock = {
    role: roleSignal.asReadonly(),
  } as unknown as AuthService;

  const pendingRequest: SupervisorRequestDetailDto = {
    id: 'sr-1',
    studentId: 'st-1',
    studentFirstName: 'Иван',
    studentLastName: 'Иванов',
    studentGroupName: '101',
    teacherUserId: 't-1',
    teacherFirstName: 'Пётр',
    teacherLastName: 'Петров',
    teacherEmail: 'p@t.ru',
    status: { id: 's1', codeName: 'Pending', displayName: 'Ожидает' },
    comment: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
  };

  beforeEach(() => {
    roleSignal.set('Teacher');
    apiMock.getById.calls.reset();
    apiMock.approve.calls.reset();
    apiMock.reject.calls.reset();
    apiMock.cancel.calls.reset();
    confirmationMock.confirm.calls.reset();
    apiMock.getById.and.returnValue(of(pendingRequest));
    apiMock.approve.and.returnValue(of({} as any));
    apiMock.reject.and.returnValue(of({} as any));
    apiMock.cancel.and.returnValue(of(void 0));

    TestBed.configureTestingModule({
      imports: [SupervisorRequestDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map([['id', 'sr-1']]) } } },
        { provide: SupervisorRequestsApiService, useValue: apiMock },
        { provide: AuthService, useValue: authMock },
        { provide: ConfirmationService, useValue: confirmationMock },
      ],
    });
  });

  it('загружает запрос по id', () => {
    const fixture = TestBed.createComponent(SupervisorRequestDetailComponent);
    fixture.detectChanges();
    expect(apiMock.getById).toHaveBeenCalledWith('sr-1');
    expect(fixture.componentInstance.request()?.id).toBe('sr-1');
  });

  it('без id показывает ошибку', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [SupervisorRequestDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => null } } },
        },
        { provide: SupervisorRequestsApiService, useValue: apiMock },
        { provide: AuthService, useValue: authMock },
        { provide: ConfirmationService, useValue: confirmationMock },
      ],
    });
    const fixture = TestBed.createComponent(SupervisorRequestDetailComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.errorMessage()).toContain('Некорректный');
    expect(apiMock.getById).not.toHaveBeenCalled();
  });

  it('studentFullName и teacherFullName склеивают ФИО', () => {
    const fixture = TestBed.createComponent(SupervisorRequestDetailComponent);
    fixture.detectChanges();
    const c = fixture.componentInstance;
    const r = c.request()!;
    expect(c.studentFullName(r)).toBe('Иванов Иван');
    expect(c.teacherFullName(r)).toBe('Петров Пётр');
  });

  it('statusClass для неизвестного кода возвращает запасной класс', () => {
    const fixture = TestBed.createComponent(SupervisorRequestDetailComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.statusClass('UnknownFutureStatus')).toBe('status-pending');
  });

  it('confirmApprove отправляет комментарий (trim) или null', () => {
    const fixture = TestBed.createComponent(SupervisorRequestDetailComponent);
    fixture.detectChanges();
    apiMock.getById.calls.reset();

    fixture.componentInstance.openApproveDialog();
    fixture.componentInstance.approveCommentControl.setValue('  заметка  ');
    fixture.componentInstance.confirmApprove();

    expect(apiMock.approve).toHaveBeenCalledWith('sr-1', 'заметка');
    expect(apiMock.getById).toHaveBeenCalledWith('sr-1');

    fixture.componentInstance.openApproveDialog();
    fixture.componentInstance.approveCommentControl.setValue('   ');
    fixture.componentInstance.confirmApprove();
    expect(apiMock.approve).toHaveBeenCalledWith('sr-1', null);
  });

  it('reject отправляет комментарий', () => {
    const fixture = TestBed.createComponent(SupervisorRequestDetailComponent);
    fixture.detectChanges();
    apiMock.getById.calls.reset();

    fixture.componentInstance.openRejectDialog();
    fixture.componentInstance.rejectCommentControl.setValue('не подходит');
    fixture.componentInstance.reject();

    expect(apiMock.reject).toHaveBeenCalledWith('sr-1', 'не подходит');
  });

  it('ошибка действия использует detail из ProblemDetails', () => {
    apiMock.approve.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { detail: '  текст  ' } })),
    );
    const fixture = TestBed.createComponent(SupervisorRequestDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openApproveDialog();
    fixture.componentInstance.confirmApprove();

    expect(fixture.componentInstance.errorMessage()).toBe('текст');
    expect(fixture.componentInstance.isSaving()).toBeFalse();
  });

  it('студент отменяет запрос через confirm', () => {
    roleSignal.set('Student');
    const fixture = TestBed.createComponent(SupervisorRequestDetailComponent);
    fixture.detectChanges();
    apiMock.getById.calls.reset();

    fixture.componentInstance.cancel();

    expect(confirmationMock.confirm).toHaveBeenCalled();
    expect(apiMock.cancel).toHaveBeenCalledWith('sr-1');
  });

  it('студент не может вызвать approve', () => {
    roleSignal.set('Student');
    const fixture = TestBed.createComponent(SupervisorRequestDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openApproveDialog();
    fixture.componentInstance.confirmApprove();

    expect(apiMock.approve).not.toHaveBeenCalled();
  });

  it('reject не вызывается при невалидном комментарии (пустая строка)', () => {
    const fixture = TestBed.createComponent(SupervisorRequestDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openRejectDialog();
    fixture.componentInstance.rejectCommentControl.setValue('');
    fixture.componentInstance.reject();

    expect(apiMock.reject).not.toHaveBeenCalled();
  });

  it('confirmApprove не вызывает API при превышении maxLength комментария', () => {
    const fixture = TestBed.createComponent(SupervisorRequestDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openApproveDialog();
    fixture.componentInstance.approveCommentControl.setValue('x'.repeat(2001));
    fixture.componentInstance.confirmApprove();

    expect(apiMock.approve).not.toHaveBeenCalled();
  });
});
