import { DatePipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';
import { SelectButton } from 'primeng/selectbutton';

import { DEFAULT_NOTIFICATION_ICON, NOTIFICATION_ICONS } from '../../../core/constants/notification-icons';
import type { NotificationDto } from '../../../core/models/notification.models';
import { NotificationBadgeService } from '../../../core/notifications/notification-badge.service';
import { NotificationsApiService } from '../notifications-api.service';

type NotificationsFilterMode = 'all' | 'unread';

interface FilterOption {
  label: string;
  value: NotificationsFilterMode;
}

const NOTIFICATION_CONTENT_PREVIEW_MAX = 220;

@Component({
  selector: 'app-notifications-list',
  imports: [DatePipe, FormsModule, Button, SelectButton, NgClass],
  templateUrl: './notifications-list.component.html',
  styleUrl: './notifications-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsListComponent {
  private readonly notificationsApi = inject(NotificationsApiService);
  private readonly notificationBadge = inject(NotificationBadgeService);
  private readonly router = inject(Router);

  readonly notifications = signal<NotificationDto[]>([]);
  readonly visibleNotifications = computed(() => {
    if (this.filterMode() === 'unread') {
      return this.notifications().filter((item) => !item.isRead);
    }
    return this.notifications();
  });
  readonly isLoading = signal(false);
  readonly isMarkAllLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly markingIds = signal<Set<string>>(new Set());

  readonly page = signal(1);
  readonly pageSize = 10;
  readonly total = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  readonly canGoPrev = computed(() => this.page() > 1);
  readonly canGoNext = computed(() => this.page() < this.totalPages());

  readonly filterOptions: FilterOption[] = [
    { label: 'Все', value: 'all' },
    { label: 'Только непрочитанные', value: 'unread' },
  ];
  readonly filterMode = signal<NotificationsFilterMode>('all');

  constructor() {
    this.loadNotifications();
  }

  prevPage(): void {
    if (!this.canGoPrev()) return;
    this.page.update((value) => value - 1);
    this.loadNotifications();
  }

  nextPage(): void {
    if (!this.canGoNext()) return;
    this.page.update((value) => value + 1);
    this.loadNotifications();
  }

  changeFilter(mode: NotificationsFilterMode): void {
    this.filterMode.set(mode);
    this.page.set(1);
    this.loadNotifications();
  }

  openNotification(item: NotificationDto): void {
    const navigate = () => this.navigateToRelated(item);
    if (item.isRead) {
      navigate();
      return;
    }
    this.markOneAsRead(item, navigate);
  }

  markOneAsRead(item: NotificationDto, onDone?: () => void): void {
    if (item.isRead || this.markingIds().has(item.id)) {
      onDone?.();
      return;
    }

    this.markingIds.update((current) => {
      const next = new Set(current);
      next.add(item.id);
      return next;
    });

    this.notificationsApi.markAsRead(item.id).subscribe({
      next: () => {
        this.notifications.update((list) =>
          list.map((n) => (n.id === item.id ? { ...n, isRead: true } : n)),
        );
        this.notificationBadge.decrement();
        this.markingIds.update((current) => {
          const next = new Set(current);
          next.delete(item.id);
          return next;
        });
        onDone?.();
      },
      error: () => {
        this.errorMessage.set('Не удалось отметить уведомление как прочитанное.');
        this.markingIds.update((current) => {
          const next = new Set(current);
          next.delete(item.id);
          return next;
        });
      },
    });
  }

  markAllAsRead(): void {
    this.isMarkAllLoading.set(true);
    this.errorMessage.set(null);

    this.notificationsApi.markAllAsRead().subscribe({
      next: () => {
        this.notifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
        this.notificationBadge.clearCount();
        this.isMarkAllLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Не удалось отметить все уведомления как прочитанные.');
        this.isMarkAllLoading.set(false);
      },
    });
  }

  iconFor(typeCodeName: string): string {
    return NOTIFICATION_ICONS[typeCodeName] ?? DEFAULT_NOTIFICATION_ICON;
  }

  previewContent(content: string): string {
    const trimmed = content.trim();
    if (trimmed.length <= NOTIFICATION_CONTENT_PREVIEW_MAX) {
      return trimmed;
    }
    return `${trimmed.slice(0, NOTIFICATION_CONTENT_PREVIEW_MAX)}…`;
  }

  private loadNotifications(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.notificationsApi
      .getNotifications({
        page: this.page(),
        pageSize: this.pageSize,
        isRead: this.filterMode() === 'unread' ? false : undefined,
      })
      .subscribe({
        next: (result) => {
          this.notifications.set(result.items);
          this.total.set(result.total);
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set('Не удалось загрузить уведомления.');
          this.isLoading.set(false);
        },
      });
  }

  private navigateToRelated(item: NotificationDto): void {
    const route = this.routeByRelatedEntity(item.relatedEntityType, item.relatedEntityId);
    if (!route) {
      this.errorMessage.set('Для этого уведомления пока недоступен переход к связанной сущности.');
      return;
    }
    void this.router.navigate(route);
  }

  private routeByRelatedEntity(
    relatedEntityType: string | null,
    relatedEntityId: string | null,
  ): string[] | null {
    if (!relatedEntityType || !relatedEntityId) {
      return null;
    }
    if (relatedEntityType === 'Application') {
      return ['/applications', relatedEntityId];
    }
    if (relatedEntityType === 'SupervisorRequest') {
      return ['/supervisor-requests', relatedEntityId];
    }
    if (relatedEntityType === 'GraduateWork') {
      return ['/graduate-works', relatedEntityId];
    }
    return null;
  }
}
