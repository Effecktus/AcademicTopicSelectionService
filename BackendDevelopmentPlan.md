# План разработки Backend (ASP.NET Core 10) для проекта «AcademicTopicSelectionService»

Документ — **отдельный, backend-ориентированный план**, составленный на базе `DevelopmentPlan.md` и **актуальной** структуры репозитория (обновлено: **2026-04-19** — auth/NFR, приоритеты перед фронтом).

## 0) Цель и границы backend

### Цель
- Реализовать Web API для сервиса выбора научного руководителя/темы ВКР, чата (polling), архива ВКР и админ‑функций.
- Обеспечить авторизацию (JWT + refresh), роли, аудит, валидацию, стабильную работу с PostgreSQL и инфраструктурные интеграции.

### Границы (что именно делает backend)
- **REST API** для фронта/клиентов
- **Бизнес‑логика** (правила статусов заявок, доступность чата, ограничения по темам и т.п.)
- **Доступ к данным** (PostgreSQL)
- Интеграции: **Redis** (refresh-токены / отзыв сессий), **S3/MinIO** (файлы), **SMTP** (уведомления) — частично внедрено, см. §1
- Тех.обвязка: логирование, healthchecks, swagger, метрики (опционально)

### Вне scope на старте (можно отложить)
- Полный мониторинг Prometheus/Grafana и дашборды
- Полноценная аналитика/экспорт (можно сделать после базовых сущностей)
- Тонкая оптимизация запросов (после появления профиля нагрузки)

## 1) Текущий статус репозитория

### Уже реализовано

- **Схема БД**: SQL‑скрипты в `infra/db/init/*.sql` (в т.ч. расширения, справочники, пользователи, темы, заявки, чат, уведомления, ВКР, FK и индексы).
- **Тестовые данные** (ручной прогон): `infra/db/test_data/99_seed_test_data.sql` (в т.ч. администратор `z_admin@example.com` / `TestPassword123!`).
- **Пересоздание БД из init**: `infra/docker/recreate-database.ps1` (удаление volumes + `docker compose up`).
- **Compose**: `infra/docker/compose.*.yml` (БД, backend+deps, полный dev и др.).
- **Каркас backend** (Clean Architecture): `backend/src/*`, solution `backend/AcademicTopicSelectionService.slnx`
  - `Domain` — сущности, `IAuditableEntity`
  - `Application` — сервисы, DTO, абстракции репозиториев
  - `Infrastructure` — EF Core `ApplicationDbContext`, репозитории, JWT, Redis, интеграции
  - `API` — контроллеры, Swagger, JWT, авторизация, DI
- **Health**: `GET /health` — анонимный smoke; `GET /health/db` — проверка PostgreSQL через `IDatabaseHealthChecker`, доступ только при **JWT с ролью Admin** или заголовке **`X-Health-Probe-Key`**, если в конфигурации задан непустой **`Health:DbProbeKey`** (`API/Health/HealthDbAccess.cs`, см. `appsettings*.json` и `TestWebApplicationFactory` для тестов).
- **Версионирование API**: `/api/v1/...` (Asp.Versioning, версия в URL).
- **Аутентификация**: `POST /api/v1/auth/login`, `refresh`, `logout`; **создание учётных записей** — `POST /api/v1/users` (**только Admin**, `UsersController` + `UserAccountsService`); публичной **саморегистрации нет** (вход по учётке вуза / SSO — отдельная фаза). Access JWT (HMAC), refresh в **Redis** с ротацией; пароли через `IPasswordHasher` (BCrypt). **Rate limiting** на `login` и `refresh` (`RateLimitPolicyNames`, в среде **`Testing`** — ослабленные лимиты для интеграционных тестов).
- **Валидация учётных данных**: `Application/Security/CredentialValidation.cs` — нормализация и проверка email, политика пароля при создании пользователя; при `login` невалидный email отсекается до обращения к БД.
- **Ошибки API**: `AddExceptionHandler<GlobalExceptionHandler>()` + `AddProblemDetails()` + `UseExceptionHandler` / `UseStatusCodePages` в `Program.cs`.
- **Утилиты API**: `ClaimsPrincipalExtensions` (`GetUserId`, `GetRoleCode`) — единое чтение JWT-claim в контроллерах заявок, тем, уведомлений, чата.
- **Авторизация**: fallback‑политика «только аутентифицированные»; **`GET /user-roles`** (список и по id) и изменение справочника — **роль `Admin`**; прочие справочники — чтение для вошедших пользователей, **изменение** — `Admin` (уточнять по контроллерам); **`ApplicationActions`** — см. `ApplicationActionsService` / `IApplicationActionsRepository.UserCanReadApplicationActionsAsync`: **админ — всё**; **чтение и создание** — студент-автор заявки, научрук из `SupervisorRequest` или пользователь, бывший `ResponsibleId` в любой записи по этой заявке; **`PATCH`/`DELETE`** — только **`ResponsibleId`** этой записи или админ.
- **Справочники (CRUD, `/api/v1/...`)**: user-roles, application-statuses, topic-statuses, notification-types, academic-degrees, academic-titles, positions, study-groups, topic-creator-types, application-action-statuses.
- **Доменный read API**: `GET /api/v1/teachers`, `GET …/{id}`; `GET /api/v1/topics`, `GET …/{id}` (фильтры `query`, `creatorQuery`, `statusCodeName`, `createdByUserId`, `creatorTypeCodeName`, **`createdFromUtc`**, **`createdToUtc`**, `sort`, пагинация); `GET /api/v1/students`, `GET …/{id}` (фильтры `query`, `groupId`). Все под `[Authorize]`; в списках преподавателей и студентов — только **активные** пользователи (`Users.IsActive`).
- **Доменный API (частично)**: `application-actions` — CRUD по действиям заявки; список **только с обязательным** `?applicationId=` (глобального списка нет).
- **Поток 1 (`SupervisorRequests`)**: реализованы endpoint-ы списка/деталей/создания/approve/reject/cancel, ограничения по ролям, атомарная авто-отмена альтернативных `Pending`-запросов студента при approve; в списке — фильтр по дате создания (`createdFromUtc` / `createdToUtc`), сортировка и пагинация.
- **Поток 2 (`StudentApplications`)**: реализован с обязательным `SupervisorRequestId`; проверка научрука/заведующего и лимитов через `SupervisorRequest.TeacherUserId`.
- **Чат (polling)** по заявке: `ApplicationChatMessagesController`, `ChatMessagesService`, `IChatMessagesRepository`, read-all; unit- и интеграционные тесты (`ApplicationsIntegrationTests`, блок Chat).
- **Архив ВКР + файлы**: `GraduateWorksController`, `GraduateWorksService`, presigned upload/download, `ConfirmUpload`; `IFileStorageService` (Development / S3); unit- и интеграционные тесты.
- **Уведомления (6а + 6б)**: Inbox API, `NotificationsService`, email-очередь (`Channel` + `EmailBackgroundService`); события в `SupervisorRequests` и `StudentApplications` (6а); **`NewMessage`** при отправке сообщения в чат и **`GraduateWorkUploaded`** при `ConfirmUploadAsync` (6б, 2026-04-19); `INotificationsService.CreateAndSaveAsync`; константы типов в `Application/Notifications/NotificationTypeCodes.cs`; в интеграционных тестах — сид типов `NotificationTypesTestSeed`.
- **Тесты**: unit-тесты сервисов (справочники, Auth, ApplicationActions) — **сотни** кейсов (точное число: `dotnet test`); интеграционные тесты API + PostgreSQL/Redis через Testcontainers (нужен Docker).
- **Документация API**: Swagger в Development; при необходимости — `docs/api/`.

