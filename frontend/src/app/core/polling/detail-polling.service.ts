import { DestroyRef, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, catchError, of, switchMap, timer } from 'rxjs';

export interface DetailPollingOptions {
  /** Интервал между опросами в мс; первая эмиссия таймера — через тот же интервал (как `timer(period, period)`). */
  periodMs?: number;
}

/**
 * Общий интервальный опрос для карточек сущностей (заявка, запрос научрука и т.д.).
 */
@Injectable({ providedIn: 'root' })
export class DetailPollingService {
  /**
   * Пока живёт {@link DestroyRef}, периодически вызывает {@link load}.
   * Ошибки HTTP глушатся и дают значение null (как прежний catchError(() => of(null))).
   */
  pollWhileAlive<T>(
    destroyRef: DestroyRef,
    load: () => Observable<T>,
    options?: DetailPollingOptions,
  ): Observable<T | null> {
    const periodMs = options?.periodMs ?? 10_000;
    return timer(periodMs, periodMs).pipe(
      takeUntilDestroyed(destroyRef),
      switchMap(() => load().pipe(catchError(() => of(null as T | null)))),
    );
  }
}
