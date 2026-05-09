import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';

import type { ProblemDetails } from '../../../core/models/common.models';
import type { GraduateWorkDto, GraduateWorkFileType } from '../../../core/models/graduate-work.models';
import { GraduateWorksApiService } from '../graduate-works-api.service';

@Component({
  selector: 'app-graduate-work-detail',
  imports: [RouterLink, Button, DatePipe],
  templateUrl: './graduate-work-detail.component.html',
  styleUrl: './graduate-work-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GraduateWorkDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly graduateWorksApi = inject(GraduateWorksApiService);

  readonly work = signal<GraduateWorkDto | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly downloadError = signal<string | null>(null);
  readonly downloadingThesis = signal(false);
  readonly downloadingPresentation = signal(false);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.isLoading.set(false);
      this.errorMessage.set('Некорректный идентификатор записи.');
      return;
    }
    this.load(id);
  }

  download(fileType: GraduateWorkFileType): void {
    const item = this.work();
    if (!item) return;

    const busy = fileType === 'thesis' ? this.downloadingThesis : this.downloadingPresentation;
    this.downloadError.set(null);
    busy.set(true);

    this.graduateWorksApi.getDownloadUrl(item.id, fileType).subscribe({
      next: (dto) => {
        busy.set(false);
        window.open(dto.url, '_blank', 'noopener,noreferrer');
      },
      error: (err: unknown) => {
        busy.set(false);
        this.downloadError.set(this.mapDownloadError(err));
      },
    });
  }

  private load(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.graduateWorksApi.getById(id).subscribe({
      next: (dto) => {
        this.work.set(dto);
        this.isLoading.set(false);
      },
      error: (err: unknown) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.mapLoadError(err));
      },
    });
  }

  private mapLoadError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 404) {
        return 'Запись не найдена.';
      }
      const pd = err.error as ProblemDetails | undefined;
      if (pd?.detail) return pd.detail;
    }
    return 'Не удалось загрузить запись.';
  }

  private mapDownloadError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      const pd = err.error as ProblemDetails | undefined;
      if (pd?.detail) return pd.detail;
      if (err.status === 400) {
        return 'Файл недоступен для скачивания.';
      }
    }
    return 'Не удалось получить ссылку на скачивание.';
  }
}
