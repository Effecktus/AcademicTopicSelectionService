import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { HttpEventType } from '@angular/common/http';
import { filter, switchMap } from 'rxjs';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { ProgressBar } from 'primeng/progressbar';
import { Select } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';

import type { GraduateWorkFileType } from '../../../core/models/graduate-work.models';
import { GraduateWorksApiService } from '../../graduate-works/graduate-works-api.service';

@Component({
  selector: 'app-upload-gw-file',
  imports: [Button, Dialog, ProgressBar, Select, FormsModule],
  templateUrl: './upload-gw-file.component.html',
  styleUrl: './upload-gw-file.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UploadGwFileComponent {
  private readonly gwApi = inject(GraduateWorksApiService);
  private readonly messageService = inject(MessageService);

  readonly visible = model(false);
  readonly gwId = input.required<string>();
  readonly uploaded = output<void>();

  readonly file = signal<File | null>(null);
  readonly fileType = signal<GraduateWorkFileType>('thesis');
  readonly isUploading = signal(false);
  readonly progress = signal(0);
  readonly serverError = signal<string | null>(null);

  readonly fileTypeOptions = [
    { label: 'Текст ВКР', value: 'thesis' as GraduateWorkFileType },
    { label: 'Презентация', value: 'presentation' as GraduateWorkFileType },
  ];

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file.set(input.files?.[0] ?? null);
  }

  onHide(): void {
    this.file.set(null);
    this.progress.set(0);
    this.serverError.set(null);
    this.fileType.set('thesis');
  }

  upload(): void {
    const f = this.file();
    if (!f) return;

    this.isUploading.set(true);
    this.serverError.set(null);
    this.progress.set(0);

    this.gwApi
      .getUploadUrl(this.gwId(), this.fileType())
      .pipe(
        switchMap(({ url }) => this.gwApi.uploadToStorage(url, f)),
        filter((e) => e.type === HttpEventType.Response),
        switchMap(() => this.gwApi.confirmUpload(this.gwId(), this.fileType(), f.name)),
      )
      .subscribe({
        next: () => {
          this.isUploading.set(false);
          this.progress.set(100);
          this.messageService.add({
            severity: 'success',
            summary: 'Файл загружен',
            detail: f.name,
          });
          this.visible.set(false);
          this.uploaded.emit();
        },
        error: (err) => {
          this.isUploading.set(false);
          this.serverError.set(err?.error?.detail ?? 'Ошибка при загрузке файла.');
        },
      });
  }
}
