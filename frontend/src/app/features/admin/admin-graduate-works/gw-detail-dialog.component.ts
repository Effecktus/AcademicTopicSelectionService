import { ChangeDetectionStrategy, Component, inject, input, model, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { SharedModule } from 'primeng/api';

import type { GraduateWorkDto, GraduateWorkFileType } from '../../../core/models/graduate-work.models';
import { GraduateWorksApiService } from '../../graduate-works/graduate-works-api.service';

@Component({
  selector: 'app-gw-detail-dialog',
  imports: [Button, Dialog, DatePipe, SharedModule],
  templateUrl: './gw-detail-dialog.component.html',
  styleUrl: './gw-detail-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GwDetailDialogComponent {
  private readonly gwApi = inject(GraduateWorksApiService);

  readonly visible = model(false);
  readonly gw = input.required<GraduateWorkDto>();

  readonly downloadingThesis = signal(false);
  readonly downloadingPresentation = signal(false);

  download(fileType: GraduateWorkFileType): void {
    const isThesis = fileType === 'thesis';
    if (isThesis) this.downloadingThesis.set(true);
    else this.downloadingPresentation.set(true);

    this.gwApi.getDownloadUrl(this.gw().id, fileType).subscribe({
      next: ({ url }) => {
        window.open(url, '_blank');
        if (isThesis) this.downloadingThesis.set(false);
        else this.downloadingPresentation.set(false);
      },
      error: () => {
        if (isThesis) this.downloadingThesis.set(false);
        else this.downloadingPresentation.set(false);
      },
    });
  }
}
