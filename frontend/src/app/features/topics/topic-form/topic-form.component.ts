import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';

import { AuthService } from '../../../core/auth/auth.service';
import type { TopicDto } from '../../../core/models/topic.models';
import { TopicsApiService } from '../topics-api.service';

interface TopicFormModel {
  title: FormControl<string>;
  description: FormControl<string>;
  statusCodeName: FormControl<'Active' | 'Inactive'>;
}

@Component({
  selector: 'app-topic-form',
  imports: [ReactiveFormsModule, RouterLink, InputText, Textarea, Select, Button],
  templateUrl: './topic-form.component.html',
  styleUrl: './topic-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicFormComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly topicsApi = inject(TopicsApiService);
  private readonly auth = inject(AuthService);

  private readonly topicId = this.route.snapshot.paramMap.get('id');

  readonly isEditMode = this.topicId !== null;
  readonly isLoading = signal(this.isEditMode);
  readonly isSaving = signal(false);
  readonly canEditExistingTopic = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isFormDisabled = computed(
    () => this.isLoading() || this.isSaving() || (this.isEditMode && !this.canEditExistingTopic()),
  );
  readonly pageTitle = computed(() => {
    if (!this.isEditMode) return 'Новая тема';
    return this.canEditExistingTopic() ? 'Редактирование темы' : 'Просмотр темы';
  });
  readonly pageDescription = computed(() => {
    if (!this.isEditMode) return 'Создание новой темы ВКР.';
    return this.canEditExistingTopic()
      ? 'Обновление существующей темы ВКР.'
      : 'Тема доступна только для просмотра.';
  });

  readonly statusOptions = [
    { label: 'Активна', value: 'Active' as const },
    { label: 'Неактивна', value: 'Inactive' as const },
  ];

  readonly form = new FormGroup<TopicFormModel>({
    title: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(500)],
    }),
    description: new FormControl('', {
      nonNullable: true,
    }),
    statusCodeName: new FormControl<'Active' | 'Inactive'>('Active', {
      nonNullable: true,
    }),
  });

  constructor() {
    if (this.topicId) {
      this.loadTopic(this.topicId);
    }
  }

  submit(): void {
    if (this.isEditMode && !this.canEditExistingTopic()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const { title, description, statusCodeName } = this.form.getRawValue();
    const statusForSave = this.isEditMode ? statusCodeName : 'Active';
    const normalizedDescription = description.trim() === '' ? null : description.trim();

    if (!this.topicId) {
      this.topicsApi
        .createTopic({
          title: title.trim(),
          description: normalizedDescription,
          creatorTypeCodeName: 'Teacher',
          statusCodeName: statusForSave,
        })
        .subscribe({
          next: (topic) => {
            this.isSaving.set(false);
            void this.router.navigate(['/topics', topic.id]);
          },
          error: (err: HttpErrorResponse) => {
            this.isSaving.set(false);
            this.errorMessage.set(this.resolveSaveError(err, 'Не удалось создать тему.'));
          },
        });
      return;
    }

    this.topicsApi
      .patchTopic(this.topicId, {
        title: title.trim(),
        description: normalizedDescription,
        statusCodeName: statusForSave,
      })
      .subscribe({
        next: (topic: TopicDto) => {
          this.isSaving.set(false);
          void this.router.navigate(['/topics', topic.id]);
        },
        error: (err: HttpErrorResponse) => {
          this.isSaving.set(false);
          this.errorMessage.set(this.resolveSaveError(err, 'Не удалось обновить тему.'));
        },
      });
  }

  private loadTopic(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.topicsApi.getTopicById(id).subscribe({
      next: (topic) => {
        this.canEditExistingTopic.set(
          this.auth.role() === 'Teacher' && this.auth.currentUser()?.userId === topic.createdByUserId,
        );
        if (this.canEditExistingTopic()) {
          this.form.enable({ emitEvent: false });
        } else {
          this.form.disable({ emitEvent: false });
        }

        this.form.patchValue({
          title: topic.title,
          description: topic.description ?? '',
          statusCodeName: topic.status.codeName === 'Inactive' ? 'Inactive' : 'Active',
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Не удалось загрузить тему.');
        this.isLoading.set(false);
      },
    });
  }

  private resolveSaveError(err: HttpErrorResponse, fallback: string): string {
    if (err.status === 403) {
      return 'Недостаточно прав для выполнения операции.';
    }
    if (err.status === 400) {
      return 'Проверьте заполнение полей формы.';
    }
    return fallback;
  }
}
