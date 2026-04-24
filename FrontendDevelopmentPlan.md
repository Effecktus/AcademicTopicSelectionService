# План разработки Frontend (Angular 20) для проекта «AcademicTopicSelectionService»

Документ — **frontend-ориентированный план**, составленный на базе `DevelopmentPlan.md`, `BackendDevelopmentPlan.md` и актуального состояния backend API.

Обновлено: **2026-04-20**.

---

## 0) Цель и границы Frontend

### Цель
Реализовать Angular 20 SPA для сервиса выбора научного руководителя и темы ВКР с ролевой навигацией, полным покрытием всех бизнес-потоков backend-а и удобным UX для каждой роли.

### Роли и их домашние страницы

| Роль | Домашняя страница | Основные функции |
|------|------------------|-----------------|
| `Student` | `/topics` | Просмотр преподавателей и тем, отправка запроса на научрука, подача заявки, чат, архив ВКР |
| `Teacher` | `/supervisor-requests` | Управление темами, входящие запросы научрука, заявки, чат со студентами |
| `DepartmentHead` | `/applications` | Список заявок кафедры на финальное утверждение |
| `Admin` | `/admin/users` | Управление пользователями, загрузка ВКР, аналитика и экспорт |

### Вне scope на старте
- WebSocket / SSE (чат — REST + polling)
- Аналитика и экспорт (последняя итерация, зависят от незаконченного backend)
- i18n / мультиязычность
- PWA / offline mode

---

## 1) Принятые архитектурные решения

| Решение | Выбор | Обоснование |
|---------|-------|-------------|
| UI-библиотека | **PrimeNG 20** | Богатый набор компонентов из коробки: `p-table` с пагинацией/фильтрацией, `p-fileUpload`, `p-chart`, `p-toast`, `p-dialog`, `p-confirmDialog`; не нужно собирать из кусочков, как в Angular Material |
| State management | **Angular Signals** | Встроен в Angular 20 (`signal`, `computed`, `effect`, `toSignal`), не требует внешних зависимостей, проще NgRx при данном масштабе |
| Архитектура компонентов | **Standalone Components** | Рекомендация Angular 17+, нет NgModule-бойлерплейта, удобный lazy loading через `loadComponent` |
| Реактивность HTTP | **RxJS** (`HttpClient`) | Стандарт для Angular, удобен с `switchMap`, `takeUntilDestroyed`, `retry` |
| Формы | **Reactive Forms** (`FormBuilder`) | Удобны для валидации, программного управления и тестирования |
| Polling чата | **RxJS `timer(0, 5000).pipe(switchMap, takeUntilDestroyed)`** | Простой механизм, авто-отмена при уничтожении компонента |
| Декораторы компонентов | **Signal-based inputs/outputs** (`input()`, `output()`, `viewChild()`) | Стандарт Angular 17+; старые `@Input()`/`@Output()` работают, но этот подход современнее |
| Control flow в шаблонах | **Built-in `@if` / `@for` / `@switch`** | Встроен в Angular 17+; `*ngIf`/`*ngFor` устарели |
| Unit-тесты | **Jasmine** + **Karma** (`@angular/build:karma`) | Стандартный фреймворк Angular; интеграция из коробки, поддержка `TestBed` без дополнительных пресетов |
| E2E-тесты | **Playwright** | В 2026 году активно вытесняет Cypress; мультибраузерность из коробки, быстрее, поддерживается Microsoft |
| Стили | **SCSS** + PrimeNG theming (Aura preset) | `providePrimeNG({ theme: { preset: Aura } })`, цветовая схема: синий + белый; кастомизация через CSS-переменные в `styles.scss` |
| Хранение refresh-токена | **httpOnly cookie** (рекомендуется) | Недоступен из JavaScript → защита от XSS; backend устанавливает через `Set-Cookie` |

### Цветовая система (CSS-переменные в `styles.scss`)

Концепция: **синий + белый**, современный минимализм. Все компонентные стили используют только переменные — не хардкодят hex-значения.

| Переменная | Значение | Назначение |
|---|---|---|
| `--blue-primary` | `#1a56db` | Кнопки, ссылки, активные элементы |
| `--blue-hover` | `#1446c0` | Hover-состояние кнопок |
| `--blue-light` | `#eef3fd` | Фоны, бейджи, подсветки |
| `--navy` | `#0d2d6b` | Фон sidebar |
| `--navy-hover` | `#163d88` | Hover пунктов навигации |
| `--navy-active` | `#1a4a9e` | Активный пункт навигации |
| `--white` | `#ffffff` | Фон topbar, карточек |
| `--bg-page` | `#f4f7fe` | Фон контентной области |
| `--border` | `#dce5f5` | Разделители |
| `--text-primary` | `#0d1f3c` | Основной текст |
| `--text-muted` | `#5270a0` | Вспомогательный текст |
| `--error` | `#dc2626` | Цвет ошибок |
| `--error-bg` | `#fef2f2` | Фон блока ошибки |
| `--error-border` | `#fca5a5` | Граница блока ошибки |

> При добавлении новых компонентов — использовать только переменные из этого списка. Для расширения палитры — добавлять переменные в `:root` в `styles.scss`.

---

### Справочник компонентов PrimeNG (используемых в проекте)

| Задача | Компонент PrimeNG | Импорт |
|--------|------------------|--------|
| Таблица с пагинацией/сортировкой | `p-table` | `TableModule` |
| Текстовый инпут | `p-inputText` | `InputTextModule` |
| Инпут с иконкой | `p-iconField` + `p-inputIcon` | `IconFieldModule` |
| Пароль с маской | `p-password` | `PasswordModule` |
| Выпадающий список | `p-select` | `SelectModule` |
| Автодополнение | `p-autoComplete` | `AutoCompleteModule` |
| Кнопка | `p-button` | `ButtonModule` |
| Диалог | `p-dialog` | `DialogModule` |
| Диалог подтверждения | `p-confirmDialog` + `ConfirmationService` | `ConfirmDialogModule` |
| Toast-уведомления | `p-toast` + `MessageService` | `ToastModule` |
| Карточка | `p-card` | `CardModule` |
| Badge (счётчик) | `p-badge` | `BadgeModule` |
| Переключатель кнопок | `p-selectButton` | `SelectButtonModule` |
| Спиннер загрузки | `p-progressSpinner` | `ProgressSpinnerModule` |
| Прогресс-бар | `p-progressBar` | `ProgressBarModule` |
| Загрузка файлов | `p-fileUpload` | `FileUploadModule` |
| Графики (Chart.js) | `p-chart` | `ChartModule` |
| Меню навигации | `p-panelMenu` / `p-menu` | `PanelMenuModule` |
| Chip/Tag статуса | `p-tag` | `TagModule` |
| Разделитель | `p-divider` | `DividerModule` |

> Все компоненты — **standalone-friendly**: импортируются напрямую в `imports: []` компонента.

---

## 2) Структура проекта

