import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Button } from 'primeng/button';

import { AdminApiService } from '../admin-api.service';

@Component({
  selector: 'app-admin-export',
  imports: [Button],
  templateUrl: './admin-export.component.html',
  styleUrl: './admin-export.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminExportComponent {
  private readonly adminApi = inject(AdminApiService);

  readonly loadingExcel = signal(false);
  readonly loadingCsvGw = signal(false);
  readonly loadingCsvApps = signal(false);
  readonly loadingCsvUsers = signal(false);

  downloadExcel(): void {
    this.loadingExcel.set(true);
    this.adminApi.exportExcel().subscribe({
      next: (blob) => {
        this.triggerDownload(blob, `export_${this.dateSuffix()}.xlsx`);
        this.loadingExcel.set(false);
      },
      error: () => this.loadingExcel.set(false),
    });
  }

  downloadCsv(dataset: 'graduate-works' | 'applications' | 'users'): void {
    const loaderMap = {
      'graduate-works': this.loadingCsvGw,
      applications: this.loadingCsvApps,
      users: this.loadingCsvUsers,
    };
    const loader = loaderMap[dataset];
    loader.set(true);

    this.adminApi.exportCsv(dataset).subscribe({
      next: (blob) => {
        this.triggerDownload(blob, `${dataset}_${this.dateSuffix()}.csv`);
        loader.set(false);
      },
      error: () => loader.set(false),
    });
  }

  private triggerDownload(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
  }

  private dateSuffix(): string {
    return new Date().toISOString().slice(0, 10).replace(/-/g, '');
  }
}
