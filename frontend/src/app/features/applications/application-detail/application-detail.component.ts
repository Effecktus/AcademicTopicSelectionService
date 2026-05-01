import { DatePipe, NgClass } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { ConfirmationService } from 'primeng/api';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { Observable } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { APPLICATION_STATUS_BADGE_CLASS } from '../../../core/constants/application-status-styles';
import type {
  ApplicationActionSnapshotDto,
  ApplicationStatusCode,
  StudentApplicationDto,
  StudentApplicationDetailDto,
} from '../../../core/models/application.models';
import type { ProblemDetails } from '../../../core/models/common.models';
import { ApplicationsApiService } from '../applications-api.service';

@Component({
  selector: 'app-application-detail',
  imports: [RouterLink, DatePipe, NgClass, Button, Dialog, Textarea, ReactiveFormsModule],
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

  readonly application = signal<StudentApplicationDetailDto | null>(null);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isRejectDialogOpen = signal(false);
  readonly rejectMode = signal<'supervisor' | 'departmentHead'>('supervisor');
  readonly rejectCommentControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(2000)],
  });

  readonly role = this.auth.role;
  readonly actionHistory = computed(() => this.application()?.actions ?? []);
  readonly statusCode = computed(() => this.application()?.status.codeName ?? null);

  readonly canCancel = computed(() => {
    if (this.role() !== 'Student') return false;
    return this.statusCode() === 'Pending' || this.statusCode() === 'ApprovedBySupervisor';
  });

  readonly canApproveOrRejectBySupervisor = computed(() => {
    return this.role() === 'Teacher' && this.statusCode() === 'Pending';
  });

  readonly canSubmitToDepartmentHead = computed(() => {
    return this.role() === 'Teacher' && this.statusCode() === 'ApprovedBySupervisor';
  });

  readonly canApproveOrRejectByDepartmentHead = computed(() => {
    return this.role() === 'DepartmentHead' && this.statusCode() === 'PendingDepartmentHead';
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage.set('Некорректный идентификатор заявки.');
      this.isLoading.set(false);
      return;
    }

    this.loadApplication(id);
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

  approveBySupervisor(): void {
    const item = this.application();
    if (!item || !this.canApproveOrRejectBySupervisor()) return;
    this.executeAction(
      this.applicationsApi.approve(item.id),
      item.id,
      'Не удалось одобрить заявку.',
    );
  }

  submitToDepartmentHead(): void {
    const item = this.application();
    if (!item || !this.canSubmitToDepartmentHead()) return;
    this.executeAction(
      this.applicationsApi.submitToDepartmentHead(item.id),
      item.id,
      'Не удалось передать заявку заведующему кафедры.',
    );
  }

  approveByDepartmentHead(): void {
    const item = this.application();
    if (!item || !this.canApproveOrRejectByDepartmentHead()) return;
    this.executeAction(
      this.applicationsApi.departmentHeadApprove(item.id),
      item.id,
      'Не удалось утвердить заявку.',
    );
  }

  openRejectDialog(mode: 'supervisor' | 'departmentHead'): void {
    this.rejectMode.set(mode);
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

    if (this.rejectMode() === 'supervisor' && this.canApproveOrRejectBySupervisor()) {
      this.executeAction(
        this.applicationsApi.reject(item.id, comment),
        item.id,
        'Не удалось отклонить заявку.',
        () => this.isRejectDialogOpen.set(false),
      );
      return;
    }

    if (this.rejectMode() === 'departmentHead' && this.canApproveOrRejectByDepartmentHead()) {
      this.executeAction(
        this.applicationsApi.departmentHeadReject(item.id, comment),
        item.id,
        'Не удалось отклонить заявку.',
        () => this.isRejectDialogOpen.set(false),
      );
    }
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
    return this.rejectMode() === 'supervisor' ? 'Отклонение научным руководителем' : 'Отклонение заведующим';
  }

  private loadApplication(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.applicationsApi.getById(id).subscribe({
      next: (item) => {
        this.application.set(item);
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
