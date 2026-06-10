# ОПИСАНИЕ ПРОЕКТА AcademicTopicSelectionService

## Что делает система

**AcademicTopicSelectionService** — веб-система управления выбором научных руководителей и тем ВКР в учебном заведении. Охватывает полный жизненный цикл: от первого запроса студента к преподавателю до загрузки защищённой работы в архив.

Система **не занимается** написанием ВКР — после финального утверждения тема считается закреплённой, работа над ней ведётся вне системы. После защиты администратор вручную загружает работу в архив.

---

## Роли пользователей

| Роль | Описание |
|------|---------|
| **Student** | Ищет преподавателей и темы, подаёт запрос на руководство, затем заявку на тему, общается в чате |
| **Teacher** | Управляет своими темами, рассматривает входящие запросы на руководство и заявки студентов, общается в чате |
| **DepartmentHead** | Финально одобряет или отклоняет заявки своей кафедры |
| **Admin** | Создаёт пользователей, загружает ВКР в архив, смотрит аналитику и делает экспорт |

Публичной регистрации нет. Учётные записи создаёт только Admin через `POST /api/v1/users`. Вход — email + пароль.

---

## Два основных потока + архив

```
ПОТОК 1: Выбор научного руководителя (SupervisorRequest)
ПОТОК 2: Утверждение темы ВКР (StudentApplication)  ← требует завершённого Потока 1
АРХИВ:   Загрузка защищённой работы (GraduateWork)   ← Admin, после защиты вне системы
```

---

## ПОТОК 1: Выбор научного руководителя (SupervisorRequest)

### Суть
Студент выбирает, к какому преподавателю хочет пойти как к научруку. Отправляет запрос — преподаватель принимает или отклоняет. После принятия студент может перейти к Потоку 2.

### Жизненный цикл

```
Student POST /supervisor-requests
             │
             ▼
         [Pending]
        /    |    \
  approve  reject  cancel (Student)
    │        │
    ▼        ▼
[ApprovedBySupervisor]  [RejectedBySupervisor]
[Cancelled — остальные запросы студента]
```

### Бизнес-правила

1. **Несколько одновременных запросов** — студент может отправить запросы нескольким преподавателям одновременно, но не более одного активного запроса на одного и того же преподавателя (дубль запрещён).

2. **Лимит количества запросов** — максимальное количество активных запросов = число преподавателей на кафедре студента. Если кафедра не определена — ограничение не применяется.

3. **Атомарная отмена** — когда преподаватель одобряет запрос, все остальные активные запросы этого же студента к другим преподавателям **автоматически отменяются** (`Cancelled`) в той же транзакции. Студент не может иметь двух одобренных научруков.

4. **Отклонение** — требует обязательного комментария.

5. **Отмена студентом** — возможна только из статуса `Pending`.

### Видимость в списке
- Student видит только свои запросы
- Teacher видит только входящие запросы к нему
- Admin видит все

### Переходы, доступные по ролям
- `approve` — только Teacher (если `TeacherUserId` == текущему пользователю)
- `reject` — только Teacher
- `cancel` — только Student (владелец запроса)

---

## ПОТОК 2: Утверждение темы ВКР (StudentApplication)

### Суть
Студент с одобренным запросом на руководство подаёт заявку на конкретную тему (созданную преподавателем) или предлагает свою тему. Заявка проходит через двух одобряющих: сначала научрук, затем заведующий кафедрой.

### Жизненный цикл

```
Student POST /applications (topicId или proposedTitle)
                  │
                  ▼
            [Pending]
           /         \
      approve        reject (Supervisor)
(Supervisor)              │
      │               [RejectedBySupervisor]
      ▼
[PendingDepartmentHead]
       /          \
  dept-approve  dept-reject
       │              │
       ▼              ▼
[ApprovedByDepartmentHead]  [RejectedByDepartmentHead]
        ФИНАЛ ✓

Student может cancel из: Pending, ApprovedBySupervisor
НЕЛЬЗЯ cancel из: PendingDepartmentHead и позже
```

**Важно**: отдельного HTTP-вызова «передать заведующему» нет. `approve` от научрука сразу переводит заявку в `PendingDepartmentHead`.

### Терминальные статусы (изменений больше нет)
- `ApprovedByDepartmentHead` — тема закреплена, процесс завершён
- `RejectedBySupervisor` — научрук отказал
- `RejectedByDepartmentHead` — завкаф отказал
- `Cancelled` — студент отменил

### Ключевые бизнес-правила

1. **Обязательный SupervisorRequest** — при создании заявки студент передаёт `SupervisorRequestId`. Запрос должен быть в статусе `ApprovedBySupervisor`. Научрук в потоке 2 определяется через `SupervisorRequest.TeacherUserId`, а не через `Topic.CreatedBy`.