```
frontend/
├── Dockerfile
├── src/
│   ├── app/
│   │   ├── core/                          # Singleton-сервисы, Guards, Interceptors (CoreModule-эквивалент)
│   │   │   ├── auth/
│   │   │   │   ├── auth.service.ts        # Signals: currentUser, isLoggedIn, role; login/logout/refresh
│   │   │   │   ├── auth.guard.ts          # CanActivateFn — проверка isLoggedIn
│   │   │   │   └── role.guard.ts          # CanActivateFn — проверка requiredRole из route.data
│   │   │   ├── interceptors/
│   │   │   │   ├── credentials.interceptor.ts  # withCredentials: true для API-запросов (httpOnly cookie)
│   │   │   │   ├── auth.interceptor.ts         # Добавляет Authorization: Bearer <token>
│   │   │   │   └── error.interceptor.ts        # 401 → refresh → retry; 403/429/5xx → toast
│   │   │   └── models/                    # TypeScript-интерфейсы, совпадающие с API DTO
│   │   │       ├── auth.models.ts
│   │   │       ├── user.models.ts
│   │   │       ├── teacher.models.ts
│   │   │       ├── topic.models.ts
│   │   │       ├── supervisor-request.models.ts
│   │   │       ├── application.models.ts
│   │   │       ├── chat.models.ts
│   │   │       ├── graduate-work.models.ts
│   │   │       ├── notification.models.ts
│   │   │       └── common.models.ts       # PagedResult<T>, ApiError, ProblemDetails
│   │   │
│   │   ├── shared/                        # Переиспользуемые «немые» компоненты и утилиты
│   │   │   ├── components/
│   │   │   │   ├── status-badge/          # Цветной badge по codeName статуса
│   │   │   │   ├── reject-dialog/         # p-dialog + обязательное поле комментария
│   │   │   │   ├── empty-state/           # Иллюстрация + текст «Ничего не найдено»
│   │   │   │   └── loading-overlay/       # p-progressSpinner при загрузке
│   │   │   └── pipes/
│   │   │       ├── status-label.pipe.ts   # codeName → displayName
│   │   │       └── full-name.pipe.ts      # { firstName, lastName, middleName } → строка
│   │   │
│   │   ├── layouts/
│   │   │   ├── auth-layout/               # Центрированный layout для страниц входа
│   │   │   └── main-layout/               # p-sidebar / custom toolbar + router-outlet
│   │   │       └── nav-items.ts           # Константа: меню по роли
│   │   │
│   │   ├── features/
│   │   │   ├── auth/
│   │   │   │   └── login/                 # LoginComponent
│   │   │   ├── teachers/
│   │   │   │   ├── teachers-list/
│   │   │   │   └── teacher-detail/
│   │   │   ├── topics/
│   │   │   │   ├── topics-list/
│   │   │   │   ├── topic-detail/
│   │   │   │   └── topic-form/            # create/edit (Teacher only)
│   │   │   ├── supervisor-requests/
│   │   │   │   ├── supervisor-requests-list/
│   │   │   │   └── supervisor-request-detail/
│   │   │   ├── applications/
│   │   │   │   ├── applications-list/
│   │   │   │   ├── application-create/
│   │   │   │   └── application-detail/    # включает чат-секцию
│   │   │   ├── chat/
│   │   │   │   └── chat-window/           # встраиваемый компонент (не отдельная страница)
│   │   │   ├── graduate-works/
│   │   │   │   ├── graduate-works-list/
│   │   │   │   └── graduate-work-detail/
│   │   │   ├── notifications/
│   │   │   │   └── notifications-list/
│   │   │   └── admin/
│   │   │       ├── users/
│   │   │       │   ├── users-list/
│   │   │       │   └── create-user-dialog/
│   │   │       ├── graduate-works-manage/
│   │   │       │   ├── admin-gw-list/
│   │   │       │   ├── create-gw-dialog/
│   │   │       │   └── upload-gw-file/    # шаги: create → upload-url → PUT S3 → confirm
│   │   │       ├── analytics/
│   │   │       └── export/
│   │   │
│   │   ├── app.routes.ts                  # Все маршруты с lazy loading
│   │   └── app.config.ts                  # provideRouter, provideHttpClient, providePrimeNG, provideAppInitializer
│   │
│   ├── environments/
│   │   ├── environment.ts                 # apiUrl: 'http://localhost:5001'
│   │   └── environment.prod.ts            # apiUrl: '/api' (nginx proxy)
│   └── styles.scss                        # Глобальные CSS-переменные палитры + базовые стили
│                                          # Цветовая схема: синий (#1a56db, #0d2d6b) + белый (#ffffff)
├── proxy.conf.json                    # Dev-прокси: /api → backend (localhost:5001)
├── playwright.config.ts               # Конфигурация E2E-тестов (создаётся при установке Playwright)
├── e2e/
│   ├── auth.spec.ts
│   ├── supervisor-requests.spec.ts
│   ├── applications.spec.ts
│   ├── chat.spec.ts
│   └── admin.spec.ts
└── angular.json
```

---

## 3) Базовые нефункциональные требования

| Требование | Решение |
|-----------|---------|
| Хранение токенов | Access token — в `AuthService` (signal, в памяти); Refresh token — **httpOnly cookie** (backend устанавливает через `Set-Cookie`; JS не видит → защита от XSS) |
| Отправка cookies | `credentials.interceptor.ts`: добавляет `withCredentials: true` ко всем `/api/` запросам — без этого браузер не отправляет httpOnly cookie |
| Авто-обновление токена | `error.interceptor.ts`: при 401 → `authService.refresh()` → повтор оригинального запроса через `switchMap` (заголовок `X-Auth-Retry` предотвращает бесконечный цикл) |
| Глобальная обработка ошибок | `error.interceptor.ts`: 403/429/5xx → `MessageService` (PrimeNG `p-toast`) с соответствующим сообщением; ошибки на auth-URL (`/auth/login`, `/auth/refresh`) пробрасываются компоненту |
| CORS | В dev — `proxy.conf.json` (`ng serve`); в prod — nginx reverse-proxy; backend разрешает `AllowCredentials()` при непустом `Cors:AllowedOrigins` |
| Защита маршрутов | `authGuard` + `roleGuard` на каждой ленивой группе маршрутов |
| Ролевая навигация | `nav-items.ts`: меню отфильтровывается по `authService.role()` в `MainLayout` |
| Pagination | Backend возвращает `{ items, total, page, pageSize }`; встроенный paginator `p-table` управляет `page`/`pageSize` |
| Polling | `timer(0, POLL_INTERVAL).pipe(switchMap(() => service.call()), takeUntilDestroyed())` — авто-отмена при destroy |
| Единый формат ошибок | Backend: `ProblemDetails`; парсится в `ApiError` в interceptor'е; поля `errors` → под-ошибки формы |
| Loading state | `isLoading = signal(false)` в каждом компоненте; `p-progressSpinner` + overlay блокирует повторные отправки |
| Responsive | PrimeFlex grid (`p-fluid`, `p-grid`); sidebar скрывается на мобильных |
| Уведомления пользователю | `MessageService` + `<p-toast>` в `AppComponent`; вызывается из `error.interceptor` и бизнес-логики |
| Диалоги подтверждения | `ConfirmationService` + `<p-confirmDialog>` (встроенный PrimeNG механизм) |

---

## 4) Общие TypeScript-интерфейсы (core/models)

Интерфейсы должны точно совпадать с API DTO. Источник истины — Swagger (`/swagger`).

```typescript
// common.models.ts
interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

interface ProblemDetails {
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

// auth.models.ts
// Backend возвращает AccessTokenDto на login и refresh.
// Refresh-токен передаётся ТОЛЬКО через httpOnly cookie (Set-Cookie),
// в теле ответа его нет. Тело запроса на refresh — пустое.
interface AccessTokenDto {
  accessToken: string;
  userId: string;
  email: string;
  role: string;
}

interface UserInfo {
  userId: string;
  email: string;
  role: string;
}

// application.models.ts — статусы заявок (строковые коды)
type ApplicationStatusCode =
  | 'Pending'
  | 'ApprovedBySupervisor'
  | 'RejectedBySupervisor'
  | 'PendingDepartmentHead'
  | 'ApprovedByDepartmentHead'
  | 'RejectedByDepartmentHead'
  | 'Cancelled';

// Статусы, при которых чат доступен (проверять на frontend)
const CHAT_ACTIVE_STATUSES: ApplicationStatusCode[] = [
  'Pending', 'ApprovedBySupervisor', 'PendingDepartmentHead'
];
```

---

## 5) Итерации (roadmap)

Нумерация намеренно начинается с 0 для согласованности с `BackendDevelopmentPlan.md`.

---

### Итерация 0 — «Скелет + инфраструктура»

**Цель:** работающее Angular-приложение с авторизацией, guards и layouts; нет бизнес-страниц, но есть фундамент для всего остального.

#### Шаги

**0.1 Создать проект и установить PrimeNG**

```bash
ng new academic-topic-selection-frontend --standalone --routing --style=scss
cd academic-topic-selection-frontend

# PrimeNG 20 + темы (Aura, Lara, Nora)
npm install primeng@^20 @primeng/themes@^20 primeicons

# PrimeFlex (опциональная утилитарная CSS-библиотека)
npm install primeflex
```

В `angular.json` добавить в `styles`:
```json
"styles": [
  "node_modules/primeicons/primeicons.css",
  "src/styles.scss"
]
```