### Принцип по БД (источник истины)

Источник истины по схеме — **SQL** в `infra/db/init`. EF Core — ORM поверх готовой схемы; **миграции EF** намеренно не ведём параллельно SQL (один источник истины).

### Что ещё не сделано (крупными блоками)

По функциональному **MVP backend крупных блоков не осталось** (итерации 0–6б закрыты). Дальше — **короткий хвост перед фронтом** (см. **«Приоритеты перед передачей на фронт»** ниже), затем по желанию: **FluentValidation**, **Serilog**, эксплуатация и мониторинг (§0, §3, §8).

### Приоритеты перед передачей на фронт (рекомендуемый порядок)

| Приоритет | Задача | Зачем |
|-----------|--------|--------|
| **P0** | Зафиксировать для себя/фронта: цепочка **login → access + refresh**, **POST /users** только с Admin JWT, **`/health/db`** | **Готово:** см. репозиторий [`docs/api/v1.auth-and-users.md`](docs/api/v1.auth-and-users.md) и ссылку в [`docs/api/README.md`](docs/api/README.md) |
| **P1** | **CORS** (`Cors:AllowedOrigins`, в Development — `http://localhost:4200`) | **Готово:** см. `Program.cs`, `appsettings.Development.json`, [`docs/api/v1.auth-and-users.md`](docs/api/v1.auth-and-users.md) |
| **P2** | **`GET /user-roles`** (список и по id) без анонимного доступа — **`[Authorize(Roles = Admin)]`** | **Готово:** `UserRolesController`, тесты, [`docs/api/v1.user-roles.md`](docs/api/v1.user-roles.md) |
| **P3** | Политики **`ApplicationActions`** (доступ по заявке и `ResponsibleId`) | **Готово:** `ApplicationActionsService`, `UserCanReadApplicationActionsAsync`, тесты |
| **P4** | **Docker / healthcheck**: для пробы БД из compose либо только `GET /health`, либо `GET /health/db` + env **`Health:DbProbeKey`** и заголовок **`X-Health-Probe-Key`** | Согласованность с закрытым `/health/db` |

**Уже не блокер для старта фронта:** индексы для `ChatMessages` в `infra/db/init/23_create_indexes.sql` присутствуют (`IX_ChatMessages_ApplicationId_SentAt`, частичный `IX_ChatMessages_ApplicationId_SenderId_ReadAt`).

**На следующую неделю (фронт):** опереться на Swagger/OpenAPI, обработать **400 / 401 / 403 / 409 / 429**, экраны login, заявок, чата (polling), уведомлений.

## 2) Архитектура решения и слоёв (Clean Architecture)

Структура проекта (совпадает по смыслу с `DevelopmentPlan.md`):

```
backend/
  AcademicTopicSelectionService.slnx
  Dockerfile
  src/
    AcademicTopicSelectionService.Domain/
    AcademicTopicSelectionService.Application/
    AcademicTopicSelectionService.Infrastructure/
    AcademicTopicSelectionService.API/
```

### Правило зависимостей
- `Domain` — ни от чего
- `Application` → `Domain`
- `Infrastructure` → `Application`, `Domain`
- `API` → `Application`, `Infrastructure` (Infrastructure — для DI)

## 3) Базовые нефункциональные требования

| Требование | Статус |
|------------|--------|
| Хеш паролей (BCrypt) | Внедрено |
| JWT + refresh, Redis для refresh | Внедрено |
| Создание пользователей (админ), без публичной регистрации | Внедрено (`POST /api/v1/users`) |
| Валидация email / пароля (создание пользователя, login) | Внедрено (`CredentialValidation`) |
| Роли и политики авторизации | Внедрено (`ApplicationActions` по заявке и ответственному; `user-roles` GET — Admin) |
| Ограничение частоты запросов (login / refresh) | Внедрено (`AddRateLimiter`, политики в `API/RateLimiting`) |
| Защита `GET /health/db` | Внедрено (Admin JWT или `X-Health-Probe-Key` + `Health:DbProbeKey`) |
| CORS | Внедрено (`Cors:AllowedOrigins`, default policy; в Testing список пустой — middleware не подключается) |
| Единый формат ошибок (ProblemDetails) | Используется в контроллерах + **глобальный** `GlobalExceptionHandler` |
| Версионирование API `/api/v1` | Внедрено |
| Swagger / OpenAPI | Внедрено |
| FluentValidation | **Не подключён**; валидация в сервисах / атрибутах — по мере необходимости |
| Serilog / структурированные логи | По желанию (усилить позже) |
| Уведомления по чату и архиву ВКР (ит. 6б: `NewMessage`, `GraduateWorkUploaded`) | Внедрено (2026-04-19) |

## 4) Итерации (roadmap)

Ниже — **логическая последовательность**. Номера и факты «готово» синхронизированы с текущим кодом.

### Итерация 0 — «Скелет + подключение к БД» ✅

- Health, Dockerfile, compose, EF к существующей схеме.

