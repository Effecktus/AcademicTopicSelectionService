# План разработки Frontend (Angular 18) для проекта «AcademicTopicSelectionService»

Документ — **frontend-ориентированный план**, составленный на базе `DevelopmentPlan.md`, `BackendDevelopmentPlan.md` и актуального состояния backend API.

Обновлено: **2026-04-20**.

---

## 0) Цель и границы Frontend

### Цель
Реализовать Angular 18 SPA для сервиса выбора научного руководителя и темы ВКР с ролевой навигацией, полным покрытием всех бизнес-потоков backend-а и удобным UX для каждой роли.

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
| UI-библиотека | **PrimeNG 18** | Богатый набор компонентов из коробки: `p-table` с пагинацией/фильтрацией, `p-fileUpload`, `p-chart`, `p-toast`, `p-dialog`, `p-confirmDialog`; не нужно собирать из кусочков, как в Angular Material |
| State management | **Angular Signals** | Встроен в Angular 18 (`signal`, `computed`, `effect`, `toSignal`), не требует внешних зависимостей, проще NgRx при данном масштабе |
| Архитектура компонентов | **Standalone Components** | Рекомендация Angular 17+, нет NgModule-бойлерплейта, удобный lazy loading через `loadComponent` |
| Реактивность HTTP | **RxJS** (`HttpClient`) | Стандарт для Angular, удобен с `switchMap`, `takeUntilDestroyed`, `retry` |
| Формы | **Reactive Forms** (`FormBuilder`) | Удобны для валидации, программного управления и тестирования |
| Polling чата | **RxJS `timer(0, 5000).pipe(switchMap, takeUntilDestroyed)`** | Простой механизм, авто-отмена при уничтожении компонента |
| Декораторы компонентов | **Signal-based inputs/outputs** (`input()`, `output()`, `viewChild()`) | Стандарт Angular 17+; старые `@Input()`/`@Output()` работают, но этот подход современнее |
| Control flow в шаблонах | **Built-in `@if` / `@for` / `@switch`** | Встроен в Angular 17+; `*ngIf`/`*ngFor` устарели |
| Unit-тесты | **Jest** + `jest-preset-angular` | Быстрее Karma (которая удалена в Angular 17), параллельное выполнение |
| E2E-тесты | **Playwright** | В 2026 году активно вытесняет Cypress; мультибраузерность из коробки, быстрее, поддерживается Microsoft |
| Стили | **SCSS** + PrimeNG theming (Aura preset) | `providePrimeNG({ theme: { preset: Aura } })`, кастомизация через CSS-переменные |
| Хранение refresh-токена | **httpOnly cookie** (рекомендуется) | Недоступен из JavaScript → защита от XSS; backend устанавливает через `Set-Cookie` |

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
│   │   │   │   ├── auth.interceptor.ts    # Добавляет Authorization: Bearer <token>
│   │   │   │   └── error.interceptor.ts   # 401 → refresh → retry; 403/429/5xx → snackbar
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
│   │   └── app.config.ts                  # provideRouter, provideHttpClient, provideAnimations, providePrimeNG
│   │
│   ├── environments/
│   │   ├── environment.ts                 # apiUrl: 'http://localhost:5001'
│   │   └── environment.prod.ts            # apiUrl: '/api' (nginx proxy)
│   └── styles/
│       ├── _variables.scss                # Цвета, отступы, типографика
│       ├── _theme.scss                    # PrimeNG CSS-переменные / override
│       └── styles.scss
├── jest.config.ts
├── playwright.config.ts
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
| Авто-обновление токена | `error.interceptor.ts`: при 401 → `authService.refresh()` → повтор оригинального запроса через `switchMap` |
| Глобальная обработка ошибок | `error.interceptor.ts`: 403/429/5xx → `MessageService` (PrimeNG `p-toast`) с соответствующим сообщением |
| CORS | Backend разрешает `http://localhost:4200` в `Development`; в prod — nginx proxy |
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
interface LoginRequest { email: string; password: string; }
interface LoginResponse { accessToken: string; refreshToken: string; }
interface RefreshRequest { refreshToken: string; }
interface UserInfo {
  id: string; email: string; role: string;
  firstName: string; lastName: string; middleName?: string;
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

# PrimeNG 18 + темы (Aura, Lara, Nora)
npm install primeng @primeng/themes primeicons

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

**0.2 Настроить environments**

```typescript
// environment.ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5001/api/v1'
};
```

**0.3 app.config.ts**

```typescript
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(appRoutes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: Aura,
        options: { darkModeSelector: '.dark-mode' }
      },
      ripple: true,
    }),
    MessageService,       // PrimeNG Toast — singleton
    ConfirmationService,  // PrimeNG ConfirmDialog — singleton
  ]
};
```

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
  restoreSession(): void                   // вызывается в APP_INITIALIZER
  getAccessToken(): string | null          // используется в interceptor
}
```

**0.5 auth.interceptor.ts**

```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getAccessToken();
  if (token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req);
};
```

**0.6 error.interceptor.ts** — при 401 пробует обновить токен, затем повторяет запрос; при повторной 401 — редиректит на `/login`.

**0.7 AuthGuard и RoleGuard**

```typescript
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? true : router.createUrlTree(['/login']);
};

