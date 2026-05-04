import { signal } from '@angular/core';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import type { ApplicationsFilter, StudentApplicationDto } from '../../../core/models/application.models';
import { ApplicationsApiService } from '../applications-api.service';
import { ApplicationsListComponent } from './applications-list.component';

describe('ApplicationsListComponent', () => {
  const applicationsApiMock = jasmine.createSpyObj<ApplicationsApiService>('ApplicationsApiService', [
    'getApplications',
  ]);
  const routerMock = {
    navigate: jasmine.createSpy('navigate').and.resolveTo(true),
  };
  const roleSignal = signal<'Student' | 'Teacher' | 'DepartmentHead' | 'Admin' | null>(null);
  const authMock = {
    role: roleSignal.asReadonly(),
  } as unknown as AuthService;

  function makeItem(overrides: Partial<StudentApplicationDto> = {}): StudentApplicationDto {
    return {
      id: 'a1',
      studentId: 's1',
      studentFirstName: 'Иван',
      studentLastName: 'Иванов',
      studentGroupName: '101',
      topicId: 't1',
      topicTitle: 'Тема',
      supervisorRequestId: 'sr1',
      supervisorUserId: 'u1',
      supervisorFirstName: 'Пётр',
      supervisorLastName: 'Петров',
      topicCreatedByUserId: 'u1',
      topicCreatedByEmail: 't@x.ru',
      topicCreatedByFirstName: 'Пётр',
      topicCreatedByLastName: 'Петров',
      status: { id: 'st', codeName: 'Pending', displayName: 'Ожидает' },
      createdAt: '2026-06-15T12:00:00.000Z',
      updatedAt: null,
      ...overrides,
    };
  }

  beforeEach(() => {
    roleSignal.set(null);
    applicationsApiMock.getApplications.calls.reset();
    routerMock.navigate.calls.reset();
    applicationsApiMock.getApplications.and.callFake((params: ApplicationsFilter) => {
      if (params.pageSize === 200) {
        return of({ items: [], total: 0, page: 1, pageSize: 200 });
      }
      return of({ items: [makeItem()], total: 1, page: 1, pageSize: 10 });
    });

    TestBed.configureTestingModule({
      imports: [ApplicationsListComponent],
      providers: [
        { provide: ApplicationsApiService, useValue: applicationsApiMock },
        { provide: Router, useValue: routerMock },
        { provide: AuthService, useValue: authMock },
      ],
    });
  });

  it('загружает заявки при инициализации', fakeAsync(() => {
    roleSignal.set('Student');
    const fixture = TestBed.createComponent(ApplicationsListComponent);
    fixture.detectChanges();

    expect(applicationsApiMock.getApplications).toHaveBeenCalledWith({ page: 1, pageSize: 10 });
    expect(fixture.componentInstance.applications()).toHaveSize(1);
    expect(fixture.componentInstance.isLoading()).toBeFalse();
    tick();
    fixture.detectChanges();
    expect(applicationsApiMock.getApplications).toHaveBeenCalledWith({ page: 1, pageSize: 200 });
  }));

  it('показывает ошибку при сбое загрузки', () => {
    roleSignal.set('Teacher');
    applicationsApiMock.getApplications.and.returnValue(throwError(() => new Error('network')));

    const fixture = TestBed.createComponent(ApplicationsListComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBe('Не удалось загрузить заявки.');
    expect(fixture.componentInstance.isLoading()).toBeFalse();
  });

  it('разрешает создание заявки только студенту без блокирующих заявок', fakeAsync(() => {
    roleSignal.set('Student');
    const f1 = TestBed.createComponent(ApplicationsListComponent);
    f1.detectChanges();
    tick();
    f1.detectChanges();
    expect(f1.componentInstance.canCreateApplication()).toBeTrue();

    roleSignal.set('Teacher');
    const f2 = TestBed.createComponent(ApplicationsListComponent);
    f2.detectChanges();
    expect(f2.componentInstance.canCreateApplication()).toBeFalse();
  }));

  it('скрывает создание заявки у студента при активной заявке', fakeAsync(() => {
    roleSignal.set('Student');
    applicationsApiMock.getApplications.and.callFake((params: ApplicationsFilter) => {
      const pending = makeItem({ id: 'x', status: { id: 'st', codeName: 'Pending', displayName: 'Ожидает' } });
      if (params.pageSize === 200) {
        return of({ items: [pending], total: 1, page: 1, pageSize: 200 });
      }
      return of({ items: [pending], total: 1, page: 1, pageSize: 10 });
    });
    const fixture = TestBed.createComponent(ApplicationsListComponent);
    fixture.detectChanges();
    tick();
    fixture.detectChanges();
    expect(fixture.componentInstance.canCreateApplication()).toBeFalse();
  }));

  it('переходит на форму создания заявки', () => {
    roleSignal.set('Student');
    const fixture = TestBed.createComponent(ApplicationsListComponent);
    fixture.componentInstance.createApplication();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/applications/new']);
  });

  it('переходит к карточке заявки по openApplication', () => {
    roleSignal.set('Student');
    const fixture = TestBed.createComponent(ApplicationsListComponent);
    fixture.componentInstance.openApplication('app-xyz');
    expect(routerMock.navigate).toHaveBeenCalledWith(['/applications', 'app-xyz']);
  });

  it('фильтрует по статусу на клиенте (после debounce)', fakeAsync(() => {
    roleSignal.set('Teacher');
    applicationsApiMock.getApplications.and.returnValue(
      of({
        items: [
          makeItem({ id: '1', status: { id: '1', codeName: 'Pending', displayName: 'А' } }),
          makeItem({
            id: '2',
            status: { id: '2', codeName: 'ApprovedBySupervisor', displayName: 'Б' },
          }),
        ],
        total: 2,
        page: 1,
        pageSize: 10,
      }),
    );

    const fixture = TestBed.createComponent(ApplicationsListComponent);
    fixture.detectChanges();

    fixture.componentInstance.statusFilterControl.setValue('ApprovedBySupervisor');
    tick(150);
    fixture.detectChanges();

    const filtered = fixture.componentInstance.filteredApplications();
    expect(filtered).toHaveSize(1);
    expect(filtered[0].id).toBe('2');
  }));
});