**Проверка (типично):**
- API в Docker: `http://localhost:5001/swagger` (маппинг `5001:80` в `compose.backend.yml`).
- API на хосте: см. `backend/run-watch.ps1` — порт из `launchSettings`/конфига (часто `5000`).
- Postgres с хоста: `localhost:5433` (см. compose).

### Итерация 1 — «Справочники + доменные read API» ✅

**Готово:** CRUD справочников (список в §1); read API **teachers**, **topics**, **students** (см. §1); `application-actions` как технический ресурс; пагинация в формате `PagedResult` (`page`, `pageSize`, `total`, `items`), как у справочников.

### Итерация 2 — «JWT Auth + роли» ✅

- **Login / refresh / logout**; создание пользователей **`POST /api/v1/users` (Admin)**; роли из `UserRoles`; политики на контроллерах; rate limiting на **login** и **refresh**; валидация учётных данных при создании пользователя и при входе.

**Уточнение:** refresh хранится в **Redis**; при отсутствии Redis приложение не поднимется (см. конфигурацию окружения).

### Итерация 3 — «Два потока заявок» ✅ (закрыто 2026-04-14)

> **Итог реализации (2026-04-14):** поток разделён на два независимых сценария:
> `SupervisorRequests` (выбор научрука) и `StudentApplications` (утверждение темы).
> `StudentApplications` переведён на связь через `SupervisorRequestId`; проверки научрука
> и заведующего кафедрой выполняются через `SupervisorRequest.TeacherUserId`.

---

#### Общая архитектура двух потоков

**Поток 1 — Выбор научного руководителя** (`SupervisorRequests`)

Студент выбирает преподавателя, которого хочет в качестве научрука. Преподаватель принимает или отклоняет.

```
Студент → POST /api/v1/supervisor-requests
  Pending → approve (Teacher) → ApprovedBySupervisor  [терминальный-успех]
  Pending → reject  (Teacher) → RejectedBySupervisor  [терминальный-отказ]
  Pending → cancel  (Student) → Cancelled             [терминальный-отказ]
```

- **Допускается несколько активных запросов** к разным преподавателям (в рамках ограничений кафедры).
- После одобрения студент может создать заявку на тему (Поток 2).
- Статусы используются из общего справочника `ApplicationStatuses`.

**Поток 2 — Утверждение темы ВКР** (`StudentApplications`)

Требует одобренного `SupervisorRequest`. Тему создаёт **преподаватель** (в разделе тем) или **студент** прямо при подаче заявки (`CreatorType = "Student"`). Научрук в этом потоке берётся из связанного `SupervisorRequest`, а **не** из `Topics.CreatedBy`.

```
Студент → POST /api/v1/applications  (с topicId или предложением новой темы)
  Pending → approve               (Supervisor из SupervisorRequest) → PendingDepartmentHead
  Pending → reject                (Supervisor)                      → RejectedBySupervisor
  PendingDepartmentHead → department-head-approve  (DepartmentHead) → ApprovedByDepartmentHead
  PendingDepartmentHead → department-head-reject   (DepartmentHead) → RejectedByDepartmentHead
  OnEditing, Pending или ApprovedBySupervisor → cancel (Student) → Cancelled
  PendingDepartmentHead → cancel                                    → ЗАПРЕЩЕНО
```

Отдельного HTTP-метода «передать заведующему» **нет** — это делает `approve`.

- Терминальные: `RejectedBySupervisor`, `RejectedByDepartmentHead`, `Cancelled`, `ApprovedByDepartmentHead`.
- Каждый переход: одна атомарная операция — `UPDATE` статуса + `INSERT ApplicationActions` (единый `SaveChangesAsync`).
- Для reject — непустой `Comment`.

---

#### Схема БД — изменения

**Новая таблица `SupervisorRequests`** (`infra/db/init/16_create_supervisor_requests.sql`):

```sql
CREATE TABLE "SupervisorRequests" (
    "Id"         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "StudentId"  UUID NOT NULL REFERENCES "Students"("Id"),
    "TeacherUserId" UUID NOT NULL REFERENCES "Users"("Id"),
    "StatusId"   UUID NOT NULL REFERENCES "ApplicationStatuses"("Id"),
    "Comment"    TEXT,
    "CreatedAt"  TIMESTAMPTZ NOT NULL DEFAULT now(),
    "UpdatedAt"  TIMESTAMPTZ
);
CREATE INDEX "IX_SupervisorRequests_StudentId" ON "SupervisorRequests"("StudentId");
CREATE INDEX "IX_SupervisorRequests_TeacherUserId" ON "SupervisorRequests"("TeacherUserId");
```

**Изменение таблицы `StudentApplications`** (внесено напрямую в `infra/db/init/17_create_applications.sql`):

```sql
CREATE TABLE "StudentApplications" (
    ...
    "SupervisorRequestId" UUID NULL REFERENCES "SupervisorRequests"("Id"),
    ...
);
```

- `Topics.CreatedBy` больше **не используется** для идентификации научрука в потоке 2; роль научрука определяется через `SupervisorRequests.TeacherUserId`.
- Тема может быть создана студентом (`Topics.CreatorTypeId` → `"Student"`); это нормально.

---

#### Ключевые бизнес-правила

| # | Правило |
|---|---------|
| 1 | Студент может подать **несколько одновременных** запросов к разным преподавателям |
| 1а | Максимальное количество активных запросов = число преподавателей на кафедре (роль `Teacher`, без учёта `DepartmentHead`); если кафедра не определена — ограничение не применяется |
| 1б | Нельзя подать повторный запрос тому же преподавателю, у которого уже есть активный запрос от этого студента |
| 2 | Когда преподаватель **одобряет** запрос (`ApprovedBySupervisor`) — все остальные активные запросы этого студента **автоматически отменяются** (`Cancelled`) в той же атомарной транзакции |
| 3 | `StudentApplication` можно создать только при наличии хотя бы одного `SupervisorRequest` со статусом `ApprovedBySupervisor` у этого студента; в `CreateApplicationCommand` — передаётся `SupervisorRequestId` явно |
| 4 | `SupervisorRequest.TeacherUserId` == `callerUserId` проверяется при `approve`/`reject` потока 1 |
| 5 | В потоке 2: научрук = `SupervisorRequest.TeacherUserId` (не `Topics.CreatedBy`) |
| 6 | `DepartmentHead` определяется как пользователь с ролью `DepartmentHead`, чей `DepartmentId` совпадает с `DepartmentId` научрука (`SupervisorRequest.TeacherUserId` → `Users.DepartmentId`) |
| 7 | `Teachers.MaxStudentsLimit` проверяется при `ApproveByDepartmentHead` (поток 2): считаем только заявки `StudentApplications` со статусом `ApprovedByDepartmentHead` у этого преподавателя |
| 8 | Конкуренция за тему: одна активная заявка на тему (`HasActiveApplicationOnTopicAsync`) |
| 9 | Тема должна быть в статусе `Active` при подаче заявки (проверка `IsActiveByIdAsync`) |

