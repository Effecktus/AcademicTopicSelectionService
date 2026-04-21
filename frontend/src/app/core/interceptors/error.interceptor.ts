import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { catchError, switchMap, throwError } from 'rxjs';

import { AuthService } from '../auth/auth.service';

const AUTH_RETRY = 'X-Auth-Retry';

function isAuthPublicUrl(url: string): boolean {
  return (
    url.includes('/auth/login') || url.includes('/auth/refresh') || url.includes('/auth/logout')
  );
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const messageService = inject(MessageService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (isAuthPublicUrl(req.url)) {
        return throwError(() => err);
      }

      if (err.status === 401) {
        if (req.headers.has(AUTH_RETRY)) {
          auth.clearSession();
          void router.navigateByUrl('/login');
          return throwError(() => err);
        }

        return auth.refresh().pipe(
          switchMap(() =>
            next(
              req.clone({
                headers: req.headers.set(AUTH_RETRY, '1'),
              }),
            ),
          ),
          catchError(() => {
            auth.clearSession();
            void router.navigateByUrl('/login');
            return throwError(() => err);
          }),
        );
      }

      if (err.status === 403) {
        messageService.add({
          severity: 'error',
          summary: 'Доступ запрещён',
          detail: 'Недостаточно прав для выполнения операции.',
        });
      } else if (err.status === 429) {
        messageService.add({
          severity: 'warn',
          summary: 'Подождите',
          detail: 'Слишком много запросов. Попробуйте позже.',
        });
      } else if (err.status >= 500) {
        messageService.add({
          severity: 'error',
          summary: 'Ошибка сервера',
          detail: 'Сервис временно недоступен. Попробуйте позже.',
        });
      }

      return throwError(() => err);
    }),
  );
};