**0.2 Настроить environments и proxy**

В dev-режиме используем `proxy.conf.json` для проксирования `/api` на backend. Это позволяет избежать CORS-проблем и работать с httpOnly cookies:

```json
// proxy.conf.json (корень frontend/)
{
  "/api": {
    "target": "http://localhost:5001",
    "secure": false,
    "changeOrigin": true
  }
}
```

В `angular.json` секция `serve.options`:
```json
"serve": {
  "options": {
    "proxyConfig": "proxy.conf.json"
  }
}
```

Environments — одинаковый `apiUrl` для dev и prod (proxy в обоих случаях):

```typescript
// environment.ts
export const environment = {
  production: false,
  apiUrl: '/api/v1',   // через proxy.conf.json → backend
};

// environment.prod.ts
export const environment = {
  production: true,
  apiUrl: '/api/v1',   // через nginx reverse-proxy
};
```

**0.3 app.config.ts**

```typescript
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import Aura from '@primeng/themes/aura';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';

import { appRoutes } from './app.routes';
import { AuthService } from './core/auth/auth.service';
import { credentialsInterceptor } from './core/interceptors/credentials.interceptor';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes, withComponentInputBinding()),
    provideHttpClient(
      withInterceptors([credentialsInterceptor, authInterceptor, errorInterceptor]),
    ),
    providePrimeNG({
      theme: {
        preset: Aura,
        options: { darkModeSelector: '.dark-mode' },
      },
      ripple: true,
    }),
    MessageService,       // PrimeNG Toast — singleton
    ConfirmationService,  // PrimeNG ConfirmDialog — singleton
    provideAppInitializer(() => inject(AuthService).restoreSession()),
  ],
};
```

> **Примечание:** `provideAnimationsAsync()` устарел в Angular 20.2+ и будет удалён в v23. Angular-анимации в этом проекте не используются. PrimeNG 20 работает через CSS-анимации.

**0.4 AuthService (Signals)**

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _currentUser = signal<UserInfo | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn = computed(() => this._currentUser() !== null);
  readonly role = computed(() => this._currentUser()?.role ?? null);

  private accessToken = signal<string | null>(null);

  login(email: string, password: string): Observable<void>
  logout(): Observable<void>
  refresh(): Observable<string>            // возвращает новый accessToken
  restoreSession(): Promise<void>          // вызывается в provideAppInitializer
  getAccessToken(): string | null          // используется в interceptor
}
```

**0.5 credentials.interceptor.ts**

Обеспечивает передачу httpOnly cookie (refresh token) на все API-запросы. Без `withCredentials: true` браузер не отправляет cookie — refresh не работает.

```typescript
import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export const credentialsInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.startsWith('/api/') || req.url.startsWith(environment.apiUrl)) {
    return next(req.clone({ withCredentials: true }));
  }
  return next(req);
};
```

**0.6 auth.interceptor.ts**

Добавляет `Authorization: Bearer <token>` ко всем запросам, кроме публичных auth-эндпоинтов (login/refresh/logout — они используют cookie, а не Bearer):

```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const url = req.url;
  if (url.includes('/auth/login') || url.includes('/auth/refresh') || url.includes('/auth/logout')) {
    return next(req);
  }

  const token = inject(AuthService).getAccessToken();
  if (token) {
    return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
  }
  return next(req);
};
```

**0.7 error.interceptor.ts**

Обработка HTTP-ошибок с двумя уровнями:
- **401** → пробует `refresh()` → повторяет запрос с заголовком `X-Auth-Retry`; если refresh тоже упал → `clearSession()` + редирект на `/login`
- **403 / 429 / 5xx** → показывает toast-уведомление через PrimeNG `MessageService`
- Ошибки на публичных auth-URL (`/auth/login`, `/auth/refresh`) пробрасываются без обработки — их обрабатывает сам компонент (LoginComponent показывает свои сообщения)

```typescript
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const messageService = inject(MessageService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // Публичные auth URL — пробрасываем как есть (компонент обработает)
      if (isAuthPublicUrl(req.url)) {
        return throwError(() => err);
      }

      // 401 → refresh + retry
      if (err.status === 401) {
        if (req.headers.has('X-Auth-Retry')) {
          auth.clearSession();
          void router.navigateByUrl('/login');
          return throwError(() => err);
        }
        return auth.refresh().pipe(
          switchMap(() => next(req.clone({ headers: req.headers.set('X-Auth-Retry', '1') }))),
          catchError(() => {
            auth.clearSession();
            void router.navigateByUrl('/login');
            return throwError(() => err);
          }),
        );
      }

      // 403 / 429 / 5xx → toast
      if (err.status === 403) {
        messageService.add({ severity: 'error', summary: 'Доступ запрещён', detail: 'Недостаточно прав для выполнения операции.' });
      } else if (err.status === 429) {
        messageService.add({ severity: 'warn', summary: 'Подождите', detail: 'Слишком много запросов. Попробуйте позже.' });
      } else if (err.status >= 500) {
        messageService.add({ severity: 'error', summary: 'Ошибка сервера', detail: 'Сервис временно недоступен. Попробуйте позже.' });
      }

      return throwError(() => err);
    }),
  );
};
```

**0.8 AuthGuard и RoleGuard**

```typescript
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? true : router.createUrlTree(['/login']);
};

export const roleGuard: CanActivateFn = (route) => {
  const requiredRole = route.data['role'] as string | undefined;
  if (!requiredRole) return true;

  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.role() === requiredRole ? true : router.createUrlTree(['/']);
};
```

> **Примечание:** `roleGuard` объявляется в итерации 0, но подключается к маршрутам начиная с **итерации 3** (когда появляются ролезависимые страницы — `TopicFormComponent` только для Teacher, и далее в итерациях 4-9).

**0.9 app.routes.ts (скелет)**

```typescript
export const appRoutes: Routes = [
  {
    path: 'login',
    component: AuthLayoutComponent,          // обёртка с центрированием
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/auth/login/login.component').then(m => m.LoginComponent),
      },
    ],
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'topics' },
      // Lazy routes добавляются в последующих итерациях
    ],
  },
];
```

**0.10 Layouts**
- `AuthLayoutComponent` — центрированная карточка (`p-card`), без шапки
- `MainLayoutComponent` — кастомная боковая панель (div + SCSS) + `<router-outlet>`; навигация через `p-panelMenu` или `p-menu`; в `AppComponent` добавить `<p-toast>` и `<p-confirmDialog>` один раз

> **Примечание по layout:** PrimeNG не имеет встроенного sidenav-аналога уровня Angular Material; используем кастомный CSS layout (flexbox) + `p-menu` / `p-panelMenu` для навигации. PrimeFlex упрощает сетку.

#### Проверка итерации 0
- [ ] `ng serve` запускается без ошибок
- [ ] Переход на `/` без токена → редирект на `/login`
- [ ] `ng build` компилируется
- [ ] `ng test` прогоняет scaffold-тесты без ошибок

---

### Итерация 1 — «Страница входа»

**Цель:** полнофункциональный login flow с обработкой ошибок API.

#### Шаги

**1.1 Подключить `ReactiveFormsModule` к `LoginComponent`**

Angular предлагает два способа работы с формами: Template-driven и Reactive. Мы используем **Reactive Forms** — они дают полный контроль над состоянием формы и валидацией через TypeScript-код, а не через атрибуты в шаблоне.

Для Reactive Forms нужен `ReactiveFormsModule`. В standalone-компоненте он подключается прямо в `imports`:

```typescript
// login.component.ts
import { ReactiveFormsModule } from '@angular/forms';

@Component({
  ...
  imports: [ReactiveFormsModule, ...],
})
```

---

**1.2 Создать тип формы и сам `FormGroup`**

`FormGroup` — это объект, который Angular использует для управления группой полей формы. Каждое поле — `FormControl`.

Сначала опишем тип формы (TypeScript-интерфейс). Это нужно, чтобы IDE подсказывала типы при обращении к полям:

```typescript
// login.component.ts
import { FormControl, FormGroup, Validators } from '@angular/forms';