---

#### Что изменяется в коде (порядок работ)

Ниже приведён исходный план работ по итерации 3; пункты реализованы и используются как чек‑лист выполненного объёма.

**Итерация 3а — Поток 1: SupervisorRequests**

1. **SQL:** `16_create_supervisor_requests.sql` — новая таблица + индекс `(StudentId, TeacherUserId)` для проверки дублей.
2. **Domain:** сущность `SupervisorRequest` (partial class, аналог `StudentApplication`).
3. **Application:**
   - `SupervisorRequests/Contracts.cs` — DTO, команды (`CreateSupervisorRequestCommand`, `ApproveSupervisorRequestCommand`, `RejectSupervisorRequestCommand`), `SupervisorRequestsError`.
   - `ISupervisorRequestsRepository`:
     - `ListForRoleAsync` — Student: свои; Teacher: входящие; Admin: все
     - `GetDetailAsync`, `GetByIdWithTrackingAsync`, `AddAsync`, `SaveChangesAsync`
     - `HasActiveRequestForTeacherAsync(studentId, teacherUserId)` — проверка дубля
     - `CountActiveRequestsForStudentAsync(studentId)` — для проверки лимита
     - `CountTeachersInDepartmentAsync(departmentId)` — для вычисления лимита
     - `CancelAllActiveRequestsExceptAsync(studentId, approvedRequestId)` — **атомарная** отмена остальных
     - `GetApprovedRequestsByStudentAsync(studentId)` — для Потока 2
   - `ISupervisorRequestsService` + `SupervisorRequestsService`:
     - `CreateAsync` — проверить дубль, вычислить лимит (если dept известен), создать запрос
     - `ApproveAsync` — одобрить + **в той же транзакции** отменить все остальные активные запросы студента
     - `RejectAsync` — отклонить (обязательный `Comment`)
     - `CancelAsync` — студент отменяет (только из `Pending`)
   - Зарегистрировать в `DependencyInjection.cs`.
4. **Infrastructure:** `SupervisorRequestsRepository` + `ApplicationDbContext` (добавить DbSet).
5. **API:** `SupervisorRequestsController`:
   - `GET  /api/v1/supervisor-requests` (Student — свои; Teacher — входящие; Admin — все; query: `page`, `pageSize`, `sort`, опционально `createdFromUtc` / `createdToUtc` — фильтр по дате создания, UTC включительно)
   - `GET  /api/v1/supervisor-requests/{id}`
   - `POST /api/v1/supervisor-requests` (Student)
   - `PUT  /api/v1/supervisor-requests/{id}/approve` (Teacher)
   - `PUT  /api/v1/supervisor-requests/{id}/reject`  (Teacher, Comment обязателен)
   - `PUT  /api/v1/supervisor-requests/{id}/cancel`  (Student)
6. **Tests:**
   - Unit: `SupervisorRequestsServiceTests` — approve отменяет остальные; лимит; дубль к тому же преподавателю; reject без комментария → Validation
   - Integration: `SupervisorRequestsIntegrationTests` — полный сценарий; конкурентное одобрение двух преподавателей; фильтр списка по `createdFromUtc` / `createdToUtc`.

**Итерация 3б — Поток 2: StudentApplications (рефакторинг)**

1. **SQL:** обновить `17_create_applications.sql` — добавить `SupervisorRequestId` напрямую в `CREATE TABLE StudentApplications`.
2. **Domain:** обновить `StudentApplication` — добавить `SupervisorRequestId` + навигационное свойство `SupervisorRequest`.
3. **Application:**
   - `StudentApplications/Contracts.cs` — добавить `SupervisorRequestId` в `CreateApplicationCommand`; обновить `StudentApplicationDetailDto` (включить `SupervisorRequest` snapshot).
   - `StudentApplicationsService.CreateAsync` — проверять наличие одобренного `SupervisorRequest` у студента; устанавливать `SupervisorRequestId`.
   - `VerifySupervisorAsync` — научрук теперь `application.SupervisorRequest.TeacherUserId`, а не `Topic.CreatedBy`.
   - `VerifyDepartmentHeadAsync` — кафедра берётся из `SupervisorRequest.TeacherUserId → Users.DepartmentId`.
   - `CountOccupiedSlotsBySupervisorAsync` — считать только заявки со статусом `ApprovedByDepartmentHead` у соответствующего научрука.
4. **Infrastructure:** обновить `StudentApplicationsRepository` — include `SupervisorRequest` в запросах.
5. **Tests:** обновить `StudentApplicationsServiceTests` + `ApplicationsIntegrationTests` (seed теперь создаёт `SupervisorRequest` перед `StudentApplication`).

---

#### HTTP endpoints итога (оба потока)

```
# Поток 1
GET    /api/v1/supervisor-requests
GET    /api/v1/supervisor-requests/{id}
POST   /api/v1/supervisor-requests
PUT    /api/v1/supervisor-requests/{id}/approve
PUT    /api/v1/supervisor-requests/{id}/reject
PUT    /api/v1/supervisor-requests/{id}/cancel

# Поток 2
GET    /api/v1/applications
GET    /api/v1/applications/{id}
POST   /api/v1/applications
PUT    /api/v1/applications/{id}/approve
PUT    /api/v1/applications/{id}/reject
PUT    /api/v1/applications/{id}/department-head-approve
PUT    /api/v1/applications/{id}/department-head-reject
PUT    /api/v1/applications/{id}/cancel
```

---

#### Актуальный приоритет выполнения (обновлено: 2026-04-19)

Порядок из списка **2026-04-14** отработан полностью:

