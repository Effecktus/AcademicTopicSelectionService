import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, merge } from 'rxjs';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import { AuthService } from '../../../core/auth/auth.service';
import type { TopicDto, TopicsFilter } from '../../../core/models/topic.models';
import { TopicsApiService } from '../topics-api.service';
import {
  currentYearDateRange,
  localDateToEndOfDayUtcIso,
  localDateToStartOfDayUtcIso,
} from '../../../core/utils/date-utils';

type TopicSortColumn = 'title' | 'status' | 'creator' | 'creatorType' | 'createdAt';

interface StatusOption {
  label: string;
  value: string;
}

interface CreatorTypeOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-topics-list',
  imports: [ReactiveFormsModule, InputText, Select, Button, DatePipe],
  templateUrl: './topics-list.component.html',
  styleUrl: './topics-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicsListComponent {
  private readonly topicsApi = inject(TopicsApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly role = this.auth.role;
  readonly canCreateTopic = computed(() => this.role() === 'Teacher');

  readonly queryControl = new FormControl('', { nonNullable: true });
  readonly creatorControl = new FormControl('', { nonNullable: true });
  readonly statusControl = new FormControl('', { nonNullable: true });
  readonly creatorTypeControl = new FormControl('', { nonNullable: true });
  readonly dateFromControl = new FormControl('', { nonNullable: true });
  readonly dateToControl = new FormControl('', { nonNullable: true });

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
  readonly creatorTypeOptions = signal<CreatorTypeOption[]>([
    { label: 'Все типы авторов', value: '' },
    { label: 'Преподаватель', value: 'Teacher' },
    { label: 'Студент', value: 'Student' },
  ]);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  readonly canGoPrev = computed(() => this.page() > 1);
  readonly canGoNext = computed(() => this.page() < this.totalPages());

  readonly sortColumn = signal<TopicSortColumn>('createdAt');
  readonly sortDir = signal<'asc' | 'desc'>('desc');

  constructor() {
    this.queryControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.page.set(1);
        this.loadTopics();
      });

    this.creatorControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.page.set(1);
        this.loadTopics();
      });

    this.statusControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.page.set(1);
      this.loadTopics();
    });

    this.creatorTypeControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.page.set(1);
      this.loadTopics();
    });

    merge(this.dateFromControl.valueChanges, this.dateToControl.valueChanges)
      .pipe(debounceTime(150), takeUntilDestroyed())
      .subscribe(() => {
        this.page.set(1);
        this.loadTopics();
      });

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

  openTopic(topicId: string): void {
    void this.router.navigate(['/topics', topicId]);
  }

  createTopic(): void {
    void this.router.navigate(['/topics/new']);
  }

  resetFilters(): void {
    this.queryControl.setValue('');
    this.creatorControl.setValue('');
    this.statusControl.setValue('');
    this.creatorTypeControl.setValue('');
    const range = currentYearDateRange();
    this.dateFromControl.setValue(range.from);
    this.dateToControl.setValue(range.to);
    this.page.set(1);
    this.loadTopics();
  }

  toggleTopicSort(column: TopicSortColumn): void {
    if (this.sortColumn() === column) {
      this.sortDir.update((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortColumn.set(column);
      this.sortDir.set('asc');
    }
    this.page.set(1);
    this.loadTopics();
  }

  topicSortIndicator(column: TopicSortColumn): string {
    if (this.sortColumn() !== column) return '';
    return this.sortDir() === 'asc' ? ' \u25b2' : ' \u25bc';
  }

  private loadTopics(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.topicsApi
      .getTopics({
        query: this.queryControl.value,
        creatorQuery: this.creatorControl.value || undefined,
        statusCodeName: this.statusControl.value || undefined,
        creatorTypeCodeName: this.creatorTypeControl.value || undefined,
        createdFromUtc: localDateToStartOfDayUtcIso(this.dateFromControl.value),
        createdToUtc: localDateToEndOfDayUtcIso(this.dateToControl.value),
        sort: this.topicSortApiValue(),
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

  private topicSortApiValue(): NonNullable<TopicsFilter['sort']> {
    const col = this.sortColumn();
    const asc = this.sortDir() === 'asc';
    const pairs: Record<TopicSortColumn, readonly [NonNullable<TopicsFilter['sort']>, NonNullable<TopicsFilter['sort']>]> =
      {
        title: ['titleAsc', 'titleDesc'],
        status: ['statusAsc', 'statusDesc'],
        creator: ['creatorAsc', 'creatorDesc'],
        creatorType: ['creatorTypeAsc', 'creatorTypeDesc'],
        createdAt: ['createdAtAsc', 'createdAtDesc'],
      };
    const pair = pairs[col];
    return asc ? pair[0] : pair[1];
  }

}
