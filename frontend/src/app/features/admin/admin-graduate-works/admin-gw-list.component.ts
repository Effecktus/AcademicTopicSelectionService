import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { ConfirmationService } from 'primeng/api';

import type { GraduateWorkDto, GraduateWorkStatusCode } from '../../../core/models/graduate-work.models';
import { GraduateWorksApiService } from '../../graduate-works/graduate-works-api.service';
import { CreateGwDialogComponent } from './create-gw-dialog.component';
import { GwDetailDialogComponent } from './gw-detail-dialog.component';

interface YearOption {
  label: string;
  value: number | null;
}

interface StatusOption {
  label: string;
  value: GraduateWorkStatusCode | null;
}

@Component({
  selector: 'app-admin-gw-list',
  imports: [
    ReactiveFormsModule,
    Button,
    InputText,
    Select,
    CreateGwDialogComponent,
    GwDetailDialogComponent,
  ],
  templateUrl: './admin-gw-list.component.html',
  styleUrl: './admin-gw-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminGwListComponent {
  private readonly gwApi = inject(GraduateWorksApiService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly items = signal<GraduateWorkDto[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly page = signal(1);
  readonly pageSize = 20;
  readonly total = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  readonly canGoPrev = computed(() => this.page() > 1);
  readonly canGoNext = computed(() => this.page() < this.totalPages());

  readonly titleControl = new FormControl('', { nonNullable: true });
  readonly yearControl = new FormControl<number | null>(null);
  readonly statusControl = new FormControl<GraduateWorkStatusCode | null>(null);

  readonly yearOptions: YearOption[] = (() => {
    const current = new Date().getFullYear();
    const opts: YearOption[] = [{ label: 'Все годы', value: null }];
    for (let y = current + 1; y >= 2000; y--) {
      opts.push({ label: String(y), value: y });
    }
    return opts;
  })();

  readonly statusOptions: StatusOption[] = [
    { label: 'Все статусы', value: null },
    { label: 'Черновик', value: 'Draft' },
    { label: 'Заполнено', value: 'Completed' },
  ];

  readonly showCreateDialog = signal(false);
  readonly selectedGw = signal<GraduateWorkDto | null>(null);
  readonly showDetailDialog = signal(false);

  openDetail(gw: GraduateWorkDto): void {
    this.selectedGw.set(gw);
    this.showDetailDialog.set(true);
  }

  constructor() {
    this.titleControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.page.set(1);
        this.load();
      });

    this.yearControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.page.set(1);
      this.load();
    });

    this.statusControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.page.set(1);
      this.load();
    });

    this.load();
  }

  prevPage(): void {
    if (!this.canGoPrev()) return;
    this.page.update((v) => v - 1);
    this.load();
  }

  nextPage(): void {
    if (!this.canGoNext()) return;
    this.page.update((v) => v + 1);
    this.load();
  }

  openCreateDialog(): void {
    this.showCreateDialog.set(true);
  }

  onGwCreated(): void {
    this.showCreateDialog.set(false);
    this.load();
  }

  onGwUpdated(): void {
    this.load();
  }

  deleteGw(gw: GraduateWorkDto): void {
    this.confirmationService.confirm({
      header: 'Удалить ВКР',
      message: `Запись «${gw.title}» и все связанные файлы будут удалены без возможности восстановления. Продолжить?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Удалить',
      rejectLabel: 'Отмена',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.gwApi.delete(gw.id).subscribe({
          next: () => this.load(),
          error: () => {
            this.errorMessage.set('Не удалось удалить запись.');
          },
        });
      },
    });
  }

  statusBadgeClass(statusCode: GraduateWorkStatusCode): string {
    return statusCode === 'Completed' ? 'badge badge--completed' : 'badge badge--draft';
  }

  private load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.gwApi
      .getAll({
        page: this.page(),
        pageSize: this.pageSize,
        year: this.yearControl.value ?? undefined,
        titleQuery: this.titleControl.value.trim() || null,
        teacherQuery: null,
        teacherId: null,
        status: this.statusControl.value ?? undefined,
      })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.total.set(Number(result.total));
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set('Не удалось загрузить архив ВКР.');
          this.isLoading.set(false);
        },
      });
  }
}
