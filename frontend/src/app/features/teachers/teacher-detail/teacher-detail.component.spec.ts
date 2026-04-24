import { of, throwError } from 'rxjs';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { SupervisorRequestsApiService } from '../../supervisor-requests/supervisor-requests-api.service';
import { TopicsApiService } from '../../topics/topics-api.service';
import { TeachersApiService } from '../teachers-api.service';
import { TeacherDetailComponent } from './teacher-detail.component';

describe('TeacherDetailComponent', () => {
  const teacher = {
    id: 'teacher-id-1',
    userId: 'teacher-user-1',
    firstName: 'Иван',
    lastName: 'Иванов',
    middleName: 'Иванович',
    email: 'ivanov@kai.ru',
    academicDegree: { codeName: 'PhD', displayName: 'Кандидат наук' },
    academicTitle: { codeName: 'Docent', displayName: 'Доцент' },
    position: { codeName: 'Professor', displayName: 'Профессор' },
    maxStudentsLimit: 5,
  };

  const teachersApiMock = jasmine.createSpyObj<TeachersApiService>('TeachersApiService', [
    'getTeacherById',
  ]);
  const topicsApiMock = jasmine.createSpyObj<TopicsApiService>('TopicsApiService', ['getTopics']);
  const requestsApiMock = jasmine.createSpyObj<SupervisorRequestsApiService>(
    'SupervisorRequestsApiService',
    ['getRequests', 'create'],
  );
  const roleSignal = signal<'Student' | 'Teacher' | 'Admin' | null>(null);
  const authServiceMock = {
    role: roleSignal.asReadonly(),
  } as unknown as AuthService;

  function setupBaseMocks(): void {
    teachersApiMock.getTeacherById.and.returnValue(of(teacher as any));
    topicsApiMock.getTopics.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20 } as any));
  }

  beforeEach(() => {
    teachersApiMock.getTeacherById.calls.reset();
    topicsApiMock.getTopics.calls.reset();
    requestsApiMock.getRequests.calls.reset();
    requestsApiMock.create.calls.reset();
    roleSignal.set(null);

    TestBed.configureTestingModule({
      imports: [TeacherDetailComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map([['id', 'teacher-id-1']]) } } },
        { provide: AuthService, useValue: authServiceMock },
        { provide: TeachersApiService, useValue: teachersApiMock },
        { provide: TopicsApiService, useValue: topicsApiMock },
        { provide: SupervisorRequestsApiService, useValue: requestsApiMock },
      ],
    });
  });

  it('скрывает кнопку запроса, если уже есть Pending запрос к этому преподавателю', () => {
    roleSignal.set('Student');
    setupBaseMocks();
    requestsApiMock.getRequests.and.returnValue(
      of({
        items: [
          {
            id: 'req-1',
            teacherUserId: 'teacher-user-1',
            status: { codeName: 'Pending', displayName: 'Ожидает' },
          },
        ],
      } as any),
    );

    const fixture = TestBed.createComponent(TeacherDetailComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(requestsApiMock.getRequests).toHaveBeenCalledWith({ page: 1, pageSize: 100 });
    expect(component.hasCreatedSupervisorRequest()).toBeTrue();
    expect(component.canCreateSupervisorRequest()).toBeFalse();
  });

  it('оставляет кнопку доступной, если Pending запроса к этому преподавателю нет', () => {
    roleSignal.set('Student');
    setupBaseMocks();
    requestsApiMock.getRequests.and.returnValue(
      of({
        items: [
          {
            id: 'req-1',
            teacherUserId: 'teacher-user-1',
            status: { codeName: 'Approved', displayName: 'Одобрено' },
          },
          {
            id: 'req-2',
            teacherUserId: 'other-teacher-user',
            status: { codeName: 'Pending', displayName: 'Ожидает' },
          },
        ],
      } as any),
    );

    const fixture = TestBed.createComponent(TeacherDetailComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.hasCreatedSupervisorRequest()).toBeFalse();
    expect(component.canCreateSupervisorRequest()).toBeTrue();
  });

  it('не проверяет существующие запросы для не-студента', () => {
    roleSignal.set('Teacher');
    setupBaseMocks();

    const fixture = TestBed.createComponent(TeacherDetailComponent);
    fixture.detectChanges();

    expect(requestsApiMock.getRequests).not.toHaveBeenCalled();
  });

  it('не ломает карточку, если проверка существующих запросов вернула ошибку', () => {
    roleSignal.set('Student');
    setupBaseMocks();
    requestsApiMock.getRequests.and.returnValue(
      throwError(() => new Error('network failure')),
    );

    const fixture = TestBed.createComponent(TeacherDetailComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.teacher()?.userId).toBe('teacher-user-1');
    expect(component.hasCreatedSupervisorRequest()).toBeFalse();
    expect(component.canCreateSupervisorRequest()).toBeTrue();
  });

  it('показывает кнопку в DOM для студента, когда Pending запроса нет', () => {
    roleSignal.set('Student');
    setupBaseMocks();
    requestsApiMock.getRequests.and.returnValue(of({ items: [] } as any));

    const fixture = TestBed.createComponent(TeacherDetailComponent);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('p-button');
    expect(button).not.toBeNull();
  });

  it('скрывает кнопку в DOM, когда найден Pending запрос к текущему преподавателю', () => {
    roleSignal.set('Student');
    setupBaseMocks();
    requestsApiMock.getRequests.and.returnValue(
      of({
        items: [
          {
            id: 'req-1',
            teacherUserId: 'teacher-user-1',
            status: { codeName: 'Pending', displayName: 'Ожидает' },
          },
        ],
      } as any),
    );

    const fixture = TestBed.createComponent(TeacherDetailComponent);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('p-button');
    expect(button).toBeNull();
  });

  it('по клику отправляет запрос и скрывает кнопку после успеха', () => {
    roleSignal.set('Student');
    setupBaseMocks();
    requestsApiMock.getRequests.and.returnValue(of({ items: [] } as any));
    requestsApiMock.create.and.returnValue(
      of({
        id: 'req-created',
        teacherUserId: 'teacher-user-1',
        status: { codeName: 'Pending', displayName: 'Ожидает' },
      } as any),
    );

    const fixture = TestBed.createComponent(TeacherDetailComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.createSupervisorRequest();
    fixture.detectChanges();

    expect(requestsApiMock.create).toHaveBeenCalledWith('teacher-user-1');
    expect(component.hasCreatedSupervisorRequest()).toBeTrue();
    expect(component.canCreateSupervisorRequest()).toBeFalse();
    expect(fixture.nativeElement.querySelector('p-button')).toBeNull();
  });
});
