import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';

import { GraduateWorksApiService } from '../graduate-works-api.service';
import { GraduateWorkDetailComponent } from './graduate-work-detail.component';

describe('GraduateWorkDetailComponent', () => {
  const dto = {
    id: 'gw-1',
    applicationId: 'app-1',
    studentId: 's-1',
    teacherId: 't-1',
    title: 'Работа',
    year: 2025,
    grade: 90,
    commissionMembers: 'Комиссия',
    hasFile: true,
    hasPresentation: false,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: null,
    fileName: 'a.docx',
    presentationFileName: null,
    studentFullName: 'Студентов С.',
    teacherFullName: 'Преподавателев П.',
  };

  const apiMock = jasmine.createSpyObj<GraduateWorksApiService>('GraduateWorksApiService', [
    'getById',
    'getDownloadUrl',
  ]);

  beforeEach(() => {
    apiMock.getById.calls.reset();
    apiMock.getDownloadUrl.calls.reset();
    apiMock.getById.and.returnValue(of(dto as any));

    TestBed.configureTestingModule({
      imports: [GraduateWorkDetailComponent],
      providers: [
        { provide: GraduateWorksApiService, useValue: apiMock },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: new Map([['id', 'gw-1']]) } },
        },
      ],
    });
  });

  it('загружает запись по id из маршрута', () => {
    const fixture = TestBed.createComponent(GraduateWorkDetailComponent);
    fixture.detectChanges();

    expect(apiMock.getById).toHaveBeenCalledWith('gw-1');
    expect(fixture.componentInstance.work()?.title).toBe('Работа');
    expect(fixture.componentInstance.isLoading()).toBeFalse();
  });

  it('показывает ошибку при 404', () => {
    apiMock.getById.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404, error: {} })));

    const fixture = TestBed.createComponent(GraduateWorkDetailComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toContain('не найден');
  });

  it('download открывает url из ответа API', () => {
    apiMock.getDownloadUrl.and.returnValue(of({ url: 'https://presigned', expiresAt: '2026-01-01T00:00:00Z' }));
    const openSpy = spyOn(window, 'open').and.stub();

    const fixture = TestBed.createComponent(GraduateWorkDetailComponent);
    fixture.detectChanges();

    fixture.componentInstance.download('thesis');

    expect(apiMock.getDownloadUrl).toHaveBeenCalledWith('gw-1', 'thesis');
    expect(openSpy).toHaveBeenCalledWith('https://presigned', '_blank', 'noopener,noreferrer');
  });
});
