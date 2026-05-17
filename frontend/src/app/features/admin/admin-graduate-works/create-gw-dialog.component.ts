import {
  ChangeDetectionStrategy,
  Component,
  inject,
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
import { AutoComplete, type AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { ProgressBar } from 'primeng/progressbar';
import { MessageService, SharedModule } from 'primeng/api';

import type { StudentApplicationDto } from '../../../core/models/application.models';
import type { GraduateWorkDto } from '../../../core/models/graduate-work.models';
import { ApplicationsApiService } from '../../applications/applications-api.service';
import { GraduateWorksApiService } from '../../graduate-works/graduate-works-api.service';

@Component({
  selector: 'app-create-gw-dialog',
  imports: [
    ReactiveFormsModule,
    Button,
    Dialog,
    InputText,
    InputNumber,
    AutoComplete,
    ProgressBar,
    SharedModule,
  ],
  templateUrl: './create-gw-dialog.component.html',
  styleUrl: './create-gw-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateGwDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly applicationsApi = inject(ApplicationsApiService);
  private readonly gwApi = inject(GraduateWorksApiService);
  private readonly messageService = inject(MessageService);

  readonly visible = model(false);
  readonly gwCreated = output<GraduateWorkDto>();

  readonly isSubmitting = signal(false);
  readonly serverError = signal<string | null>(null);

  readonly applicationSuggestions = signal<StudentApplicationDto[]>([]);
  readonly selectedApplication = signal<StudentApplicationDto | null>(null);

  readonly thesisFile = signal<File | null>(null);
  readonly presentationFile = signal<File | null>(null);
  readonly uploadProgress = signal<number>(0);

  readonly currentYear = new Date().getFullYear();

  readonly form = this.fb.group({
    application: [null as StudentApplicationDto | null, Validators.required],
    title: ['', [Validators.required, Validators.maxLength(500)]],
    year: [this.currentYear, [Validators.required, Validators.min(2000), Validators.max(2100)]],
    grade: [null as number | null, [Validators.required, Validators.min(0), Validators.max(100)]],
    commissionMembers: ['', Validators.required],
  });

  searchApplications(event: AutoCompleteCompleteEvent): void {
    const query = event.query.trim();
    if (query.length < 2) {
      this.applicationSuggestions.set([]);
      return;
    }
    this.applicationsApi
      .getApplications({ page: 1, pageSize: 20, query })
      .subscribe((result) => this.applicationSuggestions.set(result.items));
  }

  getApplicationLabel(app: StudentApplicationDto): string {
    return `${app.studentLastName} ${app.studentFirstName} — ${app.topicTitle}`;
  }

  onThesisFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.thesisFile.set(input.files?.[0] ?? null);
  }

  onPresentationFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.presentationFile.set(input.files?.[0] ?? null);
  }

  onHide(): void {
    this.form.reset({ year: this.currentYear });
    this.serverError.set(null);
    this.thesisFile.set(null);
    this.presentationFile.set(null);
    this.uploadProgress.set(0);
  }

  hasError(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl?.touched);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const app = v.application!;

    this.isSubmitting.set(true);
    this.serverError.set(null);
    this.uploadProgress.set(0);

    this.gwApi
      .create({
        applicationId: app.id,
        title: v.title!,
        year: v.year!,
        grade: v.grade!,
        commissionMembers: v.commissionMembers!,
      })
      .subscribe({
        next: (gw) => this.uploadFiles(gw),
        error: (err) => {
          this.isSubmitting.set(false);
          this.serverError.set(err?.error?.detail ?? 'Не удалось создать запись ВКР.');
        },
      });
  }

  private uploadFiles(gw: GraduateWorkDto): void {
    const thesis = this.thesisFile();
    const presentation = this.presentationFile();
    const totalSteps = (thesis ? 1 : 0) + (presentation ? 1 : 0);
    let done = 0;

    const finish = () => {
      this.isSubmitting.set(false);
      this.messageService.add({
        severity: 'success',
        summary: 'ВКР создана',
        detail: `Запись «${gw.title}» успешно добавлена.`,
      });
      this.visible.set(false);
      this.gwCreated.emit(gw);
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
        .getUploadUrl(gw.id, 'thesis')
        .pipe(
          switchMap(({ url }) => this.gwApi.uploadToStorage(url, thesis)),
          filter((e) => e.type === HttpEventType.Response),
          switchMap(() => this.gwApi.confirmUpload(gw.id, 'thesis', thesis.name)),
        )
        .subscribe({ next, error: () => next() });
    }

    if (presentation) {
      this.gwApi
        .getUploadUrl(gw.id, 'presentation')
        .pipe(
          switchMap(({ url }) => this.gwApi.uploadToStorage(url, presentation)),
          filter((e) => e.type === HttpEventType.Response),
          switchMap(() => this.gwApi.confirmUpload(gw.id, 'presentation', presentation.name)),
        )
        .subscribe({ next, error: () => next() });
    }
  }
}