interface LoginForm {
  email: FormControl<string>;
  password: FormControl<string>;
}
```

Затем создаём экземпляр `FormGroup` в классе компонента:

```typescript
readonly form = new FormGroup<LoginForm>({
  email: new FormControl('', {
    nonNullable: true,                                    // значение всегда string, никогда null
    validators: [Validators.required, Validators.email],
  }),
  password: new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(8)],
  }),
});
```

> `nonNullable: true` — важный флаг: без него TypeScript считает значение поля `string | null`, что добавляет ненужные проверки.

---

**1.3 Добавить состояние загрузки и ошибки**

Используем Angular Signals (как уже сделано в `AuthService`) — это современный реактивный примитив Angular:

```typescript
import { signal } from '@angular/core';

readonly isLoading = signal(false);   // true пока идёт HTTP-запрос
readonly errorMessage = signal<string | null>(null);  // текст ошибки под формой
```

---

**1.4 Заинжектить зависимости**

В Angular зависимости (сервисы, роутер) получаются через функцию `inject()` — это современный способ вместо constructor-injection:

```typescript
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

private readonly auth = inject(AuthService);
private readonly router = inject(Router);
```

---

**1.5 Написать метод `submit()`**

Этот метод будет вызываться при нажатии кнопки «Войти»:

```typescript
import { HttpErrorResponse } from '@angular/common/http';

submit(): void {
  if (this.form.invalid) {
    this.form.markAllAsTouched();  // показать валидационные ошибки под полями
    return;
  }

  this.isLoading.set(true);
  this.errorMessage.set(null);  // сбрасываем предыдущую ошибку

  const { email, password } = this.form.getRawValue();

  this.auth.login(email, password).subscribe({
    next: () => {
      // Успех: перенаправляем на главную страницу
      void this.router.navigateByUrl('/');
    },
    error: (err: HttpErrorResponse) => {
      this.isLoading.set(false);
      this.errorMessage.set(this.resolveError(err));
    },
  });
}
```

> Обрати внимание: `isLoading.set(false)` вызывается только в `error`. При успехе происходит переход на новую страницу, и компонент уничтожается — сбрасывать флаг не нужно.

---

**1.6 Написать вспомогательный метод `resolveError()`**

Переводит HTTP-статус в понятный пользователю текст:

```typescript
private resolveError(err: HttpErrorResponse): string {
  if (err.status === 400 || err.status === 401) {
    return 'Неверный email или пароль.';
  }
  if (err.status === 429) {
    return 'Слишком много попыток входа. Подождите несколько минут.';
  }
  return 'Сервис временно недоступен. Попробуйте позже.';
}
```

---

**1.7 Итоговый класс `LoginComponent`**

```typescript
// login.component.ts
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';

import { AuthService } from '../../../core/auth/auth.service';

interface LoginForm {
  email: FormControl<string>;
  password: FormControl<string>;
}

@Component({
  selector: 'app-login',
  imports: [Card, ReactiveFormsModule, Button, InputText],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = new FormGroup<LoginForm>({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8)],
    }),
  });

  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.form.getRawValue();

    this.auth.login(email, password).subscribe({
      next: () => void this.router.navigateByUrl('/'),
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.resolveError(err));
      },
    });
  }

  private resolveError(err: HttpErrorResponse): string {
    if (err.status === 400 || err.status === 401) return 'Неверный email или пароль.';
    if (err.status === 429) return 'Слишком много попыток входа. Подождите несколько минут.';
    return 'Сервис временно недоступен. Попробуйте позже.';
  }
}
```

---

**1.8 Шаблон `login.component.html`**

Разберём каждую часть шаблона:

- `[formGroup]="form"` — привязывает HTML-форму к нашему `FormGroup`
- `formControlName="email"` — связывает input с конкретным `FormControl`
- `(ngSubmit)="submit()"` — вызывает метод при отправке формы (нажатие Enter или кнопки submit)
- `[disabled]="isLoading()"` — кнопка задизейблена пока идёт запрос
- `@if(...)` — новый синтаксис Angular 17+ для условного рендеринга (вместо `*ngIf`)

```html
<!-- login.component.html -->
<p-card header="Вход в систему">
  <form [formGroup]="form" (ngSubmit)="submit()" class="login-form">

    <div class="field">
      <label for="email">Email</label>
      <input
        pInputText
        id="email"
        type="email"
        formControlName="email"
        placeholder="you@example.com"
        autocomplete="email"
        class="w-full"
      />
      @if (form.controls.email.invalid && form.controls.email.touched) {
        <small class="error">Введите корректный email.</small>
      }
    </div>

    <div class="field">
      <label for="password">Пароль</label>
      <input
        pInputText
        id="password"
        type="password"
        formControlName="password"
        placeholder="Минимум 8 символов"
        autocomplete="current-password"
        class="w-full"
      />
      @if (form.controls.password.invalid && form.controls.password.touched) {
        <small class="error">Пароль должен содержать не менее 8 символов.</small>
      }
    </div>

    @if (errorMessage()) {
      <div class="error-banner" role="alert">{{ errorMessage() }}</div>
    }

    <p-button
      type="submit"
      label="Войти"
      styleClass="w-full"
      [loading]="isLoading()"
      [disabled]="isLoading()"
    />

  </form>
</p-card>
```

> `form.controls.email.touched` — поле становится "touched" когда пользователь кликнул на него и ушёл. Без этой проверки ошибки показывались бы сразу при загрузке страницы, ещё до того как пользователь что-то ввёл.

---

**1.9 Стили `login.component.scss`**

Используем CSS-переменные из `styles.scss`. Все цвета берём из глобальной палитры (`--blue-primary`, `--text-primary`, `--error` и т.д.), не хардкодим значения напрямую.

```scss
.login-form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  min-width: 340px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;

  label {
    font-size: 0.8125rem;
    font-weight: 600;
    color: var(--text-primary);
    letter-spacing: 0.01em;
  }
}

.field input:focus {
  outline: none;
  border-color: var(--blue-primary) !important;
  box-shadow: 0 0 0 3px rgba(26, 86, 219, 0.12);
}

.error {
  color: var(--error);
  font-size: 0.8rem;
}

.error-banner {
  color: var(--error);
  background: var(--error-bg);
  border: 1px solid var(--error-border);
  border-radius: 6px;
  padding: 0.625rem 0.875rem;
  font-size: 0.875rem;
}
```

---

**1.10 Что нужно добавить в `app.config.ts`**

Для `ReactiveFormsModule` ничего дополнительно в конфиг добавлять не нужно — он подключается прямо в `imports` компонента.

---

#### API

- `POST /api/v1/auth/login` — тело запроса: `{ email, password }`
- Ответ при успехе: `200 OK` → `AccessTokenDto { accessToken, userId, email, role }`  
  (модель уже описана в `src/app/core/models/auth.models.ts`)
- Ответ при ошибке: `400` / `401` → неверные данные, `429` → rate limit, `5xx` → ошибка сервера

---

#### Проверка итерации 1

- [ ] `ng serve` запускается без ошибок
- [ ] Страница `/login` отображает форму с двумя полями и кнопкой
- [ ] При попытке отправить пустую форму — ошибки валидации появляются под полями
- [ ] При вводе некорректного email — ошибка под полем email
- [ ] Кнопка «Войти» задизейблена и показывает спиннер во время запроса
- [ ] Успешный вход → редирект на `/` (который сразу перенаправит на `/topics`)
- [ ] Неверный пароль → красное сообщение под формой, кнопка снова активна, форма не заблокирована
- [ ] `429` → соответствующее сообщение об ошибке
- [ ] Обновление страницы после входа → `restoreSession()` восстанавливает сессию, пользователь остаётся на `/topics`
- [ ] `ng build` компилируется без ошибок

---

### Итерация 2 — «Главный layout + ролевая навигация + badge уведомлений»

**Цель:** навигационный каркас для всех последующих страниц: боковое меню по роли, шапка с пользователем и счётчиком непрочитанных уведомлений, корректный logout.

**Опора на итерации 0–1:** уже есть `MainLayoutComponent`, `AuthService` (signals, `role()`, `logout()`), `authGuard`, `environment.apiUrl`, interceptors. В этой итерации расширяем layout и добавляем сервис badge; маршруты к «настоящим» страницам появятся в итерациях 3+.

---

#### Шаги

**2.1 Тип роли и меню в `nav-items.ts`**

Чтобы не опечатываться в строках `'Student' | 'Teacher' | …`, введи union-тип роли и используй его везде, где сравниваешь роль.

1. В `src/app/core/models/auth.models.ts` добавь:

```typescript
export type UserRole = 'Student' | 'Teacher' | 'DepartmentHead' | 'Admin';
```

и замени поле `role: string` на `role: UserRole` в интерфейсах `AccessTokenDto` и `UserInfo` (ответ API должен совпадать с этими строками).

2. Создай файл `src/app/layouts/main-layout/nav-items.ts`:

```typescript
import type { UserRole } from '../../core/models/auth.models';

