import { DatePipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, merge } from 'rxjs';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import { AuthService } from '../../../core/auth/auth.service';
import { SUPERVISOR_REQUEST_STATUS_BADGE_CLASS } from '../../../core/constants/supervisor-request-status-styles';
import type { SupervisorRequestDto } from '../../../core/models/supervisor-request.models';
import { SupervisorRequestsApiService } from '../supervisor-requests-api.service';
import {
  currentYearDateRange,
  localDateToEndOfDayUtcIso,
  localDateToStartOfDayUtcIso,
} from '../../../core/utils/date-utils';

type SupervisorSortColumn = 'status' | 'counterparty' | 'createdAt';
type SupervisorRequestStatusCode = '' | 'Pending' | 'ApprovedBySupervisor' | 'RejectedBySupervisor' | 'Cancelled';

interface StatusOption {
  label: string;
  value: SupervisorRequestStatusCode;
}

@Component({
  selector: 'app-supervisor-requests-list',
  imports: [ReactiveFormsModule, InputText, Button, DatePipe, Select, NgClass],
  templateUrl: './supervisor-requests-list.component.html',
  styleUrl: './supervisor-requests-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupervisorRequestsListComponent {
  private readonly auth = inject(AuthService);
  private readonly supervisorRequestsApi = inject(SupervisorRequestsApiService);
  private readonly router = inject(Router);

  readonly role = this.auth.role;
  readonly requests = signal<SupervisorRequestDto[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly page = signal(1);
  readonly pageSize = 10;
  readonly total = signal(0);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  readonly canGoPrev = computed(() => this.page() > 1);
  readonly canGoNext = computed(() => this.page() < this.totalPages());
  readonly isTeacherView = computed(() => this.role() === 'Teacher');

  readonly sortColumn = signal<SupervisorSortColumn>('createdAt');
  readonly sortDir = signal<'asc' | 'desc'>('desc');
  readonly selectedStatusFilter = signal<SupervisorRequestStatusCode>('');
  readonly statusFilterControl = new FormControl<SupervisorRequestStatusCode>('', { nonNullable: true });
  readonly statusOptions: StatusOption[] = [
    { label: 'Все статусы', value: '' },
    { label: 'Ожидает ответа преподавателя', value: 'Pending' },
    { label: 'Одобрен преподавателем', value: 'ApprovedBySupervisor' },
    { label: 'Отклонен преподавателем', value: 'RejectedBySupervisor' },
    { label: 'Отменен студентом', value: 'Cancelled' },
  ];

  readonly dateFromControl = new FormControl(currentYearDateRange().from, { nonNullable: true });
  readonly dateToControl = new FormControl(currentYearDateRange().to, { nonNullable: true });
  readonly filteredRequests = computed(() => {
    const status = this.selectedStatusFilter();
    if (!status) return this.requests();
    return this.requests().filter((item) => item.status.codeName === status);
  });

  constructor() {
    this.selectedStatusFilter.set(this.statusFilterControl.value);

    merge(this.dateFromControl.valueChanges, this.dateToControl.valueChanges)
      .pipe(debounceTime(150), takeUntilDestroyed())
      .subscribe(() => {
        this.page.set(1);
        this.loadRequests();
      });

    this.statusFilterControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
      this.selectedStatusFilter.set(value);
      this.errorMessage.set(null);
    });

    this.loadRequests();
  }

  prevPage(): void {
    if (!this.canGoPrev()) return;
    this.page.update((value) => value - 1);
    this.loadRequests();
  }

  nextPage(): void {
    if (!this.canGoNext()) return;
    this.page.update((value) => value + 1);
    this.loadRequests();
  }

  teacherFullName(item: SupervisorRequestDto): string {
    return `${item.teacherLastName} ${item.teacherFirstName}`.trim();
  }

  studentFullName(item: SupervisorRequestDto): string {
    return `${item.studentLastName} ${item.studentFirstName}`.trim();
  }

  openRequest(requestId: string): void {
    void this.router.navigate(['/supervisor-requests', requestId]);
  }

  resetFilters(): void {
    this.statusFilterControl.setValue('');
    const range = currentYearDateRange();
    this.dateFromControl.setValue(range.from);
    this.dateToControl.setValue(range.to);
  }

  statusClass(statusCode: string): string {
    return SUPERVISOR_REQUEST_STATUS_BADGE_CLASS[statusCode] ?? 'status-pending';
  }

  toggleSort(column: SupervisorSortColumn): void {
    if (this.sortColumn() === column) {
      this.sortDir.update((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortColumn.set(column);
      this.sortDir.set('asc');
    }
    this.page.set(1);
    this.loadRequests();
  }

  sortIndicator(column: SupervisorSortColumn): string {
    if (this.sortColumn() !== column) return '';
    return this.sortDir() === 'asc' ? ' \u25b2' : ' \u25bc';
  }

  private loadRequests(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.supervisorRequestsApi
      .getRequests({
        page: this.page(),
        pageSize: this.pageSize,
        sort: this.supervisorSortApiValue(),
        createdFromUtc: localDateToStartOfDayUtcIso(this.dateFromControl.value),
        createdToUtc: localDateToEndOfDayUtcIso(this.dateToControl.value),
      })
      .subscribe({
        next: (result) => {
          this.requests.set(result.items);
          this.total.set(result.total);
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set('Не удалось загрузить запросы на научное руководство.');
          this.isLoading.set(false);
        },
      });
  }

  private supervisorSortApiValue(): string {
    const col = this.sortColumn();
    const asc = this.sortDir() === 'asc';
    const pairs: Record<SupervisorSortColumn, readonly [string, string]> = {
      status: ['statusAsc', 'statusDesc'],
      counterparty: ['counterpartyAsc', 'counterpartyDesc'],
      createdAt: ['createdAtAsc', 'createdAtDesc'],
    };
    const pair = pairs[col];
    return asc ? pair[0] : pair[1];
  }
}
