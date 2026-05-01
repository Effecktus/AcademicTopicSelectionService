import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { SupervisorRequestsApiService } from '../../supervisor-requests/supervisor-requests-api.service';
import { TopicsApiService } from '../../topics/topics-api.service';
import type { ProblemDetails } from '../../../core/models/common.models';
import type { SupervisorRequestDto } from '../../../core/models/supervisor-request.models';
import type { TopicDto } from '../../../core/models/topic.models';
import { ApplicationsApiService } from '../applications-api.service';

type TopicSource = 'catalog' | 'custom';

function createTopicSourceValidator(sourceControl: () => TopicSource): (group: AbstractControl) => ValidationErrors | null {
  return (group: AbstractControl): ValidationErrors | null => {
    const topicId = group.get('topicId')?.value as string;
    const proposedTitle = (group.get('proposedTitle')?.value as string).trim();
    const source = sourceControl();

    const hasTopic = Boolean(topicId);
    const hasCustomTitle = Boolean(proposedTitle);

    if (hasTopic && hasCustomTitle) {
      return { topicSourceConflict: true };
    }

    if (source === 'catalog' && !hasTopic) {
      return { topicRequired: true };
    }

    if (source === 'custom' && !hasCustomTitle) {
      return { proposedTitleRequired: true };
    }

    return null;
  };
}

@Component({
  selector: 'app-application-create',
  imports: [RouterLink, ReactiveFormsModule, InputText, Textarea, Select, Button],
  templateUrl: './application-create.component.html',
  styleUrl: './application-create.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicationCreateComponent {
  private readonly topicsApi = inject(TopicsApiService);
  private readonly supervisorRequestsApi = inject(SupervisorRequestsApiService);
  private readonly applicationsApi = inject(ApplicationsApiService);
  private readonly router = inject(Router);

  readonly topicSource = signal<TopicSource>('catalog');
  readonly isSaving = signal(false);
  readonly isLoadingSupervisors = signal(true);
  readonly isLoadingTopics = signal(false);
  readonly approvedRequests = signal<SupervisorRequestDto[]>([]);
  readonly topicSuggestions = signal<TopicDto[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly topicSearchControl = new FormControl('', { nonNullable: true });
  readonly isSubmitAttempted = signal(false);

  readonly form = new FormGroup(
    {
      topicId: new FormControl('', { nonNullable: true }),
      proposedTitle: new FormControl('', { nonNullable: true }),
      proposedDescription: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(4000)] }),
    },
    { validators: [createTopicSourceValidator(() => this.topicSource())] },
  );

  readonly approvedSupervisorRequest = computed(() => this.approvedRequests()[0] ?? null);
  readonly hasApprovedSupervisors = computed(() => this.approvedSupervisorRequest() !== null);
  readonly approvedSupervisorName = computed(() => {
    const supervisor = this.approvedSupervisorRequest();
    return supervisor ? `${supervisor.teacherLastName} ${supervisor.teacherFirstName}`.trim() : '';
  });

  constructor() {
    this.topicSearchControl.valueChanges
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((query) => {
        if (this.topicSource() !== 'catalog') {
          return;
        }
        this.loadTopics(query.trim());
      });

    this.loadApprovedSupervisorRequests();
  }

  chooseSource(source: TopicSource): void {
    this.topicSource.set(source);
    this.errorMessage.set(null);

    if (source === 'catalog') {
      this.form.controls.proposedTitle.setValue('');
      this.form.controls.proposedDescription.setValue('');
      this.loadTopics(this.topicSearchControl.value.trim());
    } else {
      this.form.controls.topicId.setValue('');
      this.topicSuggestions.set([]);
    }

    this.form.updateValueAndValidity();
  }

  submit(): void {
    this.isSubmitAttempted.set(true);

    if (!this.hasApprovedSupervisors()) {
      this.errorMessage.set('Сначала получите одобрение научного руководителя.');
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const approvedSupervisorRequest = this.approvedSupervisorRequest();
    if (!approvedSupervisorRequest) {
      this.errorMessage.set('Сначала получите одобрение научного руководителя.');
      return;
    }

    const payload =
      this.topicSource() === 'catalog'
        ? {
            supervisorRequestId: approvedSupervisorRequest.id,
            topicId: raw.topicId,
          }
        : {
            supervisorRequestId: approvedSupervisorRequest.id,
            proposedTitle: raw.proposedTitle.trim(),
            proposedDescription: raw.proposedDescription.trim() || undefined,
          };

    this.isSaving.set(true);
    this.errorMessage.set(null);

    this.applicationsApi.create(payload).subscribe({
      next: (created) => {
        this.isSaving.set(false);
        void this.router.navigate(['/applications', created.id]);
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving.set(false);
        this.errorMessage.set(this.resolveApiError(err, 'Не удалось создать заявку.'));
      },
    });
  }

  private loadApprovedSupervisorRequests(): void {
    this.isLoadingSupervisors.set(true);
    this.errorMessage.set(null);

    this.supervisorRequestsApi
      .getRequests({ page: 1, pageSize: 100 })
      .subscribe({
        next: (result) => {
          this.approvedRequests.set(
            result.items.filter((item) => item.status.codeName === 'ApprovedBySupervisor'),
          );
          this.form.controls.topicId.setValue('');
          if (this.topicSource() === 'catalog') {
            this.loadTopics(this.topicSearchControl.value.trim());
          }
          this.isLoadingSupervisors.set(false);
        },
        error: () => {
          this.errorMessage.set('Не удалось загрузить список одобренных научных руководителей.');
          this.isLoadingSupervisors.set(false);
        },
      });
  }

  private loadTopics(query: string): void {
    const supervisor = this.approvedSupervisorRequest();
    if (!supervisor) {
      this.topicSuggestions.set([]);
      return;
    }

    this.isLoadingTopics.set(true);
    this.topicsApi
      .getTopics({
        query,
        statusCodeName: 'Active',
        creatorTypeCodeName: 'Teacher',
        createdByUserId: supervisor.teacherUserId,
        page: 1,
        pageSize: 20,
        sort: 'titleAsc',
      })
      .subscribe({
        next: (result) => {
          this.topicSuggestions.set(result.items);
          this.isLoadingTopics.set(false);
        },
        error: () => {
          this.topicSuggestions.set([]);
          this.isLoadingTopics.set(false);
        },
      });
  }

  private resolveApiError(err: HttpErrorResponse, fallback: string): string {
    const detail = (err.error as ProblemDetails | null)?.detail?.trim();
    return detail || fallback;
  }
}