export interface NavItem {
  label: string;
  icon: string; // классы PrimeIcons, например 'pi pi-book'
  route: string;
  roles: UserRole[];
}

export const NAV_ITEMS: NavItem[] = [
  { label: 'Преподаватели', icon: 'pi pi-users',      route: '/teachers',             roles: ['Student'] },
  { label: 'Темы',          icon: 'pi pi-book',      route: '/topics',               roles: ['Student', 'Teacher'] },
  { label: 'Мои запросы',   icon: 'pi pi-send',      route: '/supervisor-requests',  roles: ['Student'] },
  { label: 'Мои заявки',    icon: 'pi pi-file-edit', route: '/applications',         roles: ['Student'] },
  { label: 'Запросы (вх.)', icon: 'pi pi-inbox',     route: '/supervisor-requests',  roles: ['Teacher'] },
  { label: 'Заявки',        icon: 'pi pi-file-edit', route: '/applications',         roles: ['Teacher', 'DepartmentHead'] },
  { label: 'Архив ВКР',     icon: 'pi pi-server',    route: '/graduate-works',       roles: ['Student', 'Teacher', 'DepartmentHead'] },
  { label: 'Уведомления',   icon: 'pi pi-bell',      route: '/notifications',        roles: ['Student', 'Teacher', 'DepartmentHead'] },
  { label: 'Пользователи',  icon: 'pi pi-user-edit', route: '/admin/users',          roles: ['Admin'] },
  { label: 'Архив ВКР',     icon: 'pi pi-server',    route: '/admin/graduate-works', roles: ['Admin'] },
  { label: 'Аналитика',     icon: 'pi pi-chart-bar', route: '/admin/analytics',      roles: ['Admin'] },
  { label: 'Экспорт',       icon: 'pi pi-download',  route: '/admin/export',         roles: ['Admin'] },
];
```

> **Для начинающих:** `NavItem[]` — массив объектов меню; поле `roles` задаёт, у кого пункт виден. Один и тот же `route` может встречаться у разных ролей с разными подписями (например, запросы у студента и у преподавателя).

---

**2.2 `PagedResult<T>` для ответа списка уведомлений**

Если файла ещё нет, создай `src/app/core/models/common.models.ts` (как в разделе 4 плана):

```typescript
export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
```

Сервис badge будет брать из ответа поле `total` при `pageSize: 1` — так не загружаем лишние элементы.

---

**2.3 Ролевое меню в `MainLayoutComponent`**

1. Импортируй `computed` из `@angular/core`, `NAV_ITEMS` из `./nav-items.ts`.

2. Добавь вычисляемый список пунктов меню:

```typescript
readonly navItems = computed(() => {
  const role = this.auth.role();
  if (!role) return [];
  return NAV_ITEMS.filter((item) => item.roles.includes(role));
});
```

> **`computed`:** пересчитывается автоматически, когда меняется `auth.role()` (или другие сигналы внутри формулы). В шаблоне вызывай как функцию: `navItems()`.

3. В `main-layout.component.html` в `<nav class="sidebar__nav">` вместо одной жёсткой ссылки используй цикл `@for` (как в итерации 1 для `@if`):

- для каждого `item`: `<a [routerLink]="item.route" routerLinkActive="is-active">` + иконка `<i [class]="item.icon"></i>` + текст `{{ item.label }}`.

4. Стили ссылок уже заданы в `main-layout.component.scss`; при необходимости добавь flex/gap для строки «иконка + текст».

---

**2.4 Шапка: пользователь, колокольчик, `p-badge`, выход**

1. Подключи в `imports` компонента PrimeNG: `Badge` из `primeng/badge`, при необходимости `Button` (уже есть).

2. В `topbar` после email пользователя:
   - ссылка или кнопка с `routerLink="/notifications"` (или только иконка внутри `<a>`);
   - иконка `pi pi-bell`;
   - рядом `<p-badge [value]="..." />` **только если** `unreadCount() > 0` (через `@if` в шаблоне), либо `[hidden]="unreadCount() === 0"` — чтобы badge не показывал «0».

> **Для начинающих:** сигнал в шаблоне всегда с скобками: `unreadCount()`, иначе Angular увидит саму функцию-сигнал, а не число.

3. **Logout:** оставь вызов `this.auth.logout().subscribe({ next: () => this.router.navigateByUrl('/login') })`. В `AuthService.logout()` уже: `POST` на `${environment.apiUrl}/auth/logout` с `withCredentials`, затем `clearSession()` (access token и пользователь сбрасываются). Дополнительно в `next` (или `finalize`) вызови `notificationBadge.reset()` — чтобы счётчик не «залип» после выхода другого пользователя на том же браузере.

---

**2.5 `NotificationBadgeService`**

Файл, например: `src/app/core/notifications/notification-badge.service.ts`.

Требования:

- `@Injectable({ providedIn: 'root' })` — один экземпляр на всё приложение (singleton).
- `readonly unreadCount = signal(0);`
- **`startPolling(role: UserRole | null)`:** если `role === 'Admin'` или `role === null` — не подписываться; иначе `timer(0, 30_000).pipe(switchMap(() => this.http.get<PagedResult<unknown>>(...)), ...).subscribe(...)`.
- URL запроса: `` `${environment.apiUrl}/notifications?isRead=false&page=1&pageSize=1` `` (числа `page`/`pageSize` согласуй со Swagger, главное — поле `total` в ответе).
- В `subscribe`: `this.unreadCount.set(result.total)`.
- **`catchError`:** при ошибке сети/API не рвать цепочку навсегда — верни `of(null)` и не обновляй счётчик, либо оставь предыдущее значение.
- **Защита от двойного `startPolling`:** храни `Subscription` в поле класса; при повторном вызове сначала `unsubscribe()`, либо не запускай второй раз, если polling уже активен.
- **`decrement()`:** `update(n => Math.max(0, n - 1))` на сигнале — пригодится в итерации со списком уведомлений при отметке «прочитано».
- **`reset()`:** `unreadCount.set(0)` и отписка от polling при logout.
- Используй обычный `HttpClient` (не `HttpBackend`): к запросу добавятся interceptors (Bearer, credentials, refresh на 401).

Запуск polling из `MainLayoutComponent`:

- вариант для новичка: `ngOnInit` + `implements OnInit`, вызов `notificationBadge.startPolling(this.auth.role())`;
- при logout — `reset()` (см. выше).

---

**2.6 Маршруты и «ещё не готовые» страницы**

После добавления пунктов меню пользователь может перейти на `/teachers`, `/applications` и т.д. До итерации 3 маршрутов может не быть.

Выбери один вариант:

- **A (рекомендуется для итерации 2):** в `app.routes.ts` добавь `{ path: '**', redirectTo: 'topics' }` внутри дочерних маршрутов под `MainLayoutComponent`, чтобы неизвестный путь вёл на существующую заглушку/страницу.
- **B:** временно фильтруй `NAV_ITEMS` в коде только до маршрутов, которые уже объявлены (хрупко, лучше A).

---

#### API

- `POST /api/v1/auth/logout` — тело пустое `{}`; cookie refresh очищается на стороне сервера; на клиенте после ответа (или после `catchError` в сервисе) вызывается `clearSession()`.
- `GET /api/v1/notifications?isRead=false&page=1&pageSize=1` — ответ `PagedResult<NotificationDto>`; для badge достаточно поля `total`.

---

#### Проверка итерации 2

- [ ] В sidebar отображаются только пункты, у которых в `roles` есть текущая роль пользователя
- [ ] Иконки PrimeIcons отображаются (глобально подключен `primeicons.css`)
- [ ] После «Выйти»: редирект на `/login`, повторный заход на `/` без сессии ведёт на логин
- [ ] Для роли `Admin` polling уведомлений не запускается (или не дергает API)
- [ ] Для `Student` / `Teacher` / `DepartmentHead` каждые ~30 с обновляется `unreadCount` (в DevTools → Network виден запрос)
- [ ] Badge на колокольчике появляется при `unreadCount > 0` и скрывается при `0`
- [ ] `ng build` без ошибок

---

### Итерация 3 — «Каталог преподавателей и тем»

**Цель:** read-only просмотр для всех ролей; управление темами — для `Teacher`.

#### Детализация реализации (шаги)

**3.1 Модели и DTO (frontend `core/models`)**

Добавить (или актуализировать) интерфейсы, совпадающие с backend DTO:

- `teacher.models.ts` → `TeacherDto`
- `topic.models.ts` → `TopicDto`, `TopicsFilter`, `CreateTopicCommand`, `UpdateTopicCommand`
- `common.models.ts` → `DictionaryItemRef` (для `status` и `creatorType` в теме)

Критично: поля `createdByUserId`, `status.codeName`, `creatorType.codeName` нужны для UI-правил (кто может редактировать тему и как фильтровать).

---

**3.2 API-сервисы**

`TeachersApiService`:
- `GET /api/v1/teachers` с query-параметрами `query`, `page`, `pageSize`
- `GET /api/v1/teachers/{id}`

`TopicsApiService`:
- `GET /api/v1/topics` c фильтрами `query`, `statusCodeName`, `createdByUserId`, `creatorTypeCodeName`, `sort`, `page`, `pageSize`
- `GET /api/v1/topics/{id}`
- `POST /api/v1/topics`
- `PATCH /api/v1/topics/{id}`
- `DELETE /api/v1/topics/{id}`

---

**3.3 Маршруты (`app.routes.ts`)**

Добавить страницы:

- `/teachers` → `TeachersListComponent`
- `/teachers/:id` → `TeacherDetailComponent`
- `/topics` → `TopicsListComponent`
- `/topics/:id` → `TopicDetailComponent`
- `/topics/new` → `TopicFormComponent` + `canActivate: [roleGuard]`, `data: { role: 'Teacher' }`
- `/topics/:id/edit` → `TopicFormComponent` + `canActivate: [roleGuard]`, `data: { role: 'Teacher' }`

---

**3.4 Компонент `TeachersListComponent`**

- Поиск через `FormControl` + `debounceTime(300)`
- Табличный список: ФИО, email, степень, звание, должность, лимит
- Пагинация (минимум: page/pageSize + next/prev)
- Переход в детали: `/teachers/:id`

---

**3.5 Компонент `TeacherDetailComponent`**

- Загрузка карточки преподавателя
- Отдельный запрос тем преподавателя через `createdByUserId=<teacher.userId>` и `creatorTypeCodeName=Teacher`
- Вывод списка тем с переходом в `/topics/:id`

---

**3.6 Компонент `TopicsListComponent`**

- Поиск по названию/описанию (`query` + debounce)
- Фильтр статуса (`statusCodeName`: `Active` / `Inactive`)
- Фильтр по преподавателю (`createdByUserId`) через select со списком преподавателей
- Пагинация
- Для `Teacher` показывать кнопку «Добавить тему» (`/topics/new`)
- Для автора темы (`currentUser.userId === topic.createdByUserId`) показывать «Изменить»

---

**3.7 Компонент `TopicDetailComponent`**

- Отображение полей темы, автора, статуса, дат
- Для автора-`Teacher`: кнопки «Редактировать» и «Удалить»
- Удаление — через `ConfirmationService.confirm()` + `DELETE /topics/{id}` + редирект на список

---

**3.8 Компонент `TopicFormComponent`**

- Режим create: `POST /topics` (`creatorTypeCodeName = 'Teacher'`)
- Режим edit: загрузка `GET /topics/{id}`, затем `PATCH /topics/{id}`
- В режиме edit проверять, что текущий пользователь — автор темы; иначе форма блокируется и показывается сообщение о правах
- Поля формы: `title` (required, max 500), `description`, `statusCodeName`
- После успешного сохранения — переход в `TopicDetailComponent`

#### Проверка итерации 3 (расширенный чек-лист)
- [ ] `/teachers` загружает данные, поиск с debounce работает, пагинация переключает страницы
- [ ] `/teachers/:id` показывает профиль и связанные темы преподавателя
- [ ] `/topics` показывает список тем, работает поиск и фильтр по статусу
- [ ] `Teacher` видит кнопку «Добавить тему», `Student`/`DepartmentHead` — не видят
- [ ] `/topics/new` и `/topics/:id/edit` доступны только `Teacher` (через `roleGuard`)
- [ ] Автор темы может редактировать/удалять, не-автор не видит эти действия
- [ ] Удаление темы подтверждается через `p-confirmDialog` и после успеха возвращает на `/topics`
- [ ] `ng build` проходит без ошибок

---

### Итерация 4 — «Поток 1: Выбор научного руководителя (SupervisorRequests)»

**Цель:** полный UI для потока `SupervisorRequests`.

#### Страницы

**`/supervisor-requests`** → `SupervisorRequestsListComponent`
- **Student** видит свои запросы (статус, преподаватель, дата)
- **Teacher** видит входящие запросы (студент, группа, дата)
- Клик → `/supervisor-requests/:id`

**`/supervisor-requests/:id`** → `SupervisorRequestDetailComponent`
- Детали запроса
- **Student** (если `Pending`): кнопка «Отменить» → `ConfirmationService.confirm()`
- **Teacher** (если `Pending`): кнопки «Принять» + «Отклонить»; отклонение → `RejectDialogComponent` (`p-dialog` с обязательным полем комментария)

**Создание запроса** — через кнопку на `TeacherDetailComponent`:
- Модальный диалог или редирект на форму
- POST `/api/v1/supervisor-requests` с `teacherUserId`

#### Сервис

```typescript
@Injectable({ providedIn: 'root' })
export class SupervisorRequestsApiService {
  getRequests(params?): Observable<PagedResult<SupervisorRequestDto>>
  getById(id: string): Observable<SupervisorRequestDto>
  create(teacherUserId: string): Observable<SupervisorRequestDto>
  approve(id: string): Observable<void>
  reject(id: string, comment: string): Observable<void>
  cancel(id: string): Observable<void>
}

