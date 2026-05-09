import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, merge } from 'rxjs';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import type { GraduateWorkDto } from '../../../core/models/graduate-work.models';
import { GraduateWorksApiService } from '../graduate-works-api.service';

interface YearOption {
  label: string;
  value: number | null;
}

@Component({
  selector: 'app-graduate-works-list',
  imports: [ReactiveFormsModule, Button, Select, InputText],
  templateUrl: './graduate-works-list.component.html',
  styleUrl: './graduate-works-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GraduateWorksListComponent {
  private readonly graduateWorksApi = inject(GraduateWorksApiService);
  private readonly router = inject(Router);

  readonly items = signal<GraduateWorkDto[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly page = signal(1);
  readonly pageSize = 10;
  readonly total = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  readonly canGoPrev = computed(() => this.page() > 1);
  readonly canGoNext = computed(() => this.page() < this.totalPages());

  readonly titleControl = new FormControl('', { nonNullable: true });
  readonly teacherControl = new FormControl('', { nonNullable: true });
  readonly yearControl = new FormControl<number | null>(null);

  readonly yearOptions: YearOption[] = (() => {
    const current = new Date().getFullYear();
    const opts: YearOption[] = [{ label: 'Все годы', value: null }];
    for (let y = current + 1; y >= 2000; y--) {
      opts.push({ label: String(y), value: y });
    }
    return opts;
  })();

  constructor() {
    merge(this.titleControl.valueChanges, this.teacherControl.valueChanges)
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.page.set(1);
        this.load();
      });

    this.yearControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
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

  resetFilters(): void {
    this.titleControl.setValue('');
    this.teacherControl.setValue('');
    this.yearControl.setValue(null);
    this.page.set(1);
    this.load();
  }

  openDetail(id: string): void {
    void this.router.navigate(['/graduate-works', id]);
  }

  yesNo(v: boolean): string {
    return v ? 'Да' : 'Нет';
  }

  private load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.graduateWorksApi
      .getAll({
        page: this.page(),
        pageSize: this.pageSize,
        year: this.yearControl.value ?? undefined,
        titleQuery: this.titleControl.value.trim() || null,
        teacherQuery: this.teacherControl.value.trim() || null,
        teacherId: null,
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