1. **Итерация 5** — архив ВКР и файлы: **завершена** (см. раздел «Итерация 5» ниже).
2. **Итерация 4** — чат по заявкам: **завершена** (см. раздел «Итерация 4» ниже).
3. **Итерация 6б** — уведомления `NewMessage` и `GraduateWorkUploaded`: **завершена 2026-04-19** (см. «Итерация 6б» внутри блока уведомлений).
4. **Итерация 6а** — Inbox + email для заявок и запросов научрука: **завершена** (см. «Итерация 6а» ниже).

Нумерация разделов сохранена для истории.

### Итерация 4 — «Чат (polling)» — **завершено (2026-04)**

#### Фактически реализовано

- API: `GET` / `POST /api/v1/applications/{applicationId}/messages`, `PUT .../messages/read-all` (`ApplicationChatMessagesController`).
- `ChatMessagesService`, `ChatMessagesRepository`, доступ через `GetChatAccessAsync` (студент владелец + преподаватель из `SupervisorRequest`, допустимые статусы запроса).
- После **итерации 6б**: уведомление **`NewMessage`** получателю (не отправителю) и постановка письма в email-очередь.
- Тесты: `ChatMessagesServiceTests`, сценарии Chat в `ApplicationsIntegrationTests`.

#### Контекст и решения

- Чат привязан к `StudentApplication`; доступен с момента создания `SupervisorRequest` (т.е. как только у студента есть активная заявка на научрука — `Pending` или выше).
- Участники: только **Student** (владелец заявки) и **Teacher** из связанного `SupervisorRequest`. Все остальные роли — без доступа.
- `ReadAt` в `ChatMessages` — бинарный флаг «получатель прочитал»; работает корректно, так как в чате ровно два участника.
- Отметка «прочитано» — **bulk**: одним запросом отмечаем все входящие непрочитанные сообщения в чате заявки.
- Polling: клиент периодически запрашивает `afterId` для инкрементальной подгрузки; WebSocket не используется.

#### 1. SQL

Схема уже готова: `infra/db/init/19_create_chat_messages.sql`.

Индексы для чата в **`infra/db/init/23_create_indexes.sql`** добавлены: `IX_ChatMessages_ApplicationId`, `IX_ChatMessages_SentAt`, составной **`IX_ChatMessages_ApplicationId_SentAt`**, частичный **`IX_ChatMessages_ApplicationId_SenderId_ReadAt`** (для непрочитанных входящих).

#### 2. Domain

Сущность `ChatMessage` уже готова (`Domain/Entities/ChatMessage.cs`). Изменений не требуется.

#### 3. Application

**`ChatMessages/Contracts.cs`**
```
SendMessageCommand    { ApplicationId, Content }
MarkMessagesReadCommand { ApplicationId }          // отмечаем все входящие
ChatMessageDto        { Id, ApplicationId, SenderId, SenderFullName, Content, SentAt, ReadAt }
```

**`ChatMessages/IChatMessagesService.cs`**
```
GetMessagesAsync(applicationId, afterId?, limit)  → IReadOnlyList<ChatMessageDto>
SendMessageAsync(command, senderUserId)           → ChatMessageDto
MarkAsReadAsync(command, readerUserId)            → void
```

**`ChatMessages/ChatMessagesService.cs`** — бизнес-правила:
- `GetMessagesAsync`: проверяем, что текущий пользователь — участник чата (Student владелец или Teacher из SupervisorRequest); возвращаем сообщения по `ApplicationId`, `SentAt DESC`, начиная после `afterId` (если передан), limit по умолчанию 50.
- `SendMessageAsync`: проверяем участника; проверяем, что `Content` не пустой и не длиннее 4000 символов; создаём `ChatMessage`.
- `MarkAsReadAsync`: отмечаем `ReadAt = UtcNow` для всех сообщений, где `ApplicationId` совпадает, `SenderId != readerUserId` (т.е. входящие) и `ReadAt IS NULL`.

**`Abstractions/IChatMessagesRepository.cs`**
```
GetByApplicationAsync(applicationId, afterId?, limit) → IReadOnlyList<ChatMessage>
AddAsync(message)                                     → ChatMessage
MarkIncomingAsReadAsync(applicationId, readerUserId)  → void
```

#### 4. Infrastructure

**`Repositories/ChatMessagesRepository.cs`** — реализация `IChatMessagesRepository`:
- `GetByApplicationAsync`: `WHERE ApplicationId = @id [AND Id > @afterId] ORDER BY SentAt ASC LIMIT @limit`, с `Include(m => m.Sender)`.
- `MarkIncomingAsReadAsync`: `UPDATE ChatMessages SET ReadAt = NOW() WHERE ApplicationId = @appId AND SenderId != @readerId AND ReadAt IS NULL`.

Зарегистрировать в `Infrastructure/DependencyInjection.cs`.

#### 5. API

**`Controllers/ChatMessagesController.cs`**

```
GET  /api/v1/applications/{applicationId}/messages          → GetMessagesAsync
POST /api/v1/applications/{applicationId}/messages          → SendMessageAsync
PUT  /api/v1/applications/{applicationId}/messages/read-all → MarkAsReadAsync
```

Авторизация: `[Authorize]` + проверка участника внутри сервиса (студент-владелец или назначенный teacher).

#### 6. Tests

**Unit: `ChatMessagesServiceTests`**
- Отправка сообщения посторонним → `Forbidden`/`Validation`
- Пустой/слишком длинный `Content` → `Validation`
- `GetMessages` с `afterId` возвращает только последующие сообщения
- `MarkAsReadAsync` не трогает собственные исходящие сообщения пользователя

**Integration: `ChatMessagesIntegrationTests`**
- Полный сценарий: создать заявку → отправить сообщение как Student → прочитать как Teacher → проверить `ReadAt`
- Посторонний не может слать и читать

---

#### HTTP endpoints итога

```
GET  /api/v1/applications/{applicationId}/messages
POST /api/v1/applications/{applicationId}/messages
PUT  /api/v1/applications/{applicationId}/messages/read-all
```

---

### Итерация 5 — «Архив ВКР + файлы (S3/MinIO)» — **завершено (2026-04-15)**

#### Фактически реализовано

- Реализован полный API архива ВКР: CRUD + presigned upload/download URL + confirm upload.
- В `Domain/Application/Infrastructure` добавлены сущности, контракты, сервисы и репозитории для `GraduateWork` c `ApplicationId`.
- Подключена инфраструктура объектного хранилища:
  - `IFileStorageService` + `DevelopmentFileStorageService` (заглушка);
  - `S3FileStorageService` на AWS SDK (совместим с AWS S3 и MinIO).
