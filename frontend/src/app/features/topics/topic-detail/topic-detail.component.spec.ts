import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import type { TopicDto } from '../../../core/models/topic.models';
import { TopicsApiService } from '../topics-api.service';
import { TopicDetailComponent } from './topic-detail.component';

describe('TopicDetailComponent', () => {
  const topicsApiMock = jasmine.createSpyObj<TopicsApiService>('TopicsApiService', ['getTopicById', 'deleteTopic']);
  const confirmationMock = {
    confirm: jasmine.createSpy('confirm').and.callFake((opts: { accept?: () => void }) => {
      opts.accept?.();
    }),
  };
  const roleSignal = signal<'Student' | 'Teacher' | 'DepartmentHead' | 'Admin' | null>('Teacher');
  const authMock = {
    role: roleSignal.asReadonly(),
    currentUser: signal({ userId: 'teacher-1', fullName: 'T T', email: 't@t', role: 'Teacher' } as const).asReadonly(),
  } as unknown as AuthService;

  const baseTopic: TopicDto = {
    id: 'topic-1',
    title: 'Тема',
    description: null,
    status: { id: 's1', codeName: 'Active', displayName: 'Активна' },
    creatorType: { id: 'c1', codeName: 'Teacher', displayName: 'Преподаватель' },
    createdByUserId: 'teacher-1',
    createdByEmail: 't@t',
    createdByFirstName: 'П',
    createdByLastName: 'П',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
  };

  beforeEach(() => {
    topicsApiMock.getTopicById.calls.reset();
    topicsApiMock.deleteTopic.calls.reset();
    confirmationMock.confirm.calls.reset();
    roleSignal.set('Teacher');
    topicsApiMock.getTopicById.and.returnValue(of(baseTopic));
    topicsApiMock.deleteTopic.and.returnValue(of(void 0));

    TestBed.configureTestingModule({
      imports: [TopicDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map([['id', 'topic-1']]) } } },
        { provide: TopicsApiService, useValue: topicsApiMock },
        { provide: AuthService, useValue: authMock },
        { provide: ConfirmationService, useValue: confirmationMock },
      ],
    });
  });

  it('загружает тему по id из маршрута', () => {
    const fixture = TestBed.createComponent(TopicDetailComponent);
    fixture.detectChanges();
    expect(topicsApiMock.getTopicById).toHaveBeenCalledWith('topic-1');
    expect(fixture.componentInstance.topic()?.id).toBe('topic-1');
    expect(fixture.componentInstance.isLoading()).toBeFalse();
  });

  it('без id в маршруте показывает ошибку и не грузит API', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [TopicDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => null } } },
        },
        { provide: TopicsApiService, useValue: topicsApiMock },
        { provide: AuthService, useValue: authMock },
        { provide: ConfirmationService, useValue: confirmationMock },
      ],
    });
    const fixture = TestBed.createComponent(TopicDetailComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.errorMessage()).toContain('Некорректный');
    expect(topicsApiMock.getTopicById).not.toHaveBeenCalled();
  });

  it('при ошибке загрузки выставляет сообщение', () => {
    topicsApiMock.getTopicById.and.returnValue(throwError(() => new Error('x')));
    const fixture = TestBed.createComponent(TopicDetailComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.errorMessage()).toContain('Не удалось загрузить');
  });

  it('canManageTopic: только автор-преподаватель', () => {
    const fixture = TestBed.createComponent(TopicDetailComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.canManageTopic()).toBeTrue();

    topicsApiMock.getTopicById.and.returnValue(
      of({ ...baseTopic, createdByUserId: 'other-user' }),
    );
    const f2 = TestBed.createComponent(TopicDetailComponent);
    f2.detectChanges();
    expect(f2.componentInstance.canManageTopic()).toBeFalse();
  });

  it('deleteTopic вызывает API и переходит на список тем', () => {
    const router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);

    const fixture = TestBed.createComponent(TopicDetailComponent);
    fixture.detectChanges();
    fixture.componentInstance.deleteTopic();

    expect(confirmationMock.confirm).toHaveBeenCalled();
    expect(topicsApiMock.deleteTopic).toHaveBeenCalledWith('topic-1');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/topics');
  });
});
