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
import { ApplicationsApiService } from '../applications-api.service';
import { ApplicationDetailComponent } from './application-detail.component';

describe('ApplicationDetailComponent', () => {
  const applicationsApiMock = jasmine.createSpyObj<ApplicationsApiService>('ApplicationsApiService', [
    'getById',
    'approve',
    'reject',
    'submitToDepartmentHead',
    'departmentHeadApprove',
    'departmentHeadReject',
    'cancel',
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

  function makeDetail(status: ApplicationStatusCode): StudentApplicationDetailDto {
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
      actions: [],
    };
  }

  beforeEach(() => {
    roleSignal.set('Student');
    applicationsApiMock.getById.calls.reset();
    applicationsApiMock.approve.calls.reset();
    applicationsApiMock.cancel.calls.reset();
    confirmationMock.confirm.calls.reset();

    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    applicationsApiMock.approve.and.returnValue(of({} as any));
    applicationsApiMock.cancel.and.returnValue(of({} as any));
    applicationsApiMock.reject.and.returnValue(of({} as any));
    applicationsApiMock.submitToDepartmentHead.and.returnValue(of({} as any));
    applicationsApiMock.departmentHeadApprove.and.returnValue(of({} as any));
    applicationsApiMock.departmentHeadReject.and.returnValue(of({} as any));

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

  it('студент может отменить заявку в статусах Pending и ApprovedBySupervisor', () => {
    roleSignal.set('Student');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const f1 = TestBed.createComponent(ApplicationDetailComponent);
    f1.detectChanges();
    expect(f1.componentInstance.canCancel()).toBeTrue();

    applicationsApiMock.getById.and.returnValue(of(makeDetail('ApprovedBySupervisor')));
    const f2 = TestBed.createComponent(ApplicationDetailComponent);
    f2.detectChanges();
    expect(f2.componentInstance.canCancel()).toBeTrue();

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

    applicationsApiMock.getById.and.returnValue(of(makeDetail('ApprovedBySupervisor')));
    const f2 = TestBed.createComponent(ApplicationDetailComponent);
    f2.detectChanges();
    expect(f2.componentInstance.canApproveOrRejectBySupervisor()).toBeFalse();
  });

  it('преподаватель может передать заявку заведующему после своего одобрения', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('ApprovedBySupervisor')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.canSubmitToDepartmentHead()).toBeTrue();
  });

  it('заведующий видит утверждение/отклонение в PendingDepartmentHead', () => {
    roleSignal.set('DepartmentHead');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('PendingDepartmentHead')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.canApproveOrRejectByDepartmentHead()).toBeTrue();
  });

  it('вызывает approve и перезагружает заявку', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getById.and.returnValue(of(makeDetail('Pending')));
    const fixture = TestBed.createComponent(ApplicationDetailComponent);
    fixture.detectChanges();
    applicationsApiMock.getById.calls.reset();

    fixture.componentInstance.approveBySupervisor();
    expect(applicationsApiMock.approve).toHaveBeenCalledWith('app-1');
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
});
