import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';

import { AuthService } from '../../../core/auth/auth.service';
import type { SupervisorRequestDto } from '../../../core/models/supervisor-request.models';
import { SupervisorRequestsApiService } from '../supervisor-requests-api.service';

@Component({
  selector: 'app-supervisor-requests-list',
  imports: [RouterLink, Button, DatePipe],
  templateUrl: './supervisor-requests-list.component.html',
  styleUrl: './supervisor-requests-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupervisorRequestsListComponent {
  private readonly auth = inject(AuthService);
  private readonly supervisorRequestsApi = inject(SupervisorRequestsApiService);

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

  constructor() {
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

  private loadRequests(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.supervisorRequestsApi
      .getRequests({
        page: this.page(),
        pageSize: this.pageSize,
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
}