2. **Уникальность заявки на тему** — на одну тему может быть только одна активная заявка. «Первый взял — остальные не могут» (проверка `HasActiveApplicationOnTopicAsync`).

3. **Статус темы** — тема должна быть `Active` при подаче заявки.

4. **Предложение собственной темы** — студент может предложить свою тему прямо при создании заявки (поля `proposedTitle`, `proposedDescription`). В этом случае создаётся топик с `CreatorTypeId = "Student"`.

5. **Лимит студентов преподавателя** (`MaxStudentsLimit`) — проверяется при `department-head-approve`: считаются заявки со статусом `ApprovedByDepartmentHead` у данного научрука.

6. **Кафедра заведующего** — DepartmentHead определяется как пользователь с ролью `DepartmentHead`, чей `DepartmentId` совпадает с `DepartmentId` научрука (через `SupervisorRequest.TeacherUserId → Users.DepartmentId`).

7. **Reject требует комментария** — для `reject` и `department-head-reject` комментарий обязателен.

### Видимость в списке
- Student видит свои заявки
- Teacher видит заявки, где он является научруком (через `SupervisorRequest.TeacherUserId`)
- DepartmentHead видит заявки кафедры (только те, что на стадии `PendingDepartmentHead`)
- Admin видит все

### ApplicationActions — журнал истории заявки
Каждый переход статуса создаёт запись `ApplicationAction` с полями: `ApplicationId`, `ResponsibleId` (кто сделал), `NewStatusId`, `Comment`, `Kind`, `CreatedAt`. Видна в деталях заявки как история действий.

---

## ЧАТ (ApplicationChatMessages)

### Суть
Переписка между студентом (владельцем заявки) и преподавателем (научруком из `SupervisorRequest`) для обсуждения темы. Реализован через REST + polling каждые 5 секунд.

### Кто участвует
Ровно два участника: Student (владелец заявки) и Teacher (из `SupervisorRequest`). Все остальные роли — без доступа к чату.

### Когда доступен
Чат открыт, пока заявка в статусах: `Pending`, `ApprovedBySupervisor`, `PendingDepartmentHead`.  
После `ApprovedByDepartmentHead` или `RejectedByDepartmentHead` — чат **закрывается**: новые сообщения отправлять нельзя, история доступна для чтения.

### Правила
- Сообщение не может быть пустым или длиннее 4000 символов
- `ReadAt` — бинарный флаг, работает корректно т.к. в чате ровно 2 участника
- `read-all` помечает все входящие непрочитанные сообщения в чате этой заявки одним запросом
- `read-all` также автоматически помечает прочитанными `NewMessage`-уведомления для этой заявки у текущего пользователя
- Polling: клиент запрашивает с `afterId` для инкрементальной подгрузки; WebSocket не используется

---

## АРХИВ ВКР (GraduateWork)

### Суть
Архив успешно защищённых работ прошлых лет. Загружается **вручную Admin** после защиты — не автоматически. Просмотр и скачивание доступны всем авторизованным пользователям.

### Структура записи
- Привязана к `StudentApplication` (уникальная связь — одна заявка → максимум одна работа)
- Содержит: `Title`, `Year`, `Grade` (0–100), `CommissionMembers`, `FilePath` (ключ S3), `PresentationPath` (nullable)
- Студент и преподаватель берутся из связанной заявки

### Поток загрузки файла (Admin)
```
1. POST /graduate-works           — создать запись (без файла)
2. POST /graduate-works/{id}/upload-url/{fileType}   — получить presigned PUT URL (15 мин.)
3. PUT {presignedUrl} + файл      — загрузить файл напрямую в S3/MinIO (frontend → S3)
4. POST /graduate-works/{id}/confirm-upload/{fileType} — бэкенд проверяет ObjectExists, обновляет FilePath
```

`fileType` — строка `thesis` или `presentation`.

### Скачивание (любой авторизованный)
`GET /graduate-works/{id}/download-url/{fileType}` → presigned URL (15 мин.) → браузер скачивает напрямую из S3.

### Фильтры списка
`year`, `titleQuery`, `teacherId`, `teacherQuery`, `page`, `pageSize` (max 200).

### Статистика преподавателя
На странице преподавателя показывается: количество ВКР из архива, средняя оценка, темы прошлых лет, количество активных текущих заявок.

---

## УВЕДОМЛЕНИЯ (Notifications)

### Типы событий, создающих уведомления

