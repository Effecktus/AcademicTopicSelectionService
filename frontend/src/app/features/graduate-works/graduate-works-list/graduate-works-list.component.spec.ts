import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';

import { GraduateWorksApiService } from '../graduate-works-api.service';
import { GraduateWorksListComponent } from './graduate-works-list.component';

describe('GraduateWorksListComponent', () => {
  const apiMock = jasmine.createSpyObj<GraduateWorksApiService>('GraduateWorksApiService', ['getAll']);
  const routerMock = { navigate: jasmine.createSpy('navigate') };

  beforeEach(() => {
    apiMock.getAll.calls.reset();
    routerMock.navigate.calls.reset();

    apiMock.getAll.and.returnValue(
      of({
        items: [
          {
            id: 'gw-1',
            applicationId: 'a1',
            studentId: 's1',
            teacherId: 't1',
            title: 'Тема',
            year: 2025,
            grade: 80,
            commissionMembers: 'К',
            hasFile: true,
            hasPresentation: false,
            createdAt: '2025-01-01T00:00:00Z',
            updatedAt: null,
            fileName: null,
            presentationFileName: null,
            studentFullName: 'С С',
            teacherFullName: 'П П',
          },
        ],
        total: 1,
        page: 1,
        pageSize: 10,
      } as any),
    );

    TestBed.configureTestingModule({
      imports: [GraduateWorksListComponent],
      providers: [
        { provide: GraduateWorksApiService, useValue: apiMock },
        { provide: Router, useValue: routerMock },
      ],
    });
  });

  it('после инициализации запрашивает список ВКР с пагинацией', () => {
    const fixture = TestBed.createComponent(GraduateWorksListComponent);
    fixture.detectChanges();

    expect(apiMock.getAll).toHaveBeenCalled();
    const arg = apiMock.getAll.calls.mostRecent().args[0];
    expect(arg.page).toBe(1);
    expect(arg.pageSize).toBe(10);
    expect(fixture.componentInstance.items().length).toBe(1);
    expect(fixture.componentInstance.total()).toBe(1);
  });

  it('openDetail переходит на карточку записи', () => {
    const fixture = TestBed.createComponent(GraduateWorksListComponent);
    fixture.detectChanges();

    fixture.componentInstance.openDetail('gw-99');

    expect(routerMock.navigate).toHaveBeenCalledWith(['/graduate-works', 'gw-99']);
  });
});