export const roleGuard: CanActivateFn = (route) => {
  const requiredRole: string = route.data['role'];
  const auth = inject(AuthService);
  return auth.role() === requiredRole ? true : router.createUrlTree(['/']);
};
```

**0.8 app.routes.ts (скелет)**

```typescript
export const appRoutes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/login/login.component') },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'topics', pathMatch: 'full' },
      // Lazy routes добавляются в последующих итерациях
    ]
  }
];
```

**0.9 Layouts**
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

#### Компоненты

**`LoginComponent`** (внутри `AuthLayoutComponent`):
- Reactive Form: `email` (required, email), `password` (required, minlength 8)
- Кнопка «Войти» — задизейблена во время запроса (`isLoading` signal)
- При успехе: сохранить токены → определить роль → `router.navigate(['/'])` (хук `APP_INITIALIZER` загружает `UserInfo` из токена)
- При ошибках:
  - `400 / 401` → показать "Неверный email или пароль" под формой (не snackbar — это критичная ошибка)
  - `429` → показать "Слишком много попыток входа. Подождите несколько минут."
  - `5xx` → "Сервис временно недоступен"

#### API
- `POST /api/v1/auth/login` → `LoginResponse { accessToken, refreshToken }`

#### TypeScript
```typescript
interface LoginForm {
  email: FormControl<string>;
  password: FormControl<string>;
}
```

#### Проверка итерации 1
- [ ] Успешный вход → редирект на домашнюю страницу роли
- [ ] Неверный пароль → ошибка под формой, форма не заблокирована
- [ ] Кнопка задизейблена при отправке
- [ ] После входа как Admin → редирект на `/admin/users`
- [ ] Обновление страницы → браузер автоматически отправляет httpOnly cookie → `restoreSession()` восстанавливает сессию

---

### Итерация 2 — «Главный layout + ролевая навигация + badge уведомлений»

**Цель:** навигационный каркас для всех последующих страниц.

#### Компоненты

**`MainLayoutComponent`**:
- Боковая панель (div.sidebar + SCSS): список ссылок из `nav-items.ts`, фильтруется по `authService.role()`; можно использовать `p-panelMenu` или просто `routerLink`-ссылки
- Шапка (div.topbar): название приложения, имя пользователя, иконка уведомлений (`pi pi-bell`) с `p-badge`, кнопка «Выйти»
- Logout: `POST /api/v1/auth/logout` → очистить access token → `/login`

**`nav-items.ts`** — константа (иконки — PrimeIcons):

```typescript
export const NAV_ITEMS: NavItem[] = [
  { label: 'Преподаватели', icon: 'pi pi-users',      route: '/teachers',            roles: ['Student'] },
  { label: 'Темы',          icon: 'pi pi-book',        route: '/topics',              roles: ['Student', 'Teacher'] },
  { label: 'Мои запросы',   icon: 'pi pi-send',        route: '/supervisor-requests', roles: ['Student'] },
  { label: 'Мои заявки',    icon: 'pi pi-file-edit',   route: '/applications',        roles: ['Student'] },
  { label: 'Запросы (вх.)', icon: 'pi pi-inbox',       route: '/supervisor-requests', roles: ['Teacher'] },
  { label: 'Заявки',        icon: 'pi pi-file-edit',   route: '/applications',        roles: ['Teacher', 'DepartmentHead'] },
  { label: 'Архив ВКР',     icon: 'pi pi-server',      route: '/graduate-works',      roles: ['Student', 'Teacher', 'DepartmentHead'] },
  { label: 'Уведомления',   icon: 'pi pi-bell',        route: '/notifications',       roles: ['Student', 'Teacher', 'DepartmentHead'] },
  { label: 'Пользователи',  icon: 'pi pi-user-edit',   route: '/admin/users',         roles: ['Admin'] },
  { label: 'Архив ВКР',     icon: 'pi pi-server',      route: '/admin/graduate-works',roles: ['Admin'] },
  { label: 'Аналитика',     icon: 'pi pi-chart-bar',   route: '/admin/analytics',     roles: ['Admin'] },
  { label: 'Экспорт',       icon: 'pi pi-download',    route: '/admin/export',        roles: ['Admin'] },
];
```

**`NotificationBadgeService`** (singleton):
```typescript
@Injectable({ providedIn: 'root' })
export class NotificationBadgeService {
  readonly unreadCount = signal(0);

  startPolling(): void {
    // Запускать только для ролей, у которых есть уведомления (не Admin)
    timer(0, 30_000)
      .pipe(switchMap(() => this.http.get<PagedResult<any>>('.../notifications?isRead=false&pageSize=1')))
      .subscribe(result => this.unreadCount.set(result.total));
  }

