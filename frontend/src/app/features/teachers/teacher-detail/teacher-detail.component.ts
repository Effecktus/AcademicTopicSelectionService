import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';

import { AuthService } from '../../../core/auth/auth.service';
import type { ProblemDetails } from '../../../core/models/common.models';
import type { TeacherDto } from '../../../core/models/teacher.models';
import type { TopicDto } from '../../../core/models/topic.models';
import { SupervisorRequestsApiService } from '../../supervisor-requests/supervisor-requests-api.service';
import { TopicsApiService } from '../../topics/topics-api.service';
import { TeachersApiService } from '../teachers-api.service';

@Component({
  selector: 'app-teacher-detail',
  imports: [RouterLink, Button],
  templateUrl: './teacher-detail.component.html',
  styleUrl: './teacher-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeacherDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  private readonly teachersApi = inject(TeachersApiService);
  private readonly topicsApi = inject(TopicsApiService);
  private readonly supervisorRequestsApi = inject(SupervisorRequestsApiService);

  readonly teacher = signal<TeacherDto | null>(null);
  readonly topics = signal<TopicDto[]>([]);
  readonly isLoading = signal(true);
  readonly isCreatingRequest = signal(false);
  readonly hasCreatedSupervisorRequest = signal(false);
  readonly requestErrorMessage = signal<string | null>(null);
  readonly requestSuccessMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly canCreateSupervisorRequest = computed(() => {
    return (
      this.auth.role() === 'Student' &&
      this.teacher() !== null &&
      !this.hasCreatedSupervisorRequest()
    );
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.isLoading.set(false);
      this.errorMessage.set('Некорректный идентификатор преподавателя.');
      return;
    }

    this.loadTeacher(id);
  }

  formatFullName(): string {
    const teacher = this.teacher();
    if (!teacher) return '';
    return [teacher.lastName, teacher.firstName, teacher.middleName].filter(Boolean).join(' ');
  }

  createSupervisorRequest(): void {
    const teacher = this.teacher();
    if (!teacher || !this.canCreateSupervisorRequest()) return;

    this.isCreatingRequest.set(true);
    this.requestErrorMessage.set(null);
    this.requestSuccessMessage.set(null);

    this.supervisorRequestsApi.create(teacher.userId).subscribe({
      next: () => {
        this.isCreatingRequest.set(false);
        this.hasCreatedSupervisorRequest.set(true);
        this.requestSuccessMessage.set('Запрос отправлен. Теперь он доступен в разделе "Мои запросы".');
      },
      error: (err: HttpErrorResponse) => {
        this.isCreatingRequest.set(false);
        this.requestErrorMessage.set(this.resolveCreateRequestError(err));
      },
    });
  }

  private loadTeacher(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.teachersApi.getTeacherById(id).subscribe({
      next: (teacher) => {
        this.teacher.set(teacher);
        this.loadTeacherTopics(teacher.userId);

        if (this.auth.role() === 'Student') {
          this.checkExistingRequest(teacher.userId);
        }
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('Не удалось загрузить карточку преподавателя.');
      },
    });
  }

  private checkExistingRequest(teacherUserId: string): void {
    this.supervisorRequestsApi.getRequests({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => {
        const hasPending = result.items.some(
          (r) => r.teacherUserId === teacherUserId && r.status.codeName === 'Pending',
        );
        if (hasPending) {
          this.hasCreatedSupervisorRequest.set(true);
          this.requestSuccessMessage.set('У вас уже есть активный запрос к этому преподавателю.');
        }
      },
      error: () => {
        // Не критично: при ошибке кнопка остаётся видимой, а 409 обработается при попытке отправки
      },
    });
  }

  private resolveCreateRequestError(err: HttpErrorResponse): string {
    const detail = (err.error as ProblemDetails | null)?.detail?.trim();
    if (detail) {
      return detail;
    }

    if (err.status === 409) {
      return 'Активный запрос этому преподавателю уже существует.';
    }

    if (err.status === 403) {
      return 'Отправлять запрос может только студент.';
    }

    return 'Не удалось отправить запрос на научное руководство.';
  }

  private loadTeacherTopics(teacherUserId: string): void {
    this.topicsApi
      .getTopics({
        createdByUserId: teacherUserId,
        creatorTypeCodeName: 'Teacher',
        page: 1,
        pageSize: 20,
        sort: 'createdAtDesc',
      })
      .subscribe({
        next: (result) => {
          this.topics.set(result.items);
          this.isLoading.set(false);
        },
        error: () => {
          this.topics.set([]);
          this.isLoading.set(false);
        },
      });
  }
}
