import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { SupervisorRequestsApiService } from '../../supervisor-requests/supervisor-requests-api.service';
import { TopicsApiService } from '../../topics/topics-api.service';
import { ApplicationsApiService } from '../applications-api.service';
import { ApplicationCreateComponent } from './application-create.component';

describe('ApplicationCreateComponent', () => {
  const supervisorApiMock = jasmine.createSpyObj<SupervisorRequestsApiService>(
    'SupervisorRequestsApiService',
    ['getRequests'],
  );
  const topicsApiMock = jasmine.createSpyObj<TopicsApiService>('TopicsApiService', ['getTopics']);
  const applicationsApiMock = jasmine.createSpyObj<ApplicationsApiService>('ApplicationsApiService', ['create']);
  let routerNavigateSpy: jasmine.Spy;

  const approvedSupervisor = {
    id: 'sr-1',
    studentId: 'st-1',
    studentFirstName: 'С',
    studentLastName: 'Т',
    teacherUserId: 'teacher-user-1',
    teacherFirstName: 'Науч',
    teacherLastName: 'Руковод',
    status: { id: 's1', codeName: 'ApprovedBySupervisor', displayName: 'Одобрено' },
    comment: null,
    createdAt: '2026-01-01',
    updatedAt: null,
  };

  beforeEach(() => {
    supervisorApiMock.getRequests.calls.reset();
    topicsApiMock.getTopics.calls.reset();
    applicationsApiMock.create.calls.reset();

    supervisorApiMock.getRequests.and.returnValue(
      of({
        items: [
          approvedSupervisor,
          {
            id: 'sr-2',
            studentId: 'st-1',
            studentFirstName: 'С',
            studentLastName: 'Т',
            teacherUserId: 'u2',
            teacherFirstName: 'Др',
            teacherLastName: 'Учитель',
            status: { id: 's2', codeName: 'Pending', displayName: 'Ждёт' },
            comment: null,
            createdAt: '2026-01-02',
            updatedAt: null,
          },
        ],
      } as any),
    );
    topicsApiMock.getTopics.and.returnValue(
      of({
        items: [{ id: 'topic-1', title: 'Тема А', status: { codeName: 'Active', displayName: 'Активна' } } as any],
        total: 1,
        page: 1,
        pageSize: 20,
      }),
    );
    applicationsApiMock.create.and.returnValue(of({ id: 'new-app' } as any));

    TestBed.configureTestingModule({
      imports: [ApplicationCreateComponent],
      providers: [
        provideRouter([]),
        { provide: SupervisorRequestsApiService, useValue: supervisorApiMock },
        { provide: TopicsApiService, useValue: topicsApiMock },
        { provide: ApplicationsApiService, useValue: applicationsApiMock },
      ],
    });
    routerNavigateSpy = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
  });

  it('показывает пустое состояние, если нет одобренных запросов научрука', () => {
    supervisorApiMock.getRequests.and.returnValue(of({ items: [] } as any));

    const fixture = TestBed.createComponent(ApplicationCreateComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.hasApprovedSupervisors()).toBeFalse();
  });

  it('берёт первый одобренный запрос и подгружает темы преподавателя', () => {
    const fixture = TestBed.createComponent(ApplicationCreateComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.approvedSupervisorRequest()?.id).toBe('sr-1');
    expect(topicsApiMock.getTopics).toHaveBeenCalledWith(
      jasmine.objectContaining({
        createdByUserId: 'teacher-user-1',
        statusCodeName: 'Active',
        creatorTypeCodeName: 'Teacher',
      }),
    );
    expect(fixture.componentInstance.hasApprovedSupervisors()).toBeTrue();
  });

  it('отправляет create с topicId и переходит на деталь', () => {
    const fixture = TestBed.createComponent(ApplicationCreateComponent);
    fixture.detectChanges();

    fixture.componentInstance.form.controls.topicId.setValue('topic-1');
    fixture.componentInstance.submit();

    expect(applicationsApiMock.create).toHaveBeenCalledWith({
      supervisorRequestId: 'sr-1',
      topicId: 'topic-1',
    });
    expect(routerNavigateSpy).toHaveBeenCalledWith(['/applications', 'new-app']);
  });

  it('отправляет create со своей темой', () => {
    const fixture = TestBed.createComponent(ApplicationCreateComponent);
    fixture.detectChanges();

    fixture.componentInstance.chooseSource('custom');
    fixture.componentInstance.form.controls.proposedTitle.setValue('  Моя тема  ');
    fixture.componentInstance.form.controls.proposedDescription.setValue(' Описание ');
    fixture.componentInstance.submit();

    expect(applicationsApiMock.create).toHaveBeenCalledWith({
      supervisorRequestId: 'sr-1',
      proposedTitle: 'Моя тема',
      proposedDescription: 'Описание',
    });
  });

  it('не вызывает create при невалидной форме', () => {
    const fixture = TestBed.createComponent(ApplicationCreateComponent);
    fixture.detectChanges();

    fixture.componentInstance.submit();
    expect(applicationsApiMock.create).not.toHaveBeenCalled();

    fixture.componentInstance.chooseSource('custom');
    fixture.componentInstance.form.controls.proposedTitle.setValue('');
    fixture.componentInstance.submit();
    expect(applicationsApiMock.create).not.toHaveBeenCalled();
  });

  it('выводит detail из ProblemDetails при ошибке API', () => {
    applicationsApiMock.create.and.returnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { detail: '  Конфликт темы  ' },
          }),
      ),
    );

    const fixture = TestBed.createComponent(ApplicationCreateComponent);
    fixture.detectChanges();
    fixture.componentInstance.form.controls.topicId.setValue('topic-1');
    fixture.componentInstance.submit();

    expect(fixture.componentInstance.errorMessage()).toBe('Конфликт темы');
    expect(fixture.componentInstance.isSaving()).toBeFalse();
  });
});
