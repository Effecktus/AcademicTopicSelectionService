import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import type {
  ApplicationStatusCode,
  StudentApplicationDetailDto,
} from '../../../core/models/application.models';
import { APPLICATION_STATUS_BADGE_CLASS } from '../../../core/constants/application-status-styles';
import { ApplicationsApiService } from '../applications-api.service';
import { ApplicationDetailComponent } from './application-detail.component';

describe('ApplicationDetailComponent', () => {
  const applicationsApiMock = jasmine.createSpyObj<ApplicationsApiService>('ApplicationsApiService', [
    'getById',
    'approve',
    'reject',
    'departmentHeadApprove',
    'departmentHeadReject',
    'cancel',
    'submitToSupervisor',
    'updateTopic',
    'returnForEditing',
    'departmentHeadReturnForEditing',
  ]);
  let routerNavigateSpy: jasmine.Spy;
  const confirmationMock = {
    confirm: jasmine.createSpy('confirm').and.callFake((opts: { accept?: () => void }) => {
      opts.accept?.();
    }),
  };
  const roleSignal = signal<'Student' | 'Teacher' | 'DepartmentHead' | 'Admin' | null>(null);
  const authMock = {
    role: roleSignal.asReadonly(),
  } as unknown as AuthService;

  function makeDetail(
    status: ApplicationStatusCode,
    partial?: Partial<StudentApplicationDetailDto>,
  ): StudentApplicationDetailDto {
    return {
      id: 'app-1',
      studentId: 's1',
      studentFirstName: 'Иван',
      studentLastName: 'Иванов',
      studentGroupName: '101',
      topicId: 't1',
      topicTitle: 'Тема',
      topicDescription: null,
      supervisorRequestId: 'sr1',
      supervisorUserId: 'u1',
      supervisorFirstName: 'Пётр',
      supervisorLastName: 'Петров',
      supervisorDepartmentId: null,
      topicCreatedByUserId: 'u1',
      topicCreatedByFirstName: 'Пётр',
      topicCreatedByLastName: 'Петров',
      topicSupervisorDepartmentId: null,
      status: { id: 'st', codeName: status, displayName: 'Статус' },
      createdAt: '2026-01-01T10:00:00Z',
      updatedAt: null,
      actions: [
        {
          id: 'act-1',
          responsibleId: 'u9',
          responsibleFirstName: 'Анна',
          responsibleLastName: 'Смирнова',
          statusCodeName: 'Pending',
          statusDisplayName: 'Ожидает',
          comment: null,
          createdAt: '2026-01-02T10:00:00Z',
        },
      ],
      topicChangeHistory: [],
      ...partial,
    };
  }

  beforeEach(() => {
    roleSignal.set('Student');
    applicationsApiMock.getById.calls.reset();
    applicationsApiMock.approve.calls.reset();
    applicationsApiMock.cancel.calls.reset();
    applicationsApiMock.reject.calls.reset();
    applicationsApiMock.departmentHeadApprove.calls.reset();
    applicationsApiMock.departmentHeadReject.calls.reset();
    applicationsApiMock.submitToSupervisor.calls.reset();
    applicationsApiMock.updateTopic.calls.reset();
    applicationsApiMock.returnForEditing.calls.reset();
    applicationsApiMock.departmentHeadReturnForEditing.calls.reset();
    confirmationMock.confirm.calls.reset();

    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    applicationsApiMock.approve.and.returnValue(of({} as any));
    applicationsApiMock.cancel.and.returnValue(of({} as any));
    applicationsApiMock.reject.and.returnValue(of({} as any));
    applicationsApiMock.departmentHeadApprove.and.returnValue(of({} as any));
    applicationsApiMock.departmentHeadReject.and.returnValue(of({} as any));
    applicationsApiMock.submitToSupervisor.and.returnValue(of({} as any));
    applicationsApiMock.updateTopic.and.returnValue(of({} as any));
    applicationsApiMock.returnForEditing.and.returnValue(of({} as any));
    applicationsApiMock.departmentHeadReturnForEditing.and.returnValue(of({} as any));

    TestBed.configureTestingModule({
      imports: [ApplicationDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map([['id', 'app-1']]) } } },
        { provide: AuthService, useValue: authMock },
        { provide: ApplicationsApiService, useValue: applicationsApiMock },
        { provide: ConfirmationService, useValue: confirmationMock },
      ],
    });
    routerNavigateSpy = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
  });

  it('загружает заявку по id из маршрута', () => {
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    expect(applicationsApiMock.getById).toHaveBeenCalledWith('app-1');
    expect(fixture.componentInstance.application()?.id).toBe('app-1');
    expect(fixture.componentInstance.isLoading()).toBeFalse();
  });

  it('при ошибке загрузки перенаправляет на список заявок', () => {
    applicationsApiMock.getById.and.returnValue(throwError(() => new Error('not found')));

    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    expect(routerNavigateSpy).toHaveBeenCalledWith(['/applications']);
    expect(fixture.componentInstance.errorMessage()).toContain('не найдена');
  });

  it('студент может отменить заявку в статусах Pending, ApprovedBySupervisor и OnEditing', () => {
    roleSignal.set('Student');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const f1 = TestBed.createComponent(ApplicationDetailComponent);
    f1.detectChanges();
    expect(f1.componentInstance.canCancel()).toBeTrue();

    applicationsApiMock.getById.and.returnValue(of(makeDetail('ApprovedBySupervisor')));
    const f2 = TestBed.createComponent(ApplicationDetailComponent);
    f2.detectChanges();
    expect(f2.componentInstance.canCancel()).toBeTrue();

    applicationsApiMock.getById.and.returnValue(of(makeDetail('OnEditing')));
    const f2b = TestBed.createComponent(ApplicationDetailComponent);
    f2b.detectChanges();
    expect(f2b.componentInstance.canCancel()).toBeTrue();

    applicationsApiMock.getById.and.returnValue(of(makeDetail('PendingDepartmentHead')));
    const f3 = TestBed.createComponent(ApplicationDetailComponent);
    f3.detectChanges();
    expect(f3.componentInstance.canCancel()).toBeFalse();
  });

  it('преподаватель видит действия одобрения/отклонения только в Pending', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const f1 = TestBed.createComponent(ApplicationDetailComponent);
    f1.detectChanges();
    expect(f1.componentInstance.canApproveOrRejectBySupervisor()).toBeTrue();
    expect(f1.componentInstance.canReturnForEditingBySupervisor()).toBeTrue();

    applicationsApiMock.getById.and.returnValue(of(makeDetail('ApprovedBySupervisor')));
    const f2 = TestBed.createComponent(ApplicationDetailComponent);
    f2.detectChanges();
    expect(f2.componentInstance.canApproveOrRejectBySupervisor()).toBeFalse();
    expect(f2.componentInstance.canReturnForEditingBySupervisor()).toBeFalse();
  });

  it('заведующий видит утверждение/отклонение в PendingDepartmentHead', () => {
    roleSignal.set('DepartmentHead');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('PendingDepartmentHead')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.canApproveOrRejectByDepartmentHead()).toBeTrue();
    expect(fixture.componentInstance.canReturnForEditingByDepartmentHead()).toBeTrue();
  });

  it('вызывает approve и перезагружает заявку', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    applicationsApiMock.getById.calls.reset();

    fixture.componentInstance.openApproveDialog('supervisor');
    fixture.componentInstance.confirmApprove();
    expect(applicationsApiMock.approve).toHaveBeenCalledWith('app-1', null);
    expect(applicationsApiMock.getById).toHaveBeenCalledWith('app-1');
  });

  it('отмена вызывает ConfirmationService и cancel', () => {
    roleSignal.set('Student');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.cancel();

    expect(confirmationMock.confirm).toHaveBeenCalled();
    expect(applicationsApiMock.cancel).toHaveBeenCalledWith('app-1');
  });

  it('без id в маршруте показывает сообщение об ошибке', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [ApplicationDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => null } } },
        },
        { provide: AuthService, useValue: authMock },
        { provide: ApplicationsApiService, useValue: applicationsApiMock },
        { provide: ConfirmationService, useValue: confirmationMock },
      ],
    });

    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toContain('Некорректный');
    expect(applicationsApiMock.getById).not.toHaveBeenCalled();
  });

  it('statusClass и полные имена соответствуют данным', () => {
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    const c = fixture.componentInstance;
    const item = c.application()!;

    expect(c.statusClass('Pending')).toBe(APPLICATION_STATUS_BADGE_CLASS['Pending']);
    expect(c.supervisorFullName(item)).toBe('Петров Пётр');
    expect(c.studentFullName(item)).toBe('Иванов Иван');
    expect(c.actionResponsibleFullName(item.actions[0])).toBe('Смирнова Анна');
  });

  it('approve с комментарием передаёт обрезанный текст в API', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openApproveDialog('supervisor');
    fixture.componentInstance.approveCommentControl.setValue('  комментарий  ');
    fixture.componentInstance.confirmApprove();

    expect(applicationsApiMock.approve).toHaveBeenCalledWith('app-1', 'комментарий');
  });

  it('заведующий вызывает departmentHeadApprove', () => {
    roleSignal.set('DepartmentHead');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('PendingDepartmentHead')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openApproveDialog('departmentHead');
    fixture.componentInstance.confirmApprove();

    expect(applicationsApiMock.departmentHeadApprove).toHaveBeenCalledWith('app-1', null);
  });

  it('отклонение научруком вызывает reject с комментарием', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openRejectDialog('supervisor');
    fixture.componentInstance.rejectCommentControl.setValue('не подходит');
    fixture.componentInstance.reject();

    expect(applicationsApiMock.reject).toHaveBeenCalledWith('app-1', 'не подходит');
  });

  it('отклонение завкафом вызывает departmentHeadReject', () => {
    roleSignal.set('DepartmentHead');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('PendingDepartmentHead')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openRejectDialog('departmentHead');
    fixture.componentInstance.rejectCommentControl.setValue('отказ');
    fixture.componentInstance.reject();

    expect(applicationsApiMock.departmentHeadReject).toHaveBeenCalledWith('app-1', 'отказ');
  });

  it('заголовки диалогов зависят от режима', () => {
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    const c = fixture.componentInstance;

    c.openRejectDialog('supervisor');
    expect(c.rejectDialogTitle()).toContain('научным');

    c.openRejectDialog('departmentHead');
    expect(c.rejectDialogTitle()).toContain('заведующим');

    c.openReturnForEditingDialog('supervisor');
    expect(c.rejectDialogTitle()).toContain('Возврат');
    expect(c.rejectDialogConfirmLabel()).toContain('доработку');

    c.openApproveDialog('supervisor');
    expect(c.approveDialogTitle()).toContain('научным');
    expect(c.approveDialogPrimaryLabel()).toBe('Одобрить');

    c.openApproveDialog('departmentHead');
    expect(c.approveDialogTitle()).toContain('заведующим');
    expect(c.approveDialogPrimaryLabel()).toBe('Утвердить');
  });

  it('при ошибке действия показывает detail из ProblemDetails', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    applicationsApiMock.approve.and.returnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { detail: '  Ошибка с сервера  ' },
          }),
      ),
    );

    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openApproveDialog('supervisor');
    fixture.componentInstance.confirmApprove();

    expect(fixture.componentInstance.errorMessage()).toBe('Ошибка с сервера');
    expect(fixture.componentInstance.isSaving()).toBeFalse();
  });

  it('lastTopicChangeAt берётся из последней записи истории темы', () => {
    applicationsApiMock.getById.and.returnValue(
      of(
        makeDetail('OnEditing', {
          topicChangeHistory: [
            {
              id: 'h1',
              changedByUserId: 'u1',
              changedByFirstName: 'Пётр',
              changedByLastName: 'Петров',
              changeKind: 'TopicTitle',
              changeKindDisplayName: 'Название',
              newValue: 'А',
              createdAt: '2026-01-01T10:00:00Z',
            },
            {
              id: 'h2',
              changedByUserId: 'u1',
              changedByFirstName: 'Пётр',
              changedByLastName: 'Петров',
              changeKind: 'TopicDescription',
              changeKindDisplayName: 'Описание',
              newValue: null,
              createdAt: '2026-01-02T12:00:00Z',
            },
          ],
        }),
      ),
    );
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.lastTopicChangeAt()).toBe('2026-01-02T12:00:00Z');
  });

  it('topicChangeAuthorFullName и topicChangeValueOrDash форматируют поля', () => {
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    const c = fixture.componentInstance;
    const row = {
      id: 'h1',
      changedByUserId: 'u1',
      changedByFirstName: 'Пётр',
      changedByLastName: 'Петров',
      changeKind: 'TopicTitle',
      changeKindDisplayName: 'Название',
      newValue: '',
      createdAt: '2026-01-01T10:00:00Z',
    };
    expect(c.topicChangeAuthorFullName(row)).toBe('Петров Пётр');
    expect(c.topicChangeValueOrDash(null)).toBe('—');
    expect(c.topicChangeValueOrDash('')).toBe('—');
    expect(c.topicChangeValueOrDash('Текст')).toBe('Текст');
  });

  it('студент в OnEditing может редактировать тему и передавать научруку', () => {
    roleSignal.set('Student');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('OnEditing')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    const c = fixture.componentInstance;
    expect(c.canStudentEditTopic()).toBeTrue();
    expect(c.canSubmitToSupervisor()).toBeTrue();
    expect(c.hasTopicUnsavedChanges()).toBeFalse();

    c.topicTitleControl.setValue('Другое название');
    fixture.detectChanges();
    expect(c.hasTopicUnsavedChanges()).toBeTrue();
  });

  it('saveTopicEdits вызывает updateTopic с null description для пустой строки и перезагружает заявку', () => {
    roleSignal.set('Student');
    let loadCount = 0;
    applicationsApiMock.getById.and.callFake(() => {
      loadCount++;
      if (loadCount === 1) {
        return of(makeDetail('OnEditing', { topicTitle: 'Тема', topicDescription: 'Было' }));
      }
      return of(makeDetail('OnEditing', { topicTitle: 'Новое', topicDescription: null }));
    });
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    applicationsApiMock.getById.calls.reset();

    fixture.componentInstance.topicTitleControl.setValue('Новое');
    fixture.componentInstance.topicDescriptionControl.setValue('   ');
    fixture.componentInstance.saveTopicEdits();

    expect(applicationsApiMock.updateTopic).toHaveBeenCalledWith('app-1', {
      title: 'Новое',
      description: null,
    });
    expect(applicationsApiMock.getById).toHaveBeenCalledWith('app-1');
  });

  it('преподаватель не может сохранить правки темы через saveTopicEdits', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('OnEditing')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.topicTitleControl.setValue('Чужое');
    fixture.componentInstance.saveTopicEdits();

    expect(applicationsApiMock.updateTopic).not.toHaveBeenCalled();
  });

  it('submitToSupervisor вызывает API после подтверждения', () => {
    roleSignal.set('Student');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('OnEditing')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    applicationsApiMock.getById.calls.reset();

    fixture.componentInstance.submitToSupervisor();
    expect(applicationsApiMock.submitToSupervisor).toHaveBeenCalledWith('app-1');
    expect(applicationsApiMock.getById).toHaveBeenCalledWith('app-1');
  });

  it('возврат на доработку научруком вызывает returnForEditing', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openReturnForEditingDialog('supervisor');
    fixture.componentInstance.rejectCommentControl.setValue('Исправьте цель');
    fixture.componentInstance.reject();

    expect(applicationsApiMock.returnForEditing).toHaveBeenCalledWith('app-1', 'Исправьте цель');
  });

  it('возврат на доработку завкафом вызывает departmentHeadReturnForEditing', () => {
    roleSignal.set('DepartmentHead');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('PendingDepartmentHead')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openReturnForEditingDialog('departmentHead');
    fixture.componentInstance.rejectCommentControl.setValue('Добавьте обзор');
    fixture.componentInstance.reject();

    expect(applicationsApiMock.departmentHeadReturnForEditing).toHaveBeenCalledWith(
      'app-1',
      'Добавьте обзор',
    );
  });

  it('возврат на доработку не вызывает API при пустом комментарии', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openReturnForEditingDialog('supervisor');
    fixture.componentInstance.rejectCommentControl.setValue('   ');
    fixture.componentInstance.reject();

    expect(applicationsApiMock.returnForEditing).not.toHaveBeenCalled();
  });

  it('confirmApprove не вызывает API при невалидном комментарии (maxLength)', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openApproveDialog('supervisor');
    fixture.componentInstance.approveCommentControl.setValue('x'.repeat(2001));
    fixture.componentInstance.confirmApprove();

    expect(applicationsApiMock.approve).not.toHaveBeenCalled();
  });

  it('confirmApprove не вызывает departmentHeadApprove если роль не завкаф', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openApproveDialog('departmentHead');
    fixture.componentInstance.confirmApprove();

    expect(applicationsApiMock.departmentHeadApprove).not.toHaveBeenCalled();
    expect(applicationsApiMock.approve).not.toHaveBeenCalled();
  });

  it('при ошибке действия без detail используется fallback', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    applicationsApiMock.approve.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: {} })),
    );
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.openApproveDialog('supervisor');
    fixture.componentInstance.confirmApprove();

    expect(fixture.componentInstance.errorMessage()).toBe('Не удалось одобрить заявку.');
  });
});