- Добавлена конфигурация `S3Options` и переключение провайдера `S3:Provider` (`Development` / `S3`) в `Infrastructure/DependencyInjection`.
- Для Docker-compose и appsettings добавлены настройки `S3` (`Provider`, `Endpoint`, `BucketName`, `ForcePathStyle`, ключи/файлы ключей).
- Добавлены unit-тесты `GraduateWorksServiceTests` для ключевых сценариев валидации и генерации ссылок.

#### Контекст и решения

- `GraduateWork` создаётся **вручную Admin**-ом через API (не автоматически при смене статуса заявки).
- К `GraduateWork` необходимо добавить `ApplicationId` (FK → `StudentApplications`) — чтобы связать запись с конкретной заявкой.
- Загрузка и скачивание файлов — через **presigned URL** (MinIO/S3 SDK); бэкенд не проксирует байты, только генерирует временные ссылки.
- Скачивать может любой **авторизованный** пользователь.
- Загружать (генерировать upload-URL) и создавать/обновлять записи может только `Admin`.

#### 1. SQL

**Обновить `21_create_graduate_works.sql`** — добавить `ApplicationId`:
```sql
ALTER TABLE "GraduateWorks"
    ADD COLUMN "ApplicationId" UUID NOT NULL,
    ADD CONSTRAINT "FK_GraduateWorks_StudentApplications"
        FOREIGN KEY ("ApplicationId")
        REFERENCES "StudentApplications"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE;
```
Обновить `23_create_indexes.sql`:
```sql
CREATE UNIQUE INDEX "UX_GraduateWorks_ApplicationId" ON "GraduateWorks" ("ApplicationId");
CREATE INDEX "IX_GraduateWorks_StudentId"            ON "GraduateWorks" ("StudentId");
CREATE INDEX "IX_GraduateWorks_TeacherId"            ON "GraduateWorks" ("TeacherId");
```

#### 2. Domain

**Обновить `GraduateWork.cs`** — добавить:
```csharp
public Guid ApplicationId { get; set; }
public virtual StudentApplication Application { get; set; } = null!;
```

#### 3. Application

**`GraduateWorks/Contracts.cs`**
```
CreateGraduateWorkCommand   { ApplicationId, Title, Year, Grade, CommissionMembers }
UpdateGraduateWorkCommand   { Id, Title, Year, Grade, CommissionMembers }
GenerateUploadUrlQuery      { GraduateWorkId, FileType }   // FileType: "thesis" | "presentation"
GenerateDownloadUrlQuery    { GraduateWorkId, FileType }
GraduateWorkDto             { Id, ApplicationId, StudentId, TeacherId, Title, Year, Grade,
                              CommissionMembers, HasFile, HasPresentation, CreatedAt, UpdatedAt }
FileUrlDto                  { Url, ExpiresAt }
```

**`GraduateWorks/IGraduateWorksService.cs`**
```
GetAllAsync(filters)                         → PagedResult<GraduateWorkDto>
GetByIdAsync(id)                             → GraduateWorkDto
CreateAsync(command)                         → GraduateWorkDto           [Admin]
UpdateAsync(command)                         → GraduateWorkDto           [Admin]
DeleteAsync(id)                              → void                      [Admin]
GetUploadUrlAsync(query)                     → FileUrlDto                [Admin]
GetDownloadUrlAsync(query)                   → FileUrlDto                [Authorized]
ConfirmUploadAsync(graduateWorkId, fileType) → void                      [Admin]
```

**`Abstractions/IFileStorageService.cs`** (инфраструктурная абстракция):
```
GenerateUploadUrlAsync(objectKey, expiresIn)   → FileUrlDto
GenerateDownloadUrlAsync(objectKey, expiresIn) → FileUrlDto
ObjectExistsAsync(objectKey)                   → bool
DeleteObjectAsync(objectKey)                   → void
```

Бизнес-правила `GraduateWorksService`:
- `CreateAsync`: проверяем, что `ApplicationId` существует и для этой заявки ещё нет `GraduateWork` (уникальность).
- `GetUploadUrlAsync`: генерируем `objectKey` вида `graduate-works/{id}/thesis` или `graduate-works/{id}/presentation`; presigned URL действует 15 минут.
- `ConfirmUploadAsync`: вызывается Admin-ом после завершения загрузки; сервис проверяет `ObjectExistsAsync` и обновляет `FilePath` / `PresentationPath` в записи.
- `GetDownloadUrlAsync`: проверяем, что файл загружен (`FilePath != null`); генерируем presigned URL на скачивание (15 минут).

#### 4. Infrastructure

**`MinIO/MinioFileStorageService.cs`** — реализация `IFileStorageService`:
- NuGet: `AWSSDK.S3` или `Minio` (официальный SDK для MinIO).
- Конфигурация через `MinioOptions { Endpoint, AccessKey, SecretKey, BucketName }` в `appsettings.json` / секреты.
- Реализует генерацию presigned URL, проверку существования объекта, удаление.

**`Repositories/GraduateWorksRepository.cs`** — реализация `IGraduateWorksRepository`.

Зарегистрировать оба сервиса в `Infrastructure/DependencyInjection.cs`.

Обновить `ApplicationDbContext` — добавить `ApplicationId` в конфигурацию `GraduateWork`.

#### 5. API

**`Controllers/GraduateWorksController.cs`**

```
GET    /api/v1/graduate-works              → GetAllAsync         [Authorize]
GET    /api/v1/graduate-works/{id}         → GetByIdAsync        [Authorize]
POST   /api/v1/graduate-works              → CreateAsync         [Admin]
PUT    /api/v1/graduate-works/{id}         → UpdateAsync         [Admin]
DELETE /api/v1/graduate-works/{id}         → DeleteAsync         [Admin]
POST   /api/v1/graduate-works/{id}/upload-url/{fileType}   → GetUploadUrlAsync    [Admin]
POST   /api/v1/graduate-works/{id}/confirm-upload/{fileType} → ConfirmUploadAsync [Admin]
GET    /api/v1/graduate-works/{id}/download-url/{fileType} → GetDownloadUrlAsync  [Authorize]
```

`fileType` — строка `thesis` или `presentation`.