| Событие | Кто получает | Тип |
|---------|-------------|-----|
| SupervisorRequest одобрен | Student | `SupervisorRequestStatusChanged` |
| SupervisorRequest отклонён | Student | `SupervisorRequestStatusChanged` |
| SupervisorRequest создан | Teacher | `SupervisorRequestCreated` |
| Заявка одобрена научруком | Student + DeptHead | `ApplicationStatusChanged` |
| Заявка отклонена научруком | Student | `ApplicationStatusChanged` |
| Заявка одобрена завкафом | Student | `ApplicationStatusChanged` |
| Заявка отклонена завкафом | Student | `ApplicationStatusChanged` |
| Новое сообщение в чате | Получатель | `NewMessage` |
| ВКР загружена в архив | Student | `GraduateWorkUploaded` |

### Механизм

**Inbox**: уведомление создаётся атомарно в той же транзакции БД, что и бизнес-действие. Мгновенно доступно через `GET /notifications`.

**Email**: запись `EmailTask` помещается в `Channel<T>` → `EmailBackgroundService` читает и отправляет асинхронно (не блокирует бизнес-операцию). В dev/тестах — `LogEmailSender` (пишет в лог). В проде — `SmtpEmailSender` (MailKit).

При ошибке отправки email — логируется и продолжается (письмо теряется; retry вне scope MVP).

### API Inbox
```
GET  /api/v1/notifications               # ?page, ?pageSize, ?isRead
PUT  /api/v1/notifications/{id}/read     # Отметить одно
PUT  /api/v1/notifications/read-all      # Отметить все
```

Ответ включает `relatedEntityType` и `relatedEntityId` для навигации во фронтенде.

---

## ТЕМЫ (Topics)

### Кто создаёт
- **Teacher** — основной способ: преподаватель заранее создаёт список тем
- **Student** — при создании заявки, если хочет предложить свою тему (`creatorType = "Student"`)

### Статусы
- `Active` — доступна для заявок
- `Archived` — недоступна для новых заявок

### Поля
`Title`, `Description`, `Year`, `TeacherId`, `StatusId`, `CreatorTypeId`.  
Уникальность: `(TeacherId, Year, Title)`.

### Фильтры списка
`query` (по Title), `creatorQuery` (по имени создателя), `statusCodeName`, `createdByUserId`, `creatorTypeCodeName`, `createdFromUtc`, `createdToUtc`, `sort`, `page`, `pageSize`.

---

## АНАЛИТИКА И ЭКСПОРТ

Доступны только Admin и DepartmentHead (частично).

### Admin: `GET /api/v1/admin/analytics`
Возвращает `AdminAnalyticsDto`:
- Заявки по статусам
- ВКР по годам
- Заявки по кафедрам

### Admin: `GET /api/v1/admin/export`
- `?format=excel` — три листа (ВКР, заявки, пользователи) в одном `.xlsx` (ClosedXML)
- `?format=csv&dataset={graduate-works|applications|users}` — один датасет в CSV

### DepartmentHead
Видит аналитику по своей кафедре (отдельный раздел в UI).

---

## УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ (Admin)

- `GET /api/v1/users` — список с фильтрами `roleId`, `query` (поиск по email/ФИО), пагинация
- `POST /api/v1/users` — создать пользователя

При создании: email нормализуется к нижнему регистру, проверяется уникальность. Пароль: минимум 8 символов, обязательны заглавная, строчная, цифра, спецсимвол (`@!#$%^&*`).

---

## АУТЕНТИФИКАЦИЯ И АВТОРИЗАЦИЯ

### JWT + Redis Refresh Token
1. `POST /auth/login` → в ответе `{ accessToken, userId, expiresIn }`, в `Set-Cookie` — httpOnly refresh-token (14+ дней)
2. Access Token (~15 мин.) — передаётся в `Authorization: Bearer`; stateless, не хранится на сервере
3. Refresh Token — хранится в Redis с ротацией; при каждом `/auth/refresh` старый отзывается, выдаётся новый
4. `POST /auth/logout` — отзывает refresh из Redis, очищает cookie
5. При 401 — фронт автоматически вызывает refresh и повторяет запрос (заголовок `X-Auth-Retry` предотвращает петлю)

### RBAC
- Fallback-политика: все эндпоинты требуют авторизации, кроме `/health`, `/auth/login`, `/auth/refresh`
- Policy-атрибуты на контроллерах (`[Authorize(Policy = "Admin")]` и т.д.)
- Resource-level: студент видит только свои заявки/запросы; teacher — только свои; и т.д.

### Rate Limiting
15 запросов/минуту на IP для `/auth/login` и `/auth/refresh`. В окружении `Testing` — ослабленные лимиты.

### Health Checks
- `GET /health` — анонимный smoke-test
- `GET /health/db` — проверка PostgreSQL; доступен только при JWT с ролью Admin **или** заголовке `X-Health-Probe-Key` с секретом из конфига `Health:DbProbeKey`