interface SupervisorRequestDto {
  id: string;
  student: { id: string; fullName: string; studyGroup?: string; };
  teacher: { id: string; fullName: string; };
  status: string;        // codeName: Pending | ApprovedBySupervisor | ...
  statusDisplayName: string;
  comment?: string;
  createdAt: string;
}
```

#### Бизнес-правила на UI

| Правило | Реализация |
|---------|-----------|
| «Принять» и «Отклонить» — только для `Pending` | Показывать кнопки только при `status === 'Pending'` и `role === 'Teacher'` |
| «Отменить» — только для `Pending`, только Student | Условный рендеринг |
| Комментарий при отклонении — обязателен | Validators.required в `RejectDialogComponent` |
| После одобрения — у студента отменяются остальные запросы | Backend делает сам; фронтенд обновляет список по возврату |

#### Проверка итерации 4
- [ ] Student отправляет запрос → Teacher видит его в «Входящих»
- [ ] Teacher одобряет → статус у Student становится `ApprovedBySupervisor`
- [ ] Teacher отклоняет с комментарием → статус `RejectedBySupervisor`, комментарий виден
- [ ] Кнопки действий не отображаются для неподходящих статусов

---

### Итерация 5 — «Поток 2: Заявка на утверждение темы (Applications)»

**Цель:** полный UI для потока `StudentApplications`.

#### Страницы

**`/applications`** → `ApplicationsListComponent`
- Список с фильтрами по статусу
- Цветные статус-badge через `StatusBadgeComponent`
- Клик → `/applications/:id`

**`/applications/new`** → `ApplicationCreateComponent` (только `Student`)
- Выбор темы (`p-autoComplete` из `/api/v1/topics`) — или поле «Предложить свою тему»
- Выбор `SupervisorRequest` (`p-select` — только `ApprovedBySupervisor` запросы студента)
- POST `/api/v1/applications`

**`/applications/:id`** → `ApplicationDetailComponent`
- Полные детали: статус, тема, студент, преподаватель, история действий (`application-actions`)
- Кнопки действий по роли и статусу (см. таблицу ниже)
- Секция чата (Итерация 6)

#### Матрица действий по ролям и статусам

| Роль | Статус | Доступные действия |
|------|--------|-------------------|
| Student | Pending, ApprovedBySupervisor | «Отменить» |
| Teacher | Pending | «Одобрить», «Отклонить (с комментарием)» |
| Teacher | ApprovedBySupervisor | «Передать заведующему» |
| DepartmentHead | PendingDepartmentHead | «Утвердить», «Отклонить (с комментарием)» |
| Любой | Терминальный | Только просмотр |

#### Цвета статус-badge

```typescript
export const STATUS_COLORS: Record<string, string> = {
  'Pending':                   'status-pending',       // amber
  'ApprovedBySupervisor':      'status-approved',      // blue
  'PendingDepartmentHead':     'status-pending-head',  // purple
  'ApprovedByDepartmentHead':  'status-success',       // green
  'RejectedBySupervisor':      'status-rejected',      // red
  'RejectedByDepartmentHead':  'status-rejected',      // red
  'Cancelled':                 'status-cancelled',     // gray
};
```

#### Сервис

```typescript
@Injectable({ providedIn: 'root' })
export class ApplicationsApiService {
  getApplications(params?): Observable<PagedResult<ApplicationDto>>
  getById(id: string): Observable<ApplicationDetailDto>
  create(command: CreateApplicationCommand): Observable<ApplicationDto>
  approve(id: string): Observable<void>
  reject(id: string, comment: string): Observable<void>
  submitToDepartmentHead(id: string): Observable<void>
  departmentHeadApprove(id: string): Observable<void>
  departmentHeadReject(id: string, comment: string): Observable<void>
  cancel(id: string): Observable<void>
}

