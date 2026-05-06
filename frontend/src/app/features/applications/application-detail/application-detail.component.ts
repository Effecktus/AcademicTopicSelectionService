import { DatePipe, NgClass } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  model,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { ConfirmationService } from 'primeng/api';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { catchError, merge, type Observable, of, switchMap, timer } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { APPLICATION_STATUS_BADGE_CLASS } from '../../../core/constants/application-status-styles';
import type {
  ApplicationActionSnapshotDto,
  ApplicationStatusCode,
  ApplicationTopicChangeHistoryEntryDto,
  StudentApplicationDto,
  StudentApplicationDetailDto,
} from '../../../core/models/application.models';
import type { ProblemDetails } from '../../../core/models/common.models';
import { ApplicationsApiService } from '../applications-api.service';
import { ChatWindowComponent } from './chat-window/chat-window.component';

type RejectDialogMode = 'supervisor' | 'departmentHead' | 'supervisorReturn' | 'departmentHeadReturn';

@Component({
  selector: 'app-application-detail',
  imports: [
    RouterLink,
    DatePipe,
    NgClass,
    Button,
    Dialog,
    Textarea,
    InputText,
    ReactiveFormsModule,
    ChatWindowComponent,
  ],
  templateUrl: './application-detail.component.html',
  styleUrl: './application-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicationDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly applicationsApi = inject(ApplicationsApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly application = signal<StudentApplicationDetailDto | null>(null);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isRejectDialogOpen = signal(false);
  readonly rejectMode = signal<RejectDialogMode>('supervisor');
  readonly rejectCommentControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(2000)],
  });

  /** Двусторонняя привязка с p-dialog: иначе visibleChange может сразу сбросить открытие. */
  readonly approveDialogVisible = model(false);
  readonly approveMode = signal<'supervisor' | 'departmentHead'>('supervisor');
  readonly approveCommentControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.maxLength(2000)],
  });

  readonly topicTitleControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(500)],
  });
  readonly topicDescriptionControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.maxLength(4000)],
  });

  /** Счётчик правок полей темы — для computed с OnPush (FormControl не сигнал). */
  private readonly topicEditTick = signal(0);

  readonly role = this.auth.role;
  readonly actionHistory = computed(() => this.application()?.actions ?? []);
  readonly topicChangeHistory = computed(() => this.application()?.topicChangeHistory ?? []);
  readonly lastTopicChangeAt = computed(() => {
    const history = this.topicChangeHistory();
    return history.length > 0 ? history[history.length - 1].createdAt : null;
  });
  readonly statusCode = computed(() => this.application()?.status.codeName ?? null);

  /** Чат API только для студента и научрука; заведующий и админ не участники. */
  readonly canViewApplicationChat = computed(() => {
    const r = this.role();
    return r === 'Student' || r === 'Teacher';
  });

  readonly canCancel = computed(() => {
    if (this.role() !== 'Student') return false;
    const s = this.statusCode();
    return s === 'Pending' || s === 'ApprovedBySupervisor' || s === 'OnEditing';
  });

  readonly canStudentEditTopic = computed(() => {
    return this.role() === 'Student' && this.statusCode() === 'OnEditing';
  });

  readonly canSubmitToSupervisor = computed(() => {
    return this.role() === 'Student' && this.statusCode() === 'OnEditing';
  });

  /** Показать «Сохранить тему»: студент в OnEditing и текст отличается от загруженного с сервера. */
  readonly hasTopicUnsavedChanges = computed(() => {
    this.topicEditTick();
    const app = this.application();
    if (!app || this.role() !== 'Student' || this.statusCode() !== 'OnEditing') {
      return false;
    }
    const title = this.topicTitleControl.value.trim();
    const desc = this.topicDescriptionControl.value.trim();
    const baseTitle = app.topicTitle.trim();
    const baseDesc = (app.topicDescription ?? '').trim();
    return title !== baseTitle || desc !== baseDesc;
  });

  readonly canApproveOrRejectBySupervisor = computed(() => {
    return this.role() === 'Teacher' && this.statusCode() === 'Pending';
  });

  readonly canReturnForEditingBySupervisor = computed(() => {
    return this.role() === 'Teacher' && this.statusCode() === 'Pending';
  });

  readonly canApproveOrRejectByDepartmentHead = computed(() => {
    return this.role() === 'DepartmentHead' && this.statusCode() === 'PendingDepartmentHead';
  });

  readonly canReturnForEditingByDepartmentHead = computed(() => {
    return this.role() === 'DepartmentHead' && this.statusCode() === 'PendingDepartmentHead';
  });

  constructor() {
    merge(this.topicTitleControl.valueChanges, this.topicDescriptionControl.valueChanges)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.topicEditTick.update((n) => n + 1));

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage.set('Некорректный идентификатор заявки.');
      this.isLoading.set(false);
      return;
    }

    this.loadApplication(id);
    this.startApplicationRefreshPolling(id);
  }

  /** Подтягивает статус и историю с сервера, чтобы другая роль не «застревала» на старом состоянии. */
  private startApplicationRefreshPolling(id: string): void {
    timer(10_000, 10_000)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap(() => this.applicationsApi.getById(id).pipe(catchError(() => of(null)))),
      )
      .subscribe((item) => {
        if (!item) {
          return;
        }
        this.application.set(item);
        if (!this.hasTopicUnsavedChanges()) {
          this.topicTitleControl.setValue(item.topicTitle, { emitEvent: false });
          this.topicDescriptionControl.setValue(item.topicDescription ?? '', { emitEvent: false });
          this.topicTitleControl.markAsPristine();
          this.topicDescriptionControl.markAsPristine();
        }
        this.topicEditTick.update((n) => n + 1);
      });
  }

  statusClass(statusCode: ApplicationStatusCode): string {
    return APPLICATION_STATUS_BADGE_CLASS[statusCode];
  }

  supervisorFullName(item: StudentApplicationDetailDto): string {
    return `${item.supervisorLastName} ${item.supervisorFirstName}`.trim();
  }

  studentFullName(item: StudentApplicationDetailDto): string {
    return `${item.studentLastName} ${item.studentFirstName}`.trim();
  }

  actionResponsibleFullName(action: ApplicationActionSnapshotDto): string {
    return `${action.responsibleLastName} ${action.responsibleFirstName}`.trim();
  }

  topicChangeAuthorFullName(row: ApplicationTopicChangeHistoryEntryDto): string {
    return `${row.changedByLastName} ${row.changedByFirstName}`.trim();
  }

  topicChangeValueOrDash(value: string | null): string {
    if (value === null || value === '') {
      return '—';
    }
    return value;
  }

  openApproveDialog(mode: 'supervisor' | 'departmentHead'): void {
    this.approveMode.set(mode);
    this.approveCommentControl.reset('');
    this.approveCommentControl.markAsPristine();
    this.approveCommentControl.markAsUntouched();
    this.approveDialogVisible.set(true);
  }

  confirmApprove(): void {
    const item = this.application();
    if (!item) return;

    if (this.approveCommentControl.invalid) {
      this.approveCommentControl.markAsTouched();
      return;
    }

    const comment = this.approveCommentControl.value.trim();
    const payload = comment || null;

    if (this.approveMode() === 'supervisor' && this.canApproveOrRejectBySupervisor()) {
      this.executeAction(
        this.applicationsApi.approve(item.id, payload),
        item.id,
        'Не удалось одобрить заявку.',
        () => this.approveDialogVisible.set(false),
      );
      return;
    }

    if (this.approveMode() === 'departmentHead' && this.canApproveOrRejectByDepartmentHead()) {
      this.executeAction(
        this.applicationsApi.departmentHeadApprove(item.id, payload),
        item.id,
        'Не удалось утвердить заявку.',
        () => this.approveDialogVisible.set(false),
      );
    }
  }

  openRejectDialog(mode: 'supervisor' | 'departmentHead'): void {
    this.rejectMode.set(mode);
    this.rejectCommentControl.reset('');
    this.rejectCommentControl.markAsPristine();
    this.rejectCommentControl.markAsUntouched();
    this.isRejectDialogOpen.set(true);
  }

  openReturnForEditingDialog(mode: 'supervisor' | 'departmentHead'): void {
    this.rejectMode.set(mode === 'supervisor' ? 'supervisorReturn' : 'departmentHeadReturn');
    this.rejectCommentControl.reset('');
    this.rejectCommentControl.markAsPristine();
    this.rejectCommentControl.markAsUntouched();
    this.isRejectDialogOpen.set(true);
  }

  reject(): void {
    const item = this.application();
    if (!item) return;
    if (this.rejectCommentControl.invalid) {
      this.rejectCommentControl.markAsTouched();
      return;
    }

    const comment = this.rejectCommentControl.value.trim();
    if (!comment) {
      this.rejectCommentControl.markAsTouched();
      return;
    }

    const mode = this.rejectMode();

    if (mode === 'supervisor' && this.canApproveOrRejectBySupervisor()) {
      this.executeAction(
        this.applicationsApi.reject(item.id, comment),
        item.id,
        'Не удалось отклонить заявку.',
        () => this.isRejectDialogOpen.set(false),
      );
      return;
    }

    if (mode === 'departmentHead' && this.canApproveOrRejectByDepartmentHead()) {
      this.executeAction(
        this.applicationsApi.departmentHeadReject(item.id, comment),
        item.id,
        'Не удалось отклонить заявку.',
        () => this.isRejectDialogOpen.set(false),
      );
      return;
    }

    if (mode === 'supervisorReturn' && this.canReturnForEditingBySupervisor()) {
      this.executeAction(
        this.applicationsApi.returnForEditing(item.id, comment),
        item.id,
        'Не удалось вернуть заявку на редактирование.',
        () => this.isRejectDialogOpen.set(false),
      );
      return;
    }

    if (mode === 'departmentHeadReturn' && this.canReturnForEditingByDepartmentHead()) {
      this.executeAction(
        this.applicationsApi.departmentHeadReturnForEditing(item.id, comment),
        item.id,
        'Не удалось вернуть заявку на редактирование.',
        () => this.isRejectDialogOpen.set(false),
      );
    }
  }

  saveTopicEdits(): void {
    const item = this.application();
    if (!item || !this.canStudentEditTopic()) return;

    if (this.topicTitleControl.invalid || this.topicDescriptionControl.invalid) {
      this.topicTitleControl.markAsTouched();
      this.topicDescriptionControl.markAsTouched();
      return;
    }

    const title = this.topicTitleControl.value.trim();
    const descRaw = this.topicDescriptionControl.value.trim();
    this.executeAction(
      this.applicationsApi.updateTopic(item.id, {
        title,
        description: descRaw.length > 0 ? descRaw : null,
      }),
      item.id,
      'Не удалось сохранить изменения темы.',
    );
  }

  submitToSupervisor(): void {
    const item = this.application();
    if (!item || !this.canSubmitToSupervisor()) return;

    this.confirmationService.confirm({
      header: 'Передача научному руководителю',
      message: 'Заявка будет отправлена на рассмотрение. Продолжить?',
      icon: 'pi pi-send',
      acceptLabel: 'Передать',
      rejectLabel: 'Назад',
      acceptButtonProps: { severity: 'primary' },
      rejectButtonProps: { severity: 'secondary' },
      accept: () => {
        this.executeAction(
          this.applicationsApi.submitToSupervisor(item.id),
          item.id,
          'Не удалось передать заявку научному руководителю.',
        );
      },
    });
  }

  cancel(): void {
    const item = this.application();
    if (!item || !this.canCancel()) return;

    this.confirmationService.confirm({
      header: 'Отмена заявки',
      message: 'Заявка будет отменена. Продолжить?',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Отменить заявку',
      rejectLabel: 'Назад',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary' },
      accept: () => {
        this.executeAction(this.applicationsApi.cancel(item.id), item.id, 'Не удалось отменить заявку.');
      },
    });
  }

  rejectDialogTitle(): string {
    switch (this.rejectMode()) {
      case 'supervisor':
        return 'Отклонение научным руководителем';
      case 'departmentHead':
        return 'Отклонение заведующим';
      case 'supervisorReturn':
        return 'Возврат на редактирование (научный руководитель)';
      case 'departmentHeadReturn':
        return 'Возврат на редактирование (заведующий кафедрой)';
      default:
        return '';
    }
  }

  rejectDialogCommentPlaceholder(): string {
    return this.rejectMode() === 'supervisor' || this.rejectMode() === 'departmentHead'
      ? 'Укажите причину отклонения'
      : 'Укажите, что нужно доработать';
  }

  rejectDialogConfirmLabel(): string {
    return this.rejectMode() === 'supervisor' || this.rejectMode() === 'departmentHead'
      ? 'Отклонить'
      : 'Вернуть на доработку';
  }

  approveDialogTitle(): string {
    return this.approveMode() === 'supervisor'
      ? 'Одобрение заявки научным руководителем'
      : 'Утверждение заявки заведующим кафедрой';
  }

  approveDialogPrimaryLabel(): string {
    return this.approveMode() === 'supervisor' ? 'Одобрить' : 'Утвердить';
  }

  private loadApplication(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.applicationsApi.getById(id).subscribe({
      next: (item) => {
        this.application.set(item);
        this.topicTitleControl.setValue(item.topicTitle, { emitEvent: false });
        this.topicDescriptionControl.setValue(item.topicDescription ?? '', { emitEvent: false });
        this.topicTitleControl.markAsPristine();
        this.topicDescriptionControl.markAsPristine();
        this.topicTitleControl.markAsUntouched();
        this.topicDescriptionControl.markAsUntouched();
        this.topicEditTick.update((n) => n + 1);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Заявка не найдена или недоступна.');
        this.isLoading.set(false);
        void this.router.navigate(['/applications']);
      },
    });
  }

  private executeAction(
    request$: Observable<StudentApplicationDto>,
    id: string,
    fallbackError: string,
    onSuccess?: () => void,
  ): void {
    this.isSaving.set(true);
    this.errorMessage.set(null);

    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        onSuccess?.();
        this.loadApplication(id);
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving.set(false);
        this.errorMessage.set(this.resolveActionError(err, fallbackError));
      },
    });
  }

  private resolveActionError(err: HttpErrorResponse, fallback: string): string {
    const detail = (err.error as ProblemDetails | null)?.detail?.trim();
    return detail || fallback;
  }
}
