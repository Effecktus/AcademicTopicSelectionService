import { DatePipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import { AuthService } from '../../../core/auth/auth.service';
import { isStatusBlockingNewApplication } from '../../../core/constants/application-create-eligibility';
import { APPLICATION_STATUS_BADGE_CLASS } from '../../../core/constants/application-status-styles';
import type {
  ApplicationStatusCode,
  StudentApplicationDto,
} from '../../../core/models/application.models';
import { ApplicationsApiService } from '../applications-api.service';
import { currentYearDateRange } from '../../../core/utils/date-utils';

interface StatusOption {
  label: string;
  value: '' | ApplicationStatusCode;
}

/** Сколько заявок запрашивать для проверки «есть активная» (студент редко имеет сотни записей). */
const STUDENT_CREATE_ELIGIBILITY_PAGE_SIZE = 200;

@Component({
  selector: 'app-applications-list',
  imports: [ReactiveFormsModule, DatePipe, Button, Select, NgClass, InputText],
  templateUrl: './applications-list.component.html',
  styleUrl: './applications-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicationsListComponent {
  private readonly auth = inject(AuthService);
  private readonly applicationsApi = inject(ApplicationsApiService);
  private readonly router = inject(Router);

  readonly role = this.auth.role;
  /** Для студента: после ответа API о возможности создать новую заявку. */
  readonly studentEligibilityResolved = signal(false);
  readonly studentHasBlockingApplication = signal(false);
  readonly canCreateApplication = computed(() => {
    if (this.role() !== 'Student') {
      return false;
    }
    if (!this.studentEligibilityResolved()) {
      return false;
    }
    return !this.studentHasBlockingApplication();
  });
  readonly applications = signal<StudentApplicationDto[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly page = signal(1);
  readonly pageSize = 10;
  readonly total = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  readonly canGoPrev = computed(() => this.page() > 1);
  readonly canGoNext = computed(() => this.page() < this.totalPages());

  readonly statusFilterControl = new FormControl<'' | ApplicationStatusCode>('', { nonNullable: true });
  readonly selectedStatusFilter = signal<'' | ApplicationStatusCode>('');
  readonly dateFromControl = new FormControl(currentYearDateRange().from, { nonNullable: true });
  readonly dateToControl = new FormControl(currentYearDateRange().to, { nonNullable: true });
  readonly statusOptions: StatusOption[] = [
    { label: 'Все статусы', value: '' },
    { label: 'На редактировании', value: 'OnEditing' },
    { label: 'Ожидает ответа преподавателя', value: 'Pending' },
    { label: 'Одобрена преподавателем', value: 'ApprovedBySupervisor' },
    { label: 'Ожидает решения заведующего кафедрой', value: 'PendingDepartmentHead' },
    { label: 'Утверждена заведующим кафедрой', value: 'ApprovedByDepartmentHead' },
    { label: 'Отклонена преподавателем', value: 'RejectedBySupervisor' },
    { label: 'Отклонена заведующим кафедрой', value: 'RejectedByDepartmentHead' },
    { label: 'Отменена студентом', value: 'Cancelled' },
  ];

  readonly filteredApplications = computed(() => {
    const statusCode = this.selectedStatusFilter();
    const from = this.dateFromControl.value ? new Date(`${this.dateFromControl.value}T00:00:00`) : null;
    const to = this.dateToControl.value ? new Date(`${this.dateToControl.value}T23:59:59`) : null;

    return this.applications().filter((item) => {
      if (statusCode && item.status.codeName !== statusCode) {
        return false;
      }

      const createdAt = new Date(item.createdAt);
      if (from && createdAt < from) {
        return false;
      }
      if (to && createdAt > to) {
        return false;
      }

      return true;
    });
  });

  constructor() {
    this.selectedStatusFilter.set(this.statusFilterControl.value);

    this.statusFilterControl.valueChanges.pipe(debounceTime(100), takeUntilDestroyed()).subscribe((value) => {
      this.selectedStatusFilter.set(value);
      this.errorMessage.set(null);
    });
    this.dateFromControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.errorMessage.set(null));
    this.dateToControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.errorMessage.set(null));

    this.loadApplications();
  }

  prevPage(): void {
    if (!this.canGoPrev()) return;
    this.page.update((value) => value - 1);
    this.loadApplications();
  }

  nextPage(): void {
    if (!this.canGoNext()) return;
    this.page.update((value) => value + 1);
    this.loadApplications();
  }

  openApplication(applicationId: string): void {
    void this.router.navigate(['/applications', applicationId]);
  }

  createApplication(): void {
    void this.router.navigate(['/applications/new']);
  }

  resetFilters(): void {
    this.statusFilterControl.setValue('');
    const range = currentYearDateRange();
    this.dateFromControl.setValue(range.from);
    this.dateToControl.setValue(range.to);
  }

  studentFullName(item: StudentApplicationDto): string {
    return `${item.studentLastName} ${item.studentFirstName}`.trim();
  }

  supervisorFullName(item: StudentApplicationDto): string {
    return `${item.supervisorLastName} ${item.supervisorFirstName}`.trim();
  }

  statusClass(statusCode: ApplicationStatusCode): string {
    return APPLICATION_STATUS_BADGE_CLASS[statusCode];
  }

  private loadApplications(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.applicationsApi
      .getApplications({
        page: this.page(),
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (result) => {
          this.applications.set(result.items);
          this.total.set(result.total);
          this.isLoading.set(false);
          if (this.auth.role() === 'Student') {
            this.loadStudentCreateEligibility();
          } else {
            this.studentEligibilityResolved.set(true);
            this.studentHasBlockingApplication.set(false);
          }
        },
        error: () => {
          this.errorMessage.set('Не удалось загрузить заявки.');
          this.isLoading.set(false);
          if (this.auth.role() === 'Student') {
            this.studentEligibilityResolved.set(true);
            this.studentHasBlockingApplication.set(false);
          }
        },
      });
  }

  /** Согласовано с бэкендом: есть заявка не в «негативных» терминальных статусах. */
  private loadStudentCreateEligibility(): void {
    this.studentEligibilityResolved.set(false);
    this.applicationsApi
      .getApplications({ page: 1, pageSize: STUDENT_CREATE_ELIGIBILITY_PAGE_SIZE })
      .subscribe({
        next: (result) => {
          const hasBlocking = result.items.some((a) =>
            isStatusBlockingNewApplication(a.status.codeName),
          );
          this.studentHasBlockingApplication.set(hasBlocking);
          this.studentEligibilityResolved.set(true);
        },
        error: () => {
          this.studentHasBlockingApplication.set(false);
          this.studentEligibilityResolved.set(true);
        },
      });
  }
}