---

## ТЕХНОЛОГИЧЕСКИЙ СТЕК

```
┌───────────────────────────────────────────────────────┐
│                     FRONTEND                          │
│  Angular 20 · TypeScript 5.9 · PrimeNG 20 · SCSS     │
│  Angular Signals (state) · RxJS · Lazy routing       │
└───────────────────────────────────────────────────────┘
                         │ HTTP REST (JSON)
┌───────────────────────────────────────────────────────┐
│                      BACKEND                          │
│  ASP.NET Core 10 · C# 13 · Clean Architecture        │
│  JWT (HMAC) + BCrypt · EF Core 10 ORM · Swagger      │
│  Rate Limiting · API versioning (/api/v1) · MailKit  │
└────────┬──────────────────┬──────────────────┬───────┘
         │                  │                  │
   ┌─────▼──────┐   ┌───────▼──────┐  ┌───────▼──────┐
   │ PostgreSQL │   │    Redis     │  │  MinIO / S3  │
   │    16.8    │   │  7.4-alpine  │  │   (файлы)    │
   └────────────┘   └──────────────┘  └──────────────┘
```

| Компонент | Технология | Детали |
|-----------|-----------|--------|
| Backend framework | ASP.NET Core 10 | net10.0, C# 13 |
| ORM | EF Core 10 | Поверх готовой SQL-схемы, без EF-миграций |
| База данных | PostgreSQL 16.8 | citext (email), pgcrypto (UUID) |
| Кэш / сессии | Redis 7.4-alpine | Refresh-токены с ротацией |
| Файловое хранилище | MinIO (dev) / AWS S3 (prod) | Presigned URLs, AWSSDK.S3 |
| Email | MailKit | Async через Channel + BackgroundService |
| Экспорт | ClosedXML 0.104 | Excel (.xlsx) |
| Frontend framework | Angular 20 | Standalone Components, Signals |
| UI-библиотека | PrimeNG 20 | Aura preset, синяя тема |
| Реактивность | RxJS 7.8 | switchMap, takeUntilDestroyed |
| Unit-тесты (backend) | xUnit | Сотни кейсов |
| Интеграционные тесты | Testcontainers | PostgreSQL + Redis в Docker |
| Unit-тесты (frontend) | Jasmine + Karma | *.spec.ts рядом с компонентами |
| E2E-тесты | Playwright | Написаны, но вне scope MVP |
| Контейнеризация | Docker + Compose | Multi-stage Dockerfile |

---

## АРХИТЕКТУРА БЭКЕНДА (Clean Architecture)

### Правило зависимостей
```
Domain ← Application ← Infrastructure
                      ← API → Application, Infrastructure (только для DI)
```

### Проекты

```
backend/src/
├── AcademicTopicSelectionService.Domain/
│   ├── Common/IAuditableEntity.cs
│   └── Entities/                    # User, UserRole, Department, Student, Teacher,
│                                    # Topic, SupervisorRequest, StudentApplication,
│                                    # ApplicationAction, ChatMessage, GraduateWork,
│                                    # Notification, NotificationType, StudyGroup,
│                                    # AcademicDegree, AcademicTitle, Position,
│                                    # TopicStatus, ApplicationStatus, TopicCreatorType
│
├── AcademicTopicSelectionService.Application/
│   ├── Abstractions/                # Интерфейсы репозиториев, IFileStorageService,
│   │                                # IDatabaseHealthChecker, IEmailSender,
│   │                                # IEmailTaskChannel, IPasswordHasher
│   ├── Auth/                        # AuthService, CredentialValidation, UserAccountsService
│   ├── SupervisorRequests/          # SupervisorRequestsService, Contracts, ISupervisorRequestsService
│   ├── StudentApplications/         # StudentApplicationsService, Contracts
│   ├── ChatMessages/                # ChatMessagesService, Contracts
│   ├── GraduateWorks/               # GraduateWorksService, Contracts
│   ├── Notifications/               # NotificationsService, NotificationTypeCodes, Contracts
│   ├── ApplicationActions/          # ApplicationActionsService
│   ├── Topics/                      # TopicsService
│   ├── Teachers/                    # TeachersService
│   ├── Students/                    # StudentsService
│   ├── Admin/                       # AdminAnalyticsService, AdminExportService
│   ├── Dictionaries/                # CRUD-сервисы справочников
│   └── Security/CredentialValidation.cs  # Нормализация email, политика пароля
│
├── AcademicTopicSelectionService.Infrastructure/
│   ├── Data/ApplicationDbContext.cs       # EF Core, Fluent API, аудит (UpdatedAt)
│   ├── Auth/JwtTokenGenerator.cs          # Генерация JWT (HMAC)
│   ├── Auth/BcryptPasswordHasher.cs       # BCrypt
│   ├── Cache/RedisRefreshTokenCache.cs    # Хранение и ротация refresh-токенов
│   ├── Storage/S3FileStorageService.cs    # AWSSDK.S3, presigned URLs, ObjectExists
│   ├── Storage/DevelopmentFileStorageService.cs  # Заглушка для dev
│   ├── Email/SmtpEmailSender.cs           # MailKit, продакшн
│   ├── Email/LogEmailSender.cs            # Лог в консоль, dev/test
│   ├── Email/EmailTaskChannel.cs          # Channel<EmailTask> (Singleton)
│   ├── Email/EmailBackgroundService.cs    # BackgroundService, читает Channel
│   └── Repositories/                     # По одному репозиторию на агрегат
│
└── AcademicTopicSelectionService.API/
    ├── Controllers/                       # Все контроллеры, [Authorize]-атрибуты
    ├── Health/                            # HealthDbAccess (Admin JWT или X-Health-Probe-Key)
    ├── RateLimiting/                      # Политики rate limiting
    ├── Middleware/GlobalExceptionHandler.cs
    └── Program.cs                         # DI-регистрация, pipeline
```

