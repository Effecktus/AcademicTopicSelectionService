import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { catchError, of, Subscription, switchMap, timer } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { Role } from '../models/auth.models';
import type { PagedResult } from '../models/common.models';

@Injectable({ providedIn: 'root' })
export class NotificationBadgeService {
  private readonly http = inject(HttpClient);
  private pollingSub: Subscription | null = null;

  readonly unreadCount = signal(0);

  startPolling(role: Role | null): void {
    if (role === null || role === 'Admin') {
      this.stopPolling();
      this.unreadCount.set(0);
      return;
    }

    this.stopPolling();

    this.pollingSub = timer(0, 30_000)
      .pipe(
        switchMap(() =>
          this.http
            .get<PagedResult<unknown>>(
              `${environment.apiUrl}/notifications?isRead=false&page=1&pageSize=1`,
            )
            // Ошибка одного запроса не должна останавливать polling-цепочку.
            .pipe(catchError(() => of(null))),
        ),
      )
      .subscribe((result) => {
        if (result) {
          this.unreadCount.set(result.total);
        }
      });
  }

  decrement(): void {
    this.unreadCount.update((count) => Math.max(0, count - 1));
  }

  reset(): void {
    this.unreadCount.set(0);
    this.stopPolling();
  }

  private stopPolling(): void {
    this.pollingSub?.unsubscribe();
    this.pollingSub = null;
  }
}