#### 6. Tests

**Unit: `GraduateWorksServiceTests`**
- Создание дубля по одной `ApplicationId` → `Validation`
- `GetUploadUrlAsync` для несуществующей записи → `NotFound`
- `ConfirmUploadAsync` когда объекта нет в хранилище → `Validation`
- `GetDownloadUrlAsync` когда `FilePath == null` → `Validation`

**Integration: `GraduateWorksIntegrationTests`** (MinIO через Testcontainers или mocked `IFileStorageService`)
- Полный CRUD; проверка политик (не-Admin не может создать/загрузить)

---

#### HTTP endpoints итога

```
GET    /api/v1/graduate-works
GET    /api/v1/graduate-works/{id}
POST   /api/v1/graduate-works
PUT    /api/v1/graduate-works/{id}
DELETE /api/v1/graduate-works/{id}
POST   /api/v1/graduate-works/{id}/upload-url/{fileType}
POST   /api/v1/graduate-works/{id}/confirm-upload/{fileType}
GET    /api/v1/graduate-works/{id}/download-url/{fileType}
```

---

### Итерация 6а — «Уведомления (email + таблица Notifications)» — **завершено (2026-04-14)**

#### Фактически реализовано

- Реализован Inbox API:
  - `GET /api/v1/notifications`
  - `PUT /api/v1/notifications/{id}/read`
  - `PUT /api/v1/notifications/read-all`
- Реализованы `IEmailSender`, `IEmailTaskChannel`, `EmailBackgroundService`, `LogEmailSender`, `SmtpEmailSender`.
- Включена фоновая email-очередь на `BackgroundService + Channel`.
- Подключены уведомления в потоках:
  - `SupervisorRequests`: create/approve/reject/cancel;
  - `StudentApplications`: create/approve (уведомление завкафу)/reject/department-head-approve/department-head-reject.
- Добавлены новые типы уведомлений в SQL seed (`SupervisorRequestCreated`, `ApplicationSubmittedToSupervisor`, `ApplicationSubmittedToDepartmentHead`).
- **Итерация 6б (2026-04-19):** см. отдельный подраздел «Итерация 6б» ниже в этом же документе — `NewMessage`, `GraduateWorkUploaded`, `CreateAndSaveAsync`.

#### Контекст и решения

- Уведомления создаются **атомарно в той же транзакции** БД, что и бизнес-действие (смена статуса, новое сообщение и т.д.).
- Фоновая email-отправка — через **`BackgroundService` + `Channel<T>`**: сервис пишет задачу в канал, фоновый воркер читает и отправляет. Бэкенд не блокируется.
- Email-отправка: интерфейс `IEmailSender` + `LogEmailSender` (заглушка для dev/test, пишет в лог) + `SmtpEmailSender` (продакшн).
- Inbox API: пользователь видит свои уведомления и может их отмечать.

#### 1. SQL

Схема уже готова: `infra/db/init/20_create_notifications.sql`.

**Обновить `05_create_notification_types.sql`** — добавить новые типы:
```sql
INSERT INTO "NotificationTypes" ("CodeName", "DisplayName") VALUES
('ApplicationStatusChanged',       'Статус заявки изменён'),
('NewMessage',                     'Новое сообщение'),
('TopicApproved',                  'Тема утверждена'),
('TopicRejected',                  'Тема отклонена'),
('SupervisorRequestStatusChanged', 'Статус запроса на научрука изменён'),
('GraduateWorkUploaded',           'ВКР загружена в архив');
```

Проверить индексы в `23_create_indexes.sql` — убедиться, что присутствуют:
```sql
CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_IsRead"
    ON "Notifications" ("UserId", "IsRead") WHERE "IsRead" = FALSE;
CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_CreatedAt"
    ON "Notifications" ("UserId", "CreatedAt" DESC);
```

#### 2. Domain

Сущность `Notification` уже готова. Изменений не требуется.

#### 3. Application

**`Notifications/Contracts.cs`**
```
CreateNotificationCommand  { UserId, TypeCodeName, Title, Content }
NotificationDto            { Id, TypeCodeName, TypeDisplayName, Title, Content, IsRead, CreatedAt }
NotificationsFilterQuery   { IsRead?, Page, PageSize }
```

**`Notifications/INotificationsService.cs`**
```
GetForCurrentUserAsync(filter, userId)     → PagedResult<NotificationDto>
MarkAsReadAsync(notificationId, userId)    → void
MarkAllAsReadAsync(userId)                 → void
CreateAsync(command)                       → Notification?  // Add в контекст; вызывается из других сервисов
CreateAndSaveAsync(command)                → Notification?  // Create + SaveChanges (для 6б после отдельного commit чата/файла)
```

**`Abstractions/INotificationsRepository.cs`**
```
GetByUserIdAsync(userId, filter)           → (IReadOnlyList<Notification>, int total)
GetByIdAsync(id)                           → Notification?
AddAsync(notification)                     → Notification
MarkAsReadAsync(notificationId, userId)    → void
MarkAllAsReadAsync(userId)                 → void
```

**`Abstractions/IEmailSender.cs`**
```csharp
Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
```

**`Abstractions/IEmailTaskChannel.cs`** — обёртка над `Channel<EmailTask>`:
```
WriteAsync(task)  → ValueTask
ReadAsync()       → IAsyncEnumerable<EmailTask>
```

Структура `EmailTask { ToEmail, Subject, Body }`.

**Интеграция уведомлений в существующие сервисы:**

В тех сервисах, где происходят ключевые события, добавить вызов `_notificationsService.CreateAsync(...)` **внутри той же транзакции**:

| Сервис | Событие | Получатель | Тип |
|--------|---------|------------|-----|
| `SupervisorRequestsService.ApproveAsync` | Запрос одобрен | Student | `SupervisorRequestStatusChanged` |
| `SupervisorRequestsService.RejectAsync` | Запрос отклонён | Student | `SupervisorRequestStatusChanged` |
| `StudentApplicationsService.ApproveAsync` | Заявка одобрена научруком | Student | `ApplicationStatusChanged` |
| `StudentApplicationsService.RejectAsync` | Заявка отклонена | Student | `ApplicationStatusChanged` |
| `StudentApplicationsService.DepartmentHeadApproveAsync` | Одобрено зав. кафедрой | Student | `ApplicationStatusChanged` |
| `StudentApplicationsService.DepartmentHeadRejectAsync` | Отклонено зав. кафедрой | Student | `ApplicationStatusChanged` |
| `ChatMessagesService.SendMessageAsync` | Новое сообщение | Получатель (второй участник чата) | `NewMessage` |
| `GraduateWorksService.ConfirmUploadAsync` | Файл ВКР подтверждён в хранилище | Student (`UserId` по профилю) | `GraduateWorkUploaded` |