  decrement(): void { ... }  // вызывать при отметке прочитанным в inbox
  reset(): void { ... }
}
```

#### Проверка итерации 2
- [ ] Меню содержит только пункты, доступные текущей роли
- [ ] После logout токены очищены, редирект на `/login`
- [ ] Badge появляется/исчезает при изменении `unreadCount`

---

### Итерация 3 — «Каталог преподавателей и тем»

**Цель:** read-only просмотр для всех ролей; управление темами — для `Teacher`.

#### Страницы и компоненты

**`/teachers`** → `TeachersListComponent`
- Таблица `p-table`: ФИО, степень, звание, должность, лимит студентов; встроенная пагинация (`[paginator]="true"`)
- Поиск: `p-iconField` + `p-inputText` с debounce 300ms через `FormControl.valueChanges`
- Клик на строку → `/teachers/:id`

**`/teachers/:id`** → `TeacherDetailComponent`
- Профиль преподавателя
- Его темы (вложенная таблица)
- Кнопка «Запросить в научруки» (только `Student`, перенаправляет к форме создания `SupervisorRequest`)

**`/topics`** → `TopicsListComponent`
- Фильтры: по статусу (`p-select`), по преподавателю (`p-autoComplete`)
- Поиск по названию: `p-iconField` + `p-inputText` с debounce
- Таблица `p-table` со встроенной пагинацией
- Для `Teacher` — кнопка «Добавить тему» (`p-button`)

**`/topics/:id`** → `TopicDetailComponent`
- Детали темы
- Для `Student` — кнопка «Подать заявку» (только если тема `Active` и у студента есть одобренный `SupervisorRequest`)

**`/topics/new`**, **`/topics/:id/edit`** → `TopicFormComponent` (только `Teacher`)
- Поля: название, описание
- PATCH / PUT / DELETE

#### Сервисы

```typescript
@Injectable({ providedIn: 'root' })
export class TeachersApiService {
  getTeachers(params: { query?: string; page: number; pageSize: number }): Observable<PagedResult<TeacherDto>>
  getTeacherById(id: string): Observable<TeacherDto>
}

@Injectable({ providedIn: 'root' })
export class TopicsApiService {
  getTopics(params: TopicsFilter): Observable<PagedResult<TopicDto>>
  getTopicById(id: string): Observable<TopicDto>
  createTopic(command: CreateTopicCommand): Observable<TopicDto>
  updateTopic(id: string, command: UpdateTopicCommand): Observable<TopicDto>
  patchTopic(id: string, patch: Partial<UpdateTopicCommand>): Observable<TopicDto>
  deleteTopic(id: string): Observable<void>
}

interface TopicsFilter {
  query?: string;
  statusCodeName?: string;
  createdByUserId?: string;
  page: number;
  pageSize: number;
}
```

#### Проверка итерации 3
- [ ] Список преподавателей загружается, работает поиск и пагинация
- [ ] Список тем фильтруется по статусу и преподавателю
- [ ] Teacher видит кнопки редактирования/удаления, Student — нет
- [ ] Удаление темы — через `ConfirmationService.confirm()` (PrimeNG `p-confirmDialog`)

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

### Unit-тесты (Jest)

Покрывать в первую очередь: сервисы и guards (бизнес-логика, не компоненты).

| Что тестировать | Примеры кейсов |
|----------------|----------------|
| `AuthService` | `login()` сохраняет токен → `isLoggedIn()` = true; `logout()` очищает всё |
| `authGuard` | без токена → `UrlTree('/login')`; с токеном → `true` |
| `roleGuard` | роль не совпадает → `UrlTree('/')`; совпадает → `true` |
| `error.interceptor` | 401 → вызывает `refresh()`, повторяет запрос; повторная 401 → редирект |
| `StatusBadgePipe` / `StatusLabelPipe` | `'Pending'` → `'Ожидает ответа'` |
| `NotificationBadgeService` | polling вызывает HTTP каждые 30 секунд |

**Запуск:**
```bash
npx jest --coverage
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
ng new academic-topic-selection-frontend --standalone --routing --style=scss
cd academic-topic-selection-frontend

# 2. Установить PrimeNG + PrimeIcons + PrimeFlex
npm install primeng @primeng/themes primeicons primeflex

# 3. Установить Jest (вместо Karma, которая удалена в Angular 17)
npm install --save-dev jest jest-preset-angular @types/jest
# Создать jest.config.ts с preset: 'jest-preset-angular'

# 4. Установить Playwright
npm install --save-dev @playwright/test
npx playwright install   # скачать браузеры (Chromium, Firefox, WebKit)

# 5. Запустить backend (из корня репозитория)
cd ../infra/docker && docker compose -f compose.backend.yml up -d

# 6. Запустить frontend dev-сервер
ng serve                              # http://localhost:4200

# 7. Тесты
npx jest --coverage                   # unit
npx playwright test --ui              # e2e (интерактивный)
npx playwright test                   # e2e (headless, CI)
```

---

*При существенных изменениях в backend API обновляйте интерфейсы в `core/models/` и сверяйтесь с актуальным Swagger (`http://localhost:5001/swagger`).*