### Ключевые детали Application-слоя

**AuthService**: логин (проверка email + BCrypt), генерация JWT, сохранение refresh в Redis, ротация при refresh, отзыв при logout.

**SupervisorRequestsService.ApproveAsync**: одобрение + **в той же транзакции** авто-отмена всех остальных `Pending`-запросов этого студента. Атомарно через EF.

**StudentApplicationsService.ApproveAsync**: смена статуса `Pending` → `PendingDepartmentHead` (без промежуточных шагов).

**ChatMessagesService**: проверка участника через `GetChatAccessAsync` (Student-владелец или Teacher из SupervisorRequest); проверка статуса заявки; после сохранения — `CreateAndSaveAsync` (уведомление) + `EnqueueEmail`.

**GraduateWorksService.ConfirmUploadAsync**: проверяет `ObjectExistsAsync` в S3, обновляет `FilePath`/`PresentationPath`, создаёт уведомление студенту.

**NotificationsService**: `CreateAsync` — добавляет в контекст (без `SaveChanges`, для атомарности с бизнес-действием); `CreateAndSaveAsync` — отдельный `SaveChanges` (для уведомлений после чата/файла, где коммит уже произошёл).

---

## СТРУКТУРА ФРОНТЕНДА

### Архитектурные решения

| Решение | Выбор |
|---------|-------|
| State | Angular Signals (`signal`, `computed`, `effect`, `toSignal`) |
| Компоненты | Standalone, без NgModule |
| Lazy loading | `loadComponent` в `app.routes.ts` |
| Шаблоны | Built-in `@if / @for / @switch` (Angular 17+) |
| Inputs/outputs | `input()`, `output()`, `viewChild()` — signal-based |
| HTTP | `HttpClient` + RxJS |
| Формы | Reactive Forms + `FormBuilder` |
| Polling | `timer(0, 5000).pipe(switchMap(...), takeUntilDestroyed())` |
| Авто-отмена | `takeUntilDestroyed()` в подписках |

### Структура директорий