После создания `Notification` в БД — запись `EmailTask` в `Channel` для фоновой отправки.

#### 4. Infrastructure

**`Email/LogEmailSender.cs`** — реализация `IEmailSender`, пишет в `ILogger` (dev/test).

**`Email/SmtpEmailSender.cs`** — реализация `IEmailSender` через `SmtpClient` / MailKit.  
Конфигурация через `SmtpOptions { Host, Port, Username, Password, FromAddress }` в `appsettings.json`.

**`Email/EmailTaskChannel.cs`** — реализация `IEmailTaskChannel` поверх `System.Threading.Channels.Channel<T>` (unbounded или bounded с DropOldest).

**`Email/EmailBackgroundService.cs`** — `BackgroundService`:
```csharp
protected override async Task ExecuteAsync(CancellationToken ct)
{
    await foreach (var task in _channel.ReadAsync(ct))
    {
        await _emailSender.SendAsync(task.ToEmail, task.Subject, task.Body, ct);
    }
}
```
При ошибке отправки — логировать и продолжать (письмо теряется; retry — вне scope MVP).

**`Repositories/NotificationsRepository.cs`** — реализация `INotificationsRepository`.

Зарегистрировать в `Infrastructure/DependencyInjection.cs`:
- `IEmailSender` выбирается через `Email:Provider` (`Log` или `Smtp`) в конфигурации.
- `IEmailTaskChannel` → `EmailTaskChannel` (Singleton).
- `EmailBackgroundService` → `AddHostedService`.
- `INotificationsRepository` → `NotificationsRepository`.

#### 5. API

**`Controllers/NotificationsController.cs`**

```
GET  /api/v1/notifications              → GetForCurrentUserAsync   [Authorize]
PUT  /api/v1/notifications/{id}/read    → MarkAsReadAsync          [Authorize]
PUT  /api/v1/notifications/read-all     → MarkAllAsReadAsync       [Authorize]
```

Пагинация стандартная: `?page=1&pageSize=20&isRead=false`.

#### 6. Tests

**Unit: `NotificationsServiceTests`**
- `MarkAsReadAsync` чужого уведомления → `Forbidden`
- `GetForCurrentUserAsync` с фильтром `isRead=false` возвращает только непрочитанные
- `CreateAsync` с несуществующим `TypeCodeName` → `Validation`

**Unit: `EmailBackgroundServiceTests`**
- добавлены:
  - вызов `IEmailSender.SendAsync` при поступлении задачи в канал;
  - продолжение обработки очереди после ошибки отправки отдельного письма.

**Integration: `NotificationsIntegrationTests`**
- добавлены:
  - создание `SupervisorRequest` создаёт уведомление преподавателю в Inbox;
  - `PUT /notifications/{id}/read` возвращает `403` для чужого уведомления;
  - `PUT /notifications/read-all` отмечает только уведомления текущего пользователя.

#### Итерация 6б — расширение уведомлений (чат + ВКР) — **завершено (2026-04-19)**

- **`ChatMessagesService.SendMessageAsync`**: после сохранения сообщения — `CreateAndSaveAsync` с типом `NewMessage` для адресата, затем `EnqueueEmailAsync`.
- **`GraduateWorksService.ConfirmUploadAsync`**: после успешного `SaveChanges` записи ВКР — уведомление студенту с типом `GraduateWorkUploaded` (через `IGraduateWorksRepository.GetStudentUserIdByStudentProfileIdAsync`), затем email в очередь.
- **Код**: `NotificationTypeCodes` (`NewMessage`, `GraduateWorkUploaded` и пр.); `INotificationsService.CreateAndSaveAsync`.
- **Тесты**: unit (`ChatMessagesServiceTests`, `GraduateWorksServiceTests`, `NotificationsServiceTests`); интеграция — `Chat_StudentPost_CreatesNewMessageNotificationForTeacher`, `ConfirmUpload_CreatesGraduateWorkUploadedNotificationForStudent`; сид типов в тестах — `NotificationTypesTestSeed`.

---

#### HTTP endpoints итога

```
GET  /api/v1/notifications
PUT  /api/v1/notifications/{id}/read
PUT  /api/v1/notifications/read-all
```

## 5) Данные и доступ к данным (PostgreSQL)

- **EF Core** поверх SQL-схемы; при изменении схемы — правки в `infra/db/init` и синхронизация модели.
- **Конкурентность** для заявок/тем: транзакции, уникальные ограничения, при необходимости `FOR UPDATE` / оптимистичная блокировка (см. `DevelopmentPlan.md` и индексы в `23_create_indexes.sql`).

## 6) Контейнеризация и локальная проверка

- **Backend + DB + Redis**: `infra/docker/compose.backend.yml`
- **Порты (хост):**
  - API: **`http://localhost:5001`** (Swagger: `/swagger`)
  - PostgreSQL: **`localhost:5433`**
  - Redis: **`localhost:6380`**
- **Секреты:** `infra/docker/secrets/postgres_password.txt`, `redis_password.txt`

## 7) Тестирование

- **Unit:** бизнес-правила в сервисах (справочники, auth, application actions).
- **Integration:** контроллеры + реальные Postgres/Redis (Testcontainers) — требуется Docker.
- Контракты JSON для фронта — по мере стабилизации DTO.

## 8) Definition of Done (чек‑лист готовности продукта)

- API поднимается через Dockerfile/compose
- Swagger отражает актуальные контракты
- Критические операции с валидацией и авторизацией
- Смена статусов заявок — транзакции и инварианты не нарушаются
- Healthchecks и логи для диагностики (`/health`, защищённый `/health/db` при необходимости)
- **Передача на фронт:** CORS под dev-origin, по желанию — черновик контрактов или ссылка на Swagger; хвосты из подраздела **«Приоритеты перед передачей на фронт»** (§1)

---

*При существенных изменениях в репозитории обновляйте §1 и таблицу в §3.*