interface CreateApplicationCommand {
  topicId?: string;
  proposedTitle?: string;
  proposedDescription?: string;
  supervisorRequestId: string;
}
```

#### Проверка итерации 5
- [ ] Студент создаёт заявку → статус `Pending`
- [ ] Преподаватель одобряет → статус `ApprovedBySupervisor`
- [ ] Преподаватель передаёт заведующему → `PendingDepartmentHead`
- [ ] Заведующий утверждает → `ApprovedByDepartmentHead` (зелёный badge)
- [ ] Отклонение с пустым комментарием → форма не отправляется

---

### Итерация 6 — «Чат»

**Цель:** встроенный чат в `ApplicationDetailComponent` с автоматическим polling.

#### Компонент

`ChatWindowComponent` — встраивается в `ApplicationDetailComponent` через signal-based `input()`:

```typescript
@Component({ ... })
export class ChatWindowComponent {
  // Signal-based input (Angular 17+) — без @Input() декоратора
  readonly applicationId = input.required<string>();
  readonly applicationStatus = input.required<ApplicationStatusCode>();

  readonly isChatActive = computed(() =>
    CHAT_ACTIVE_STATUSES.includes(this.applicationStatus())
  );
}
```

- `@if (isChatActive())` — показываем чат с инпутом; иначе — read-only история с плашкой «Чат закрыт»
- Список сообщений (пузырьки: свои справа, чужие слева)
- Инпут для отправки (`p-inputText` + `p-button`; `Ctrl+Enter` через `(keydown)`)
- Polling сообщений с интервалом 5 секунд

#### Логика polling

```typescript
// В ChatWindowComponent
private readonly destroyRef = inject(DestroyRef);

ngOnInit(): void {
  this.loadInitialMessages();
  // Автоматически останавливается при уничтожении компонента
  timer(5_000, 5_000).pipe(
    switchMap(() => this.chatService.getMessages(this.applicationId())),
    takeUntilDestroyed(this.destroyRef)
  ).subscribe(messages => this.messages.set(messages));
}

onChatOpened(): void {
  this.chatService.markAllAsRead(this.applicationId()).subscribe();
}
```

#### Сервис

```typescript
@Injectable({ providedIn: 'root' })
export class ChatApiService {
  getMessages(applicationId: string, params?: { afterId?: string }): Observable<ChatMessageDto[]>
  sendMessage(applicationId: string, content: string): Observable<ChatMessageDto>
  markAllAsRead(applicationId: string): Observable<void>
}

interface ChatMessageDto {
  id: string;
  applicationId: string;
  senderId: string;
  senderFullName: string;
  content: string;
  sentAt: string;    // ISO 8601
  readAt?: string;
}
```

#### Бизнес-правила на UI

| Правило | Реализация |
|---------|-----------|
| Чат доступен только при `Pending`, `ApprovedBySupervisor`, `PendingDepartmentHead` | `@if (isChatActive())` (computed signal) |
| При финальном статусе — read-only история | Показывать список без инпута и с плашкой «Чат закрыт» |
| Отметка прочитанным при открытии | `markAllAsRead` в `ngOnInit` / при фокусе на инпуте |
| Ограничение длины сообщения 4000 символов | `Validators.maxLength(4000)` + счётчик символов |

#### Проверка итерации 6
- [ ] Сообщения загружаются и обновляются автоматически без перезагрузки страницы
- [ ] Polling останавливается при уходе со страницы (нет лишних запросов)
- [ ] Ввод отключён при терминальном статусе заявки
- [ ] Счётчик символов отображается; кнопка «Отправить» задизейблена при пустом тексте

---

### Итерация 7 — «Inbox уведомлений»

**Цель:** страница уведомлений + обновление badge в шапке.

#### Страница

**`/notifications`** → `NotificationsListComponent`
- Список уведомлений: иконка по типу, заголовок, краткое содержание, дата, индикатор прочитано/нет
- Непрочитанные выделяются (фон/жирность)
- Клик на уведомление → `markAsRead` + (опционально) переход к связанной сущности
- Кнопка «Отметить все прочитанными» (`p-button`)
- Фильтр: все / только непрочитанные (`p-selectButton`)
- Пагинация (`p-table` или `p-paginator`)

#### Иконки по типу

```typescript
// PrimeIcons (классы)
export const NOTIFICATION_ICONS: Record<string, string> = {
  'ApplicationStatusChanged':       'pi pi-check-circle',
  'SupervisorRequestStatusChanged': 'pi pi-user',
  'NewMessage':                     'pi pi-comments',
  'GraduateWorkUploaded':           'pi pi-upload',
};
```

#### Связь с badge

`NotificationsListComponent` при загрузке вызывает `notificationBadgeService.reset()` при открытии страницы и обновляет через `markAllAsRead`.

#### Проверка итерации 7
- [ ] Список уведомлений загружается
- [ ] Кнопка «Отметить все» → все становятся прочитанными, badge сбрасывается
- [ ] Непрочитанные визуально выделены
- [ ] Фильтр «только непрочитанные» работает

---

### Итерация 8 — «Архив ВКР»

**Цель:** просмотр и скачивание архивных работ для всех ролей (кроме Admin-управления — следующая итерация).

#### Страницы

**`/graduate-works`** → `GraduateWorksListComponent`
- Таблица `p-table`: название, студент, преподаватель, год, оценка, есть ли файл/презентация; встроенная пагинация
- Фильтры: по году (`p-select`), по преподавателю (`p-autoComplete`), поиск по названию (`p-iconField`)

**`/graduate-works/:id`** → `GraduateWorkDetailComponent`
- Полные метаданные: тема, студент, преподаватель, год, оценка, состав комиссии
- Кнопки «Скачать ВКР» / «Скачать презентацию» (только если файл есть)
- Скачивание: GET `/api/v1/graduate-works/{id}/download-url/thesis` → открыть `url` в новой вкладке

#### Сервис

```typescript
@Injectable({ providedIn: 'root' })
export class GraduateWorksApiService {
  getAll(params: GwFilter): Observable<PagedResult<GraduateWorkDto>>
  getById(id: string): Observable<GraduateWorkDto>
  getDownloadUrl(id: string, fileType: 'thesis' | 'presentation'): Observable<FileUrlDto>
}