```
frontend/src/app/
├── core/
│   ├── auth/
│   │   ├── auth.service.ts          # Signals: currentUser, isLoggedIn, userRole
│   │   │                            # Методы: login(), logout(), refresh()
│   │   ├── auth.guard.ts            # Редирект на /login если !isLoggedIn
│   │   └── role.guard.ts            # Редирект если role != route.data.role
│   ├── interceptors/
│   │   ├── credentials.interceptor.ts  # withCredentials: true для всех /api/ запросов
│   │   ├── auth.interceptor.ts         # Authorization: Bearer <token>
│   │   └── error.interceptor.ts        # 401→refresh→retry; 403/429/5xx→p-toast
│   └── models/                      # TypeScript-интерфейсы = API DTO
│       ├── common.models.ts          # PagedResult<T>, ProblemDetails
│       ├── auth.models.ts            # AccessTokenDto { accessToken, userId, expiresIn }
│       ├── teacher.models.ts
│       ├── topic.models.ts
│       ├── supervisor-request.models.ts
│       ├── application.models.ts
│       ├── chat.models.ts
│       ├── graduate-work.models.ts
│       └── notification.models.ts
│
├── shared/
│   ├── components/
│   │   ├── status-badge/            # p-tag с цветом по codeName
│   │   ├── reject-dialog/           # p-dialog + обязательный комментарий
│   │   ├── empty-state/             # «Ничего не найдено»
│   │   └── loading-overlay/         # p-progressSpinner
│   └── pipes/
│       ├── status-label.pipe.ts     # codeName → displayName
│       └── full-name.pipe.ts        # { firstName, lastName, middleName } → строка
│
├── layouts/
│   ├── auth-layout/                 # Центрированный layout для /login
│   └── main-layout/                 # Sidebar (по роли) + topbar + <router-outlet>
│       └── nav-items.ts             # Меню: фильтруется по authService.role()
│
└── features/ (все — lazy-loaded через loadComponent)
    ├── auth/login/                  # Форма входа
    ├── teachers/
    │   ├── teachers-list/           # Таблица с поиском; кнопка «Запросить руководство» (Student)
    │   └── teacher-detail/          # Детали + статистика ВКР + список тем
    ├── topics/
    │   ├── topics-list/             # Таблица с фильтрами; кнопки CRUD для Teacher
    │   └── topic-form/              # /topics/new + /topics/:id (create + edit по правам)
    ├── supervisor-requests/
    │   ├── supervisor-requests-list/
    │   └── supervisor-request-detail/ # Кнопки approve/reject (Teacher) или cancel (Student)
    ├── applications/
    │   ├── applications-list/
    │   ├── application-create/      # Выбор темы + SupervisorRequest
    │   └── application-detail/      # Детали + история действий + чат-секция
    ├── chat/
    │   └── chat-window/             # Встраиваемый компонент (не отдельная страница)
    │                                # Polling каждые 5 сек., afterId для инкремента
    ├── graduate-works/
    │   ├── graduate-works-list/     # Таблица с фильтрами (год, преподаватель, название)
    │   └── graduate-work-detail/    # Детали + кнопка скачать (presigned URL)
    ├── notifications/
    │   └── notifications-list/      # Inbox с пагинацией и фильтром isRead
    └── admin/
        ├── users/
        │   ├── users-list/          # Таблица пользователей с фильтрами
        │   └── create-user-dialog/  # Диалог создания
        ├── graduate-works-manage/
        │   ├── admin-gw-list/       # Управление архивом ВКР
        │   ├── create-gw-dialog/    # Создание записи
        │   └── upload-gw-file/      # Шаги: create → upload-url → PUT S3 → confirm
        ├── analytics/               # Графики (p-chart + Chart.js) по заявкам и ВКР
        └── export/                  # Кнопки экспорта (Excel, CSV)
```

### Цветовая система (CSS-переменные в styles.scss)

Все компоненты используют только переменные, не хардкодят цвета.

| Переменная | Значение | Назначение |
|---|---|---|
| `--blue-primary` | `#1a56db` | Кнопки, ссылки, активные элементы |
| `--navy` | `#0d2d6b` | Фон sidebar |
| `--white` | `#ffffff` | Фон topbar, карточек |
| `--bg-page` | `#f4f7fe` | Фон контентной области |
| `--text-primary` | `#0d1f3c` | Основной текст |
| `--error` | `#dc2626` | Ошибки |

Тема PrimeNG: Aura preset с синим primary (`#1a56db`), без `severity="success"` (конфликтует с темой).

---

## БАЗА ДАННЫХ (PostgreSQL 16.8)

### Источник истины
SQL-скрипты в `infra/db/init/`. EF Core работает **поверх готовой схемы** — EF-миграций не ведётся параллельно (один источник истины).

### Инициализация (27 скриптов)

| Скрипт | Содержание |
|--------|-----------|
| `00` | Расширения: `citext` (нечувствительный к регистру текст для email), `pgcrypto` (gen_random_uuid) |
| `01–09` | Справочники: UserRoles, ApplicationStatuses, ApplicationActionStatuses, TopicStatuses, NotificationTypes, AcademicDegrees, AcademicTitles, Positions, TopicCreatorTypes |
| `10` | Departments (c HeadId → Users nullable FK) |
| `11` | Users (Email citext unique, PasswordHash, FirstName, LastName, MiddleName, RoleId, DepartmentId, IsActive) |
| `12` | StudyGroups |
| `13` | Students (UserId 1:1, StudyGroupId) |
| `14` | Teachers (UserId 1:1, MaxStudentsLimit nullable, AcademicDegreeId, AcademicTitleId, PositionId) |
| `15` | Topics (TeacherId, StatusId, CreatorTypeId, Title, Description, Year) |
| `16` | SupervisorRequests (StudentId, TeacherUserId, StatusId, Comment) |
| `17` | StudentApplications (StudentId, TopicId nullable, SupervisorRequestId, StatusId, ProposedTitle nullable) |
| `18` | ApplicationActions (ApplicationId, ResponsibleId, NewStatusId, Comment, Kind) |
| `19` | ChatMessages (ApplicationId, SenderId, Content, SentAt, ReadAt nullable) |
| `20` | Notifications (UserId, TypeId, Title, Content, IsRead) |
| `21` | GraduateWorks (ApplicationId unique FK, StudentId, TeacherId, Title, Year, Grade, CommissionMembers, FilePath, PresentationPath nullable) |
| `22` | ApplicationTopicChangeHistories |
| `23` | Все FK-ограничения (применяются после создания всех таблиц) |
| `24` | Индексы производительности |
| `99` | Тестовые данные (admin: `z_admin@example.com` / `TestPassword123!`) |

