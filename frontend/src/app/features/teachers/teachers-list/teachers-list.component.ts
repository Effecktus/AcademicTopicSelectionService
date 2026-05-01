import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';

import type { TeacherDto } from '../../../core/models/teacher.models';
import { TeachersApiService, type TeachersFilter } from '../teachers-api.service';

type TeacherSortColumn = 'name' | 'email' | 'degree' | 'title' | 'position' | 'maxStudents';

@Component({
  selector: 'app-teachers-list',
  imports: [ReactiveFormsModule, InputText, Button],
  templateUrl: './teachers-list.component.html',
  styleUrl: './teachers-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeachersListComponent {
  private readonly teachersApi = inject(TeachersApiService);
  private readonly router = inject(Router);

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

  readonly sortColumn = signal<TeacherSortColumn>('name');
  readonly sortDir = signal<'asc' | 'desc'>('asc');

  constructor() {
    this.queryControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.page.set(1);
        this.loadTeachers();
      });

    this.loadTeachers();
  }

  toggleTeacherSort(column: TeacherSortColumn): void {
    if (this.sortColumn() === column) {
      this.sortDir.update((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortColumn.set(column);
      this.sortDir.set('asc');
    }
    this.page.set(1);
    this.loadTeachers();
  }

  teacherSortIndicator(column: TeacherSortColumn): string {
    if (this.sortColumn() !== column) return '';
    return this.sortDir() === 'asc' ? ' \u25b2' : ' \u25bc';
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

  openTeacher(teacherId: string): void {
    void this.router.navigate(['/teachers', teacherId]);
  }

  resetFilters(): void {
    this.queryControl.setValue('');
    this.page.set(1);
    this.loadTeachers();
  }

  private loadTeachers(): void {
    if (this.teachers().length === 0) {
      this.isLoading.set(true);
    }
    this.errorMessage.set(null);

    this.teachersApi
      .getTeachers({
        query: this.queryControl.value,
        sort: this.teacherSortApiValue(),
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

  private teacherSortApiValue(): NonNullable<TeachersFilter['sort']> {
    const col = this.sortColumn();
    const asc = this.sortDir() === 'asc';
    const pairs: Record<TeacherSortColumn, readonly [string, string]> = {
      name: ['nameAsc', 'nameDesc'],
      email: ['emailAsc', 'emailDesc'],
      degree: ['academicDegreeAsc', 'academicDegreeDesc'],
      title: ['academicTitleAsc', 'academicTitleDesc'],
      position: ['positionAsc', 'positionDesc'],
      maxStudents: ['maxStudentsAsc', 'maxStudentsDesc'],
    };
    const pair = pairs[col];
    return asc ? pair[0] : pair[1];
  }
}