interface GraduateWorkDto {
  id: string;
  title: string;
  year: number;
  grade: number;
  student: { id: string; fullName: string; };
  teacher: { id: string; fullName: string; };
  commissionMembers: string;
  hasThesis: boolean;
  hasPresentation: boolean;
  createdAt: string;
}

interface FileUrlDto {
  url: string;
  expiresAt: string;
}
```

#### Проверка итерации 8
- [ ] Список ВКР загружается с пагинацией
- [ ] Фильтры работают
- [ ] Клик «Скачать ВКР» → браузер открывает presigned URL

---

### Итерация 9 — «Панель администратора»

**Цель:** полный инструментарий Admin-а: управление пользователями, загрузка ВКР, аналитика, экспорт.

#### 9.1 Управление пользователями

**`/admin/users`** → `AdminUsersListComponent`
- Таблица `p-table`: email, ФИО, роль, активен/не активен, дата создания; встроенная пагинация
- Фильтры: по роли (`p-select`), поиск по email/ФИО (`p-iconField`)
- Кнопка «Создать пользователя» → открывает `CreateUserDialogComponent`

**`CreateUserDialogComponent`** (`p-dialog`):
- Поля: email (`p-inputText`), пароль (`p-password`), имя/фамилия/отчество, роль (`p-select`), кафедра (`p-select`, опционально)
- POST `/api/v1/users`
- Валидация: email формат, пароль мин. 8 символов, роль обязательна

#### 9.2 Управление архивом ВКР

**`/admin/graduate-works`** → `AdminGwListComponent`
- Таблица `p-table`: название, студент, год, есть ли файлы
- Кнопки (`p-button`): «Добавить», «Загрузить файл», «Удалить»

**Поток загрузки файла ВКР (3 шага):**

```
1. Admin нажимает «Загрузить файл» → выбирает файл в input[type=file]
2. POST /api/v1/graduate-works/{id}/upload-url/thesis → { url, expiresAt }
3. PUT <presignedUrl> (прямой запрос к S3/MinIO, не через бэкенд)
   — заголовок Content-Type: application/octet-stream
   — прогресс-бар через HttpRequest reportProgress
4. POST /api/v1/graduate-works/{id}/confirm-upload/thesis → success
```

Компонент `UploadGwFileComponent`:
```typescript
upload(file: File, type: 'thesis' | 'presentation'): Observable<void> {
  return this.gwService.getUploadUrl(this.gwId, type).pipe(
    switchMap(({ url }) => this.http.put(url, file, {
      reportProgress: true,
      observe: 'events',
      headers: { 'Content-Type': 'application/octet-stream' }
    })),
    filter(event => event.type === HttpEventType.Response),
    switchMap(() => this.gwService.confirmUpload(this.gwId, type))
  );
}
```

#### 9.3 Аналитика

**`/admin/analytics`** → `AdminAnalyticsComponent`

> ⚠️ Backend endpoint для аналитики ещё не реализован. Страница — заглушка с UI-структурой; данные подключаются по мере готовности backend.

Планируемые метрики:
- Количество заявок по статусам (`p-chart` — PrimeNG встроенная обёртка над Chart.js)
- Статистика по кафедрам
- Количество ВКР в архиве по годам

#### 9.4 Экспорт

**`/admin/export`** → `AdminExportComponent`

> ⚠️ Backend endpoint для экспорта ещё не реализован. Страница — заглушка.

Планируемое: кнопки «Экспорт в Excel» / «Экспорт в CSV» → GET `/api/v1/admin/export?format=excel`.

#### Проверка итерации 9
- [ ] Admin создаёт пользователя → пользователь появляется в списке
- [ ] Admin создаёт запись ВКР и загружает файл → в detail есть кнопка скачивания
- [ ] Admin удаляет ВКР → подтверждение через `ConfirmationService.confirm()`
- [ ] Не-Admin не может попасть в `/admin/*` маршруты (roleGuard)

---

## 6) Тестирование

### Unit-тесты (Jasmine + Karma)

Проект использует стандартный Angular-стек: **Jasmine** (assertion framework) + **Karma** (test runner), интегрированный через `@angular/build:karma`.

Покрывать в первую очередь: сервисы, guards и interceptors (бизнес-логика).

| Что тестировать | Примеры кейсов |
|----------------|----------------|
| `AuthService` | `login()` сохраняет токен → `isLoggedIn()` = true; `logout()` очищает всё |
| `authGuard` | без токена → `UrlTree('/login')`; с токеном → `true` |
| `roleGuard` | роль не совпадает → `UrlTree('/')`; совпадает → `true` |
| `error.interceptor` | 401 → вызывает `refresh()`, повторяет запрос; повторная 401 → редирект |
| `StatusBadgePipe` / `StatusLabelPipe` | `'Pending'` → `'Ожидает ответа'` |
| `NotificationBadgeService` | polling вызывает HTTP каждые 30 секунд |

Помимо юнит-тестов, создаются **интеграционные тесты** (файлы `*.integration.spec.ts`), проверяющие совместную работу:
- `interceptors.integration.spec.ts` — все три интерцептора + реальный `AuthService`
- `login.component.integration.spec.ts` — `LoginComponent` + реальный `AuthService` + DOM

**Запуск:**
```bash
ng test                                # watch-режим (авто-перезапуск при изменениях)
ng test --no-watch --code-coverage     # один прогон + отчёт покрытия
```

### E2E-тесты (Playwright)

Критические пользовательские сценарии:

| Сценарий | Файл |
|----------|------|
| Login → home по роли | `e2e/auth.spec.ts` |
| Student → запрос научруку → Teacher одобряет → статус обновился | `e2e/supervisor-requests.spec.ts` |
| Student → подать заявку → Teacher одобряет → DepartmentHead утверждает | `e2e/applications.spec.ts` |
| Чат: отправить сообщение → второй участник видит его | `e2e/chat.spec.ts` |
| Admin → создать пользователя → пользователь в списке | `e2e/admin.spec.ts` |

**Запуск:**
```bash
npx playwright test                      # headless, все браузеры
npx playwright test --ui                 # интерактивный UI-режим
npx playwright test --project=chromium   # только Chromium
```

---

## 7) Definition of Done (чек-лист)

- [ ] `ng build --configuration=production` без ошибок
- [ ] Все 4 роли могут войти и видят корректное меню
- [ ] Полный поток: запрос → одобрение → заявка → финальное утверждение проходит без перезагрузки страниц
- [ ] Чат работает без WebSocket (polling), polling останавливается при уходе со страницы
- [ ] Архив ВКР: скачивание через presigned URL открывается в браузере
- [ ] Уведомления: badge обновляется, inbox работает, «прочитать все» работает
- [ ] Admin: создание пользователя + загрузка ВКР (3-шаговый S3 flow)
- [ ] Все 401 → авто-refresh; повторная 401 → logout
- [ ] 403, 429, 5xx → информативное сообщение пользователю (`p-toast`)
- [ ] Unit-тесты ключевых сервисов и guards — зелёные
- [ ] E2E — критические сценарии — зелёные

---

## 8) Быстрый старт (команды)

```bash
# 1. Создать проект
ng new frontend --standalone --routing --style=scss
cd frontend

# 2. Установить PrimeNG + PrimeIcons + PrimeFlex
npm install primeng @primeng/themes primeicons primeflex

# 3. Unit-тесты (Jasmine + Karma — уже включены в Angular из коробки)
# Никаких дополнительных установок не нужно.

# 4. Установить Playwright (E2E)
npm install --save-dev @playwright/test
npx playwright install   # скачать браузеры (Chromium, Firefox, WebKit)

# 5. Запустить backend (из корня репозитория)
cd ../infra/docker && docker compose -f compose.backend.yml up -d

# 6. Запустить frontend dev-сервер
ng serve                              # http://localhost:4200

# 7. Тесты
ng test --no-watch --code-coverage    # unit (Jasmine + Karma)
npx playwright test --ui              # e2e (интерактивный)
npx playwright test                   # e2e (headless, CI)
```

---

*При существенных изменениях в backend API обновляйте интерфейсы в `core/models/` и сверяйтесь с актуальным Swagger (`http://localhost:5001/swagger`).*