### Ключевые ограничения и индексы

```sql
-- Уникальность
Users.Email — UNIQUE (citext, нечувствителен к регистру)
(TeacherId, Year, Title) — уникальная тема
GraduateWorks.ApplicationId — UNIQUE (одна работа на заявку)

-- Производительность
IX_ChatMessages_ApplicationId_SentAt (составной)
IX_ChatMessages_ApplicationId_SenderId_ReadAt (частичный, WHERE ReadAt IS NULL)
IX_Notifications_UserId_IsRead (частичный, WHERE IsRead = FALSE)
IX_Notifications_UserId_CreatedAt

-- CASCADE / RESTRICT
ON DELETE CASCADE — большинство дочерних записей
ON DELETE RESTRICT — GraduateWorks.ApplicationId (историческое сохранение)
```

### Автоматическое UpdatedAt
PostgreSQL-триггеры `update_{table}_updated_at` обновляют `UpdatedAt = NOW()` при любом `UPDATE`. EF Core не управляет этим полем.

---

## ИНФРАСТРУКТУРА

### Docker Compose конфигурации

| Файл | Назначение |
|------|-----------|
| `compose.db.yml` | Только PostgreSQL (порт 5433) + init-скрипты |
| `compose.backend.yml` | PostgreSQL + Redis (6380) + MinIO (9000/9001) + Backend API (5001) |
| `compose.dev.yml` | Полный стек разработки (+Prometheus/Grafana) |
| `compose.prod.yml` | Production (+Nginx reverse proxy, HTTPS) |

### Секреты
Хранятся в `infra/docker/secrets/`: `postgres_password.txt`, `redis_password.txt`, `minio_access_key.txt`, `minio_secret_key.txt`.

### Пересоздание БД
`infra/docker/recreate-database.ps1` — удаляет volumes, поднимает заново.

### Порты (хост, dev)
- API: `http://localhost:5001` (Swagger: `/swagger`)
- PostgreSQL: `localhost:5433`
- Redis: `localhost:6380`
- MinIO API: `localhost:9000`
- MinIO Console: `localhost:9001`

---

## ТЕСТЫ

### Backend Unit (`tests/AcademicTopicSelectionService.UnitTests/`)
xUnit. Покрывают бизнес-правила сервисов:
- `SupervisorRequestsServiceTests` — approve отменяет остальные запросы; лимит; дубль к тому же преподавателю; reject без комментария → Validation
- `StudentApplicationsServiceTests` — проверка SupervisorRequest при создании; лимит студентов; конкуренция за тему
- `ChatMessagesServiceTests` — посторонний не может писать; пустой/длинный контент; read не трогает свои исходящие
- `GraduateWorksServiceTests` — дубль ApplicationId; ConfirmUpload когда файла нет в S3; DownloadUrl когда FilePath == null
- `NotificationsServiceTests` — чужое уведомление → Forbidden; фильтр isRead
- `ApplicationActionsServiceTests`, `AuthServiceTests` и др.

### Backend Integration (`tests/AcademicTopicSelectionService.IntegrationTests/`)
Testcontainers (PostgreSQL + Redis в реальных Docker-контейнерах). Покрывают полные сценарии:
- `SupervisorRequestsIntegrationTests` — полный сценарий; конкурентное одобрение; фильтр по датам
- `ApplicationsIntegrationTests` — полный поток; чат; видимость по ролям
- `GraduateWorksIntegrationTests` — CRUD; политики доступа
- `NotificationsIntegrationTests` — создание запроса создаёт уведомление; read-all только своих

### Frontend Unit (`*.spec.ts` рядом с компонентами)
Jasmine + Karma. Покрывают: `auth.service`, `auth.guard`, `role.guard`, interceptors, ключевые компоненты.

### E2E (Playwright, `frontend/e2e/`)
Написаны (`auth.spec.ts`, `supervisor-requests.spec.ts`, `applications.spec.ts`, `chat.spec.ts`, `admin.spec.ts`), но **вне scope MVP**.

---

