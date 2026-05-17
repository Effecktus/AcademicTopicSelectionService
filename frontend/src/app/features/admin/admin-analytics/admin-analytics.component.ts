import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Button } from 'primeng/button';

import type { AdminAnalyticsDto } from '../../../core/models/admin.models';
import { AdminApiService } from '../admin-api.service';

@Component({
  selector: 'app-admin-analytics',
  imports: [Button],
  templateUrl: './admin-analytics.component.html',
  styleUrl: './admin-analytics.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAnalyticsComponent {
  private readonly adminApi = inject(AdminApiService);

  readonly data = signal<AdminAnalyticsDto | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  constructor() {
    this.load();
  }

  reload(): void {
    this.load();
  }

  private load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.adminApi.getAnalytics().subscribe({
      next: (result) => {
        this.data.set(result);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Не удалось загрузить аналитику.');
        this.isLoading.set(false);
      },
    });
  }
}
