import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { ConfirmationService } from 'primeng/api';

import { AuthService } from '../../../core/auth/auth.service';
import type { ProblemDetails } from '../../../core/models/common.models';
import type { TopicDto } from '../../../core/models/topic.models';
import { TopicsApiService } from '../topics-api.service';

@Component({
  selector: 'app-topic-detail',
  imports: [RouterLink, Button, DatePipe],
  templateUrl: './topic-detail.component.html',
  styleUrl: './topic-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly topicsApi = inject(TopicsApiService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly topic = signal<TopicDto | null>(null);
  readonly isLoading = signal(true);
  readonly isDeleting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly canManageTopic = computed(() => {
    const topic = this.topic();
    return (
      !!topic &&
      this.auth.role() === 'Teacher' &&
      this.auth.currentUser()?.userId === topic.createdByUserId
    );
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.isLoading.set(false);
      this.errorMessage.set('Некорректный идентификатор темы.');
      return;
    }

    this.loadTopic(id);
  }

  deleteTopic(): void {
    const topic = this.topic();
    if (!topic || !this.canManageTopic()) return;

    this.confirmationService.confirm({
      header: 'Удаление темы',
      message: 'Тема будет удалена без возможности восстановления. Продолжить?',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Удалить',
      rejectLabel: 'Отмена',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.isDeleting.set(true);
        this.topicsApi.deleteTopic(topic.id).subscribe({
          next: () => {
            this.isDeleting.set(false);
            void this.router.navigateByUrl('/topics');
          },
          error: (err: HttpErrorResponse) => {
            this.isDeleting.set(false);
            this.errorMessage.set(this.resolveDeleteError(err));
          },
        });
      },
    });
  }

  private loadTopic(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.topicsApi.getTopicById(id).subscribe({
      next: (topic) => {
        this.topic.set(topic);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Не удалось загрузить тему.');
        this.isLoading.set(false);
      },
    });
  }

  private resolveDeleteError(err: HttpErrorResponse): string {
    const detail = (err.error as ProblemDetails | null)?.detail?.trim();
    if (detail) {
      return detail;
    }

    if (err.status === 403) {
      return 'Удалять тему может только ее автор.';
    }

    if (err.status === 404) {
      return 'Тема не найдена.';
    }

    return 'Не удалось удалить тему.';
  }
}
