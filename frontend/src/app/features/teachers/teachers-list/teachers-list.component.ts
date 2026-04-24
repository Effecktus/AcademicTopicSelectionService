import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';

import type { TeacherDto } from '../../../core/models/teacher.models';
import { TeachersApiService } from '../teachers-api.service';

@Component({
  selector: 'app-teachers-list',
  imports: [ReactiveFormsModule, RouterLink, InputText, Button],
  templateUrl: './teachers-list.component.html',
  styleUrl: './teachers-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeachersListComponent {
  private readonly teachersApi = inject(TeachersApiService);

  readonly queryControl = new FormControl('', { nonNullable: true });
  readonly teachers = signal<TeacherDto[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly page = signal(1);
  readonly pageSize = 10;
  readonly total = signal(0);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  readonly canGoPrev = computed(() => this.page() > 1);
  readonly canGoNext = computed(() => this.page() < this.totalPages());

  constructor() {
    this.queryControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.page.set(1);
        this.loadTeachers();
      });

    this.loadTeachers();
  }

  prevPage(): void {
    if (!this.canGoPrev()) return;
    this.page.update((value) => value - 1);
    this.loadTeachers();
  }

  nextPage(): void {
    if (!this.canGoNext()) return;
    this.page.update((value) => value + 1);
    this.loadTeachers();
  }

  formatFullName(teacher: TeacherDto): string {
    return [teacher.lastName, teacher.firstName, teacher.middleName].filter(Boolean).join(' ');
  }

  private loadTeachers(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.teachersApi
      .getTeachers({
        query: this.queryControl.value,
        page: this.page(),
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (result) => {
          this.teachers.set(result.items);
          this.total.set(result.total);
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set('Не удалось загрузить преподавателей.');
          this.isLoading.set(false);
        },
      });
  }
}