## API — ПОЛНЫЙ СПИСОК

Базовый URL: `/api/v1`. Все эндпоинты требуют авторизации, кроме Auth.

```
HEALTH
  GET  /health
  GET  /health/db                             # Admin JWT или X-Health-Probe-Key

AUTH
  POST /api/v1/auth/login
  POST /api/v1/auth/refresh
  POST /api/v1/auth/logout

USERS (Admin)
  GET  /api/v1/users                          # ?roleId, ?query, ?page, ?pageSize
  POST /api/v1/users

СПРАВОЧНИКИ (GET — все; CRUD — Admin)
  /api/v1/user-roles
  /api/v1/application-statuses
  /api/v1/application-action-statuses
  /api/v1/topic-statuses
  /api/v1/topic-creator-types
  /api/v1/notification-types
  /api/v1/study-groups
  /api/v1/academic-degrees
  /api/v1/academic-titles
  /api/v1/positions

TEACHERS (только чтение)
  GET  /api/v1/teachers
  GET  /api/v1/teachers/{id}

STUDENTS (только чтение)
  GET  /api/v1/students
  GET  /api/v1/students/{id}

TOPICS
  GET    /api/v1/topics                       # ?query, ?statusCodeName, ?createdFromUtc, ?createdToUtc...
  GET    /api/v1/topics/{id}
  POST   /api/v1/topics                       # Teacher
  PUT    /api/v1/topics/{id}                  # Teacher
  PATCH  /api/v1/topics/{id}
  DELETE /api/v1/topics/{id}                  # Teacher

ПОТОК 1: SUPERVISOR REQUESTS
  GET  /api/v1/supervisor-requests            # Student: свои; Teacher: входящие; Admin: все
  GET  /api/v1/supervisor-requests/{id}
  POST /api/v1/supervisor-requests            # Student
  PUT  /api/v1/supervisor-requests/{id}/approve   # Teacher
  PUT  /api/v1/supervisor-requests/{id}/reject    # Teacher (Comment обязателен)
  PUT  /api/v1/supervisor-requests/{id}/cancel    # Student

ПОТОК 2: STUDENT APPLICATIONS
  GET  /api/v1/applications                   # Видимость по роли
  GET  /api/v1/applications/{id}
  POST /api/v1/applications                   # Student (supervisorRequestId обязателен)
  PUT  /api/v1/applications/{id}/approve              # Teacher → PendingDepartmentHead
  PUT  /api/v1/applications/{id}/reject               # Teacher (Comment обязателен)
  PUT  /api/v1/applications/{id}/department-head-approve  # DepartmentHead → финал
  PUT  /api/v1/applications/{id}/department-head-reject   # DepartmentHead (Comment)
  PUT  /api/v1/applications/{id}/cancel               # Student (из Pending или ApprovedBySupervisor)

APPLICATION ACTIONS (журнал истории заявки)
  GET  /api/v1/application-actions?applicationId={id}  # обязательный параметр
  GET  /api/v1/application-actions/{id}
  POST /api/v1/application-actions
  PATCH /api/v1/application-actions/{id}
  DELETE /api/v1/application-actions/{id}

ЧАТ
  GET  /api/v1/applications/{applicationId}/messages       # ?afterId, ?limit
  POST /api/v1/applications/{applicationId}/messages
  PUT  /api/v1/applications/{applicationId}/messages/read-all  # также читает NewMessage-уведомления

NOTIFICATIONS
  GET  /api/v1/notifications                  # ?page, ?pageSize, ?isRead
  PUT  /api/v1/notifications/{id}/read
  PUT  /api/v1/notifications/read-all

GRADUATE WORKS
  GET    /api/v1/graduate-works               # ?year, ?titleQuery, ?teacherId, ?teacherQuery, ?page, ?pageSize
  GET    /api/v1/graduate-works/{id}
  POST   /api/v1/graduate-works               # Admin
  PUT    /api/v1/graduate-works/{id}          # Admin
  DELETE /api/v1/graduate-works/{id}          # Admin
  POST   /api/v1/graduate-works/{id}/upload-url/{fileType}    # Admin (fileType: thesis|presentation)
  POST   /api/v1/graduate-works/{id}/confirm-upload/{fileType} # Admin
  GET    /api/v1/graduate-works/{id}/download-url/{fileType}  # Все авторизованные

ADMIN
  GET  /api/v1/admin/analytics                # Сводная аналитика
  GET  /api/v1/admin/export?format=excel
  GET  /api/v1/admin/export?format=csv&dataset={graduate-works|applications|users}
```

---

## СТАТУС ПРОЕКТА

MVP реализован полностью (итерации 0–9 бэкенда и фронтенда закрыты, 2026-05-19).