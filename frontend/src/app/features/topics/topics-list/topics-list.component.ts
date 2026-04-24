import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import { AuthService } from '../../../core/auth/auth.service';
import type { TeacherDto } from '../../../core/models/teacher.models';
import type { TopicDto } from '../../../core/models/topic.models';
import { TeachersApiService } from '../../teachers/teachers-api.service';
import { TopicsApiService } from '../topics-api.service';

interface StatusOption {
  label: string;
  value: string;
}

interface TeacherOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-topics-list',
  imports: [ReactiveFormsModule, RouterLink, InputText, Select, Button, DatePipe],
  templateUrl: './topics-list.component.html',
  styleUrl: './topics-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicsListComponent {
  private readonly topicsApi = inject(TopicsApiService);
  private readonly teachersApi = inject(TeachersApiService);
  private readonly auth = inject(AuthService);

  readonly currentUser = this.auth.currentUser;
  readonly role = this.auth.role;
  readonly canCreateTopic = computed(() => this.role() === 'Teacher');

  readonly queryControl = new FormControl('', { nonNullable: true });
  readonly statusControl = new FormControl('', { nonNullable: true });
  readonly teacherControl = new FormControl('', { nonNullable: true });

  readonly topics = signal<TopicDto[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly page = signal(1);
  readonly pageSize = 10;
  readonly total = signal(0);

  readonly statusOptions = signal<StatusOption[]>([
    { label: 'Все статусы', value: '' },
    { label: 'Активна', value: 'Active' },
    { label: 'Неактивна', value: 'Inactive' },
  ]);
  readonly teacherOptions = signal<TeacherOption[]>([{ label: 'Все преподаватели', value: '' }]);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  readonly canGoPrev = computed(() => this.page() > 1);
  readonly canGoNext = computed(() => this.page() < this.totalPages());

  constructor() {
    this.queryControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.page.set(1);
        this.loadTopics();
      });

    this.statusControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.page.set(1);
      this.loadTopics();
    });

    this.teacherControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.page.set(1);
      this.loadTopics();
    });

    this.loadTeachersForFilter();
    this.loadTopics();
  }

  prevPage(): void {
    if (!this.canGoPrev()) return;
    this.page.update((value) => value - 1);
    this.loadTopics();
  }

  nextPage(): void {
    if (!this.canGoNext()) return;
    this.page.update((value) => value + 1);
    this.loadTopics();
  }

  canManageTopic(topic: TopicDto): boolean {
    return this.role() === 'Teacher' && this.currentUser()?.userId === topic.createdByUserId;
  }

  private loadTopics(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.topicsApi
      .getTopics({
        query: this.queryControl.value,
        statusCodeName: this.statusControl.value || undefined,
        createdByUserId: this.teacherControl.value || undefined,
        creatorTypeCodeName: this.teacherControl.value ? 'Teacher' : undefined,
        sort: 'createdAtDesc',
        page: this.page(),
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (result) => {
          this.topics.set(result.items);
          this.total.set(result.total);
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set('Не удалось загрузить темы.');
          this.isLoading.set(false);
        },
      });
  }

  private loadTeachersForFilter(): void {
    this.teachersApi
      .getTeachers({
        page: 1,
        pageSize: 200,
      })
      .subscribe({
        next: (result) => {
          const options = result.items.map((teacher) => ({
            label: this.formatTeacherLabel(teacher),
            value: teacher.userId,
          }));
          this.teacherOptions.set([{ label: 'Все преподаватели', value: '' }, ...options]);
        },
      });
  }

  private formatTeacherLabel(teacher: TeacherDto): string {
    const fullName = [teacher.lastName, teacher.firstName, teacher.middleName]
      .filter(Boolean)
      .join(' ');
    return `${fullName} (${teacher.email})`;
  }
}
