import { DatePipe, NgClass } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { ConfirmationService } from 'primeng/api';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { catchError, of, switchMap, timer } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { SUPERVISOR_REQUEST_STATUS_BADGE_CLASS } from '../../../core/constants/supervisor-request-status-styles';
import type { ProblemDetails } from '../../../core/models/common.models';
import type { SupervisorRequestDetailDto } from '../../../core/models/supervisor-request.models';
import { SupervisorRequestsApiService } from '../supervisor-requests-api.service';

@Component({
  selector: 'app-supervisor-request-detail',
  imports: [RouterLink, DatePipe, Button, Dialog, ReactiveFormsModule, Textarea, NgClass],
  templateUrl: './supervisor-request-detail.component.html',
  styleUrl: './supervisor-request-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupervisorRequestDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly supervisorRequestsApi = inject(SupervisorRequestsApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly request = signal<SupervisorRequestDetailDto | null>(null);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isApproveDialogOpen = signal(false);
  readonly isRejectDialogOpen = signal(false);
  readonly approveCommentControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.maxLength(2000)],
  });
  readonly rejectCommentControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(2000)],
  });

  readonly canApproveOrReject = computed(() => {
    const item = this.request();
    return this.auth.role() === 'Teacher' && item?.status.codeName === 'Pending';
  });

  readonly canCancel = computed(() => {
    const item = this.request();
    return this.auth.role() === 'Student' && item?.status.codeName === 'Pending';
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.isLoading.set(false);
      this.errorMessage.set('Некорректный идентификатор запроса.');
      return;
    }

    this.loadRequest(id);
    this.startRequestRefreshPolling(id);
  }

  /** Подтягивает статус с сервера, чтобы вторая сторона не видела устаревшее состояние. */
  private startRequestRefreshPolling(id: string): void {
    timer(10_000, 10_000)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap(() => this.supervisorRequestsApi.getById(id).pipe(catchError(() => of(null)))),
      )
      .subscribe((item) => {
        if (item) {
          this.request.set(item);
        }
      });
  }

  studentFullName(item: SupervisorRequestDetailDto): string {
    return `${item.studentLastName} ${item.studentFirstName}`.trim();
  }

  teacherFullName(item: SupervisorRequestDetailDto): string {
    return `${item.teacherLastName} ${item.teacherFirstName}`.trim();
  }

  statusClass(statusCode: string): string {
    return SUPERVISOR_REQUEST_STATUS_BADGE_CLASS[statusCode] ?? 'status-pending';
  }

  openApproveDialog(): void {
    this.approveCommentControl.reset('');
    this.approveCommentControl.markAsPristine();
    this.approveCommentControl.markAsUntouched();
    this.isApproveDialogOpen.set(true);
  }

  confirmApprove(): void {
    const item = this.request();
    if (!item || !this.canApproveOrReject()) return;

    if (this.approveCommentControl.invalid) {
      this.approveCommentControl.markAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const comment = this.approveCommentControl.value.trim();
    this.supervisorRequestsApi.approve(item.id, comment || null).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.isApproveDialogOpen.set(false);
        this.loadRequest(item.id);
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving.set(false);
        this.errorMessage.set(this.resolveActionError(err, 'Не удалось принять запрос.'));
      },
    });
  }

  openRejectDialog(): void {
    this.rejectCommentControl.reset('');
    this.rejectCommentControl.markAsPristine();
    this.rejectCommentControl.markAsUntouched();
    this.isRejectDialogOpen.set(true);
  }

  reject(): void {
    const item = this.request();
    if (!item || !this.canApproveOrReject()) return;

    if (this.rejectCommentControl.invalid) {
      this.rejectCommentControl.markAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    this.supervisorRequestsApi.reject(item.id, this.rejectCommentControl.value.trim()).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.isRejectDialogOpen.set(false);
        this.loadRequest(item.id);
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving.set(false);
        this.errorMessage.set(this.resolveActionError(err, 'Не удалось отклонить запрос.'));
      },
    });
  }

  cancel(): void {
    const item = this.request();
    if (!item || !this.canCancel()) return;

    this.confirmationService.confirm({
      header: 'Отмена запроса',
      message: 'Запрос будет отменен. Продолжить?',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Отменить запрос',
      rejectLabel: 'Назад',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary' },
      accept: () => {
        this.isSaving.set(true);
        this.errorMessage.set(null);

        this.supervisorRequestsApi.cancel(item.id).subscribe({
          next: () => {
            this.isSaving.set(false);
            this.loadRequest(item.id);
          },
          error: (err: HttpErrorResponse) => {
            this.isSaving.set(false);
            this.errorMessage.set(this.resolveActionError(err, 'Не удалось отменить запрос.'));
          },
        });
      },
    });
  }

  private loadRequest(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.supervisorRequestsApi.getById(id).subscribe({
      next: (item) => {
        this.request.set(item);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Не удалось загрузить запрос.');
        this.isLoading.set(false);
      },
    });
  }

  private resolveActionError(err: HttpErrorResponse, fallback: string): string {
    const detail = (err.error as ProblemDetails | null)?.detail?.trim();
    return detail || fallback;
  }
}
