import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import type { TeacherDto } from '../../../core/models/teacher.models';
import type { TopicDto } from '../../../core/models/topic.models';
import { TopicsApiService } from '../../topics/topics-api.service';
import { TeachersApiService } from '../teachers-api.service';

@Component({
  selector: 'app-teacher-detail',
  imports: [RouterLink],
  templateUrl: './teacher-detail.component.html',
  styleUrl: './teacher-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeacherDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly teachersApi = inject(TeachersApiService);
  private readonly topicsApi = inject(TopicsApiService);

  readonly teacher = signal<TeacherDto | null>(null);
  readonly topics = signal<TopicDto[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.isLoading.set(false);
      this.errorMessage.set('Некорректный идентификатор преподавателя.');
      return;
    }

    this.loadTeacher(id);
  }

  formatFullName(): string {
    const teacher = this.teacher();
    if (!teacher) return '';
    return [teacher.lastName, teacher.firstName, teacher.middleName].filter(Boolean).join(' ');
  }

  private loadTeacher(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.teachersApi.getTeacherById(id).subscribe({
      next: (teacher) => {
        this.teacher.set(teacher);
        this.loadTeacherTopics(teacher.userId);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('Не удалось загрузить карточку преподавателя.');
      },
    });
  }

  private loadTeacherTopics(teacherUserId: string): void {
    this.topicsApi
      .getTopics({
        createdByUserId: teacherUserId,
        creatorTypeCodeName: 'Teacher',
        page: 1,
        pageSize: 20,
        sort: 'createdAtDesc',
      })
      .subscribe({
        next: (result) => {
          this.topics.set(result.items);
          this.isLoading.set(false);
        },
        error: () => {
          this.topics.set([]);
          this.isLoading.set(false);
        },
      });
  }
}
