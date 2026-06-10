import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpEventType } from '@angular/common/http';
import { filter, switchMap } from 'rxjs';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { ProgressBar } from 'primeng/progressbar';
import { MessageService, SharedModule } from 'primeng/api';

import type { GraduateWorkDto, GraduateWorkFileType, GraduateWorkStatusCode } from '../../../core/models/graduate-work.models';
import { GraduateWorksApiService } from '../../graduate-works/graduate-works-api.service';

@Component({
  selector: 'app-gw-detail-dialog',
  imports: [
    ReactiveFormsModule,
    Button,
    Dialog,
    InputText,
    InputNumber,
    ProgressBar,
    SharedModule,
  ],
  templateUrl: './gw-detail-dialog.component.html',
  styleUrl: './gw-detail-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GwDetailDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly gwApi = inject(GraduateWorksApiService);
  private readonly messageService = inject(MessageService);

  readonly visible = model(false);
  readonly gw = input.required<GraduateWorkDto>();
  readonly gwUpdated = output<void>();

  readonly isSubmitting = signal(false);
  readonly serverError = signal<string | null>(null);
  readonly thesisFile = signal<File | null>(null);
  readonly presentationFile = signal<File | null>(null);
  readonly uploadProgress = signal<number>(0);

  readonly form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(500)]],
    year: [new Date().getFullYear(), [Validators.required, Validators.min(2000), Validators.max(2100)]],
    grade: [null as number | null, [Validators.min(0), Validators.max(100)]],
    commissionMembers: [null as string | null],
  });

  constructor() {
    effect(() => {
      const gw = this.gw();
      this.form.patchValue({
        title: gw.title,
        year: gw.year,
        grade: gw.grade ?? null,
        commissionMembers: gw.commissionMembers ?? null,
      });
      this.thesisFile.set(null);
      this.presentationFile.set(null);
      this.uploadProgress.set(0);
      this.serverError.set(null);
    });
  }

  statusBadgeClass(statusCode: GraduateWorkStatusCode): string {
    return statusCode === 'Completed' ? 'badge badge--completed' : 'badge badge--draft';
  }

  hasError(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl?.touched);
  }

  onThesisFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.thesisFile.set(input.files?.[0] ?? null);
  }

  onPresentationFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.presentationFile.set(input.files?.[0] ?? null);
  }

  download(fileType: GraduateWorkFileType): void {
    this.gwApi.getDownloadUrl(this.gw().id, fileType).subscribe({
      next: ({ url }) => window.open(url, '_blank'),
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    this.isSubmitting.set(true);
    this.serverError.set(null);
    this.uploadProgress.set(0);

    this.gwApi
      .update(this.gw().id, {
        title: v.title!,
        year: v.year!,
        grade: v.grade ?? null,
        commissionMembers: v.commissionMembers ?? null,
      })
      .subscribe({
        next: () => this.uploadFiles(),
        error: (err) => {
          this.isSubmitting.set(false);
          this.serverError.set(err?.error?.detail ?? 'Не удалось сохранить изменения.');
        },
      });
  }

  private uploadFiles(): void {
    const id = this.gw().id;
    const thesis = this.thesisFile();
    const presentation = this.presentationFile();
    const totalSteps = (thesis ? 1 : 0) + (presentation ? 1 : 0);
    let done = 0;

    const finish = () => {
      this.isSubmitting.set(false);
      this.messageService.add({
        severity: 'success',
        summary: 'Сохранено',
        detail: `ВКР обновлена.`,
      });
      this.visible.set(false);
      this.gwUpdated.emit();
    };

    const next = () => {
      done++;
      this.uploadProgress.set(Math.round((done / totalSteps) * 100));
      if (done === totalSteps) finish();
    };

    if (totalSteps === 0) {
      finish();
      return;
    }

    if (thesis) {
      this.gwApi
        .getUploadUrl(id, 'thesis')
        .pipe(
          switchMap(({ url }) => this.gwApi.uploadToStorage(url, thesis)),
          filter((e) => e.type === HttpEventType.Response),
          switchMap(() => this.gwApi.confirmUpload(id, 'thesis', thesis.name)),
        )
        .subscribe({ next, error: () => next() });
    }

    if (presentation) {
      this.gwApi
        .getUploadUrl(id, 'presentation')
        .pipe(
          switchMap(({ url }) => this.gwApi.uploadToStorage(url, presentation)),
          filter((e) => e.type === HttpEventType.Response),
          switchMap(() => this.gwApi.confirmUpload(id, 'presentation', presentation.name)),
        )
        .subscribe({ next, error: () => next() });
    }
  }
}
