# План разработки Backend (ASP.NET Core 10) для проекта «AcademicTopicSelectionService»

Документ — **отдельный, backend-ориентированный план**, составленный на базе `DevelopmentPlan.md` и **актуальной** структуры репозитория (обновлено: 2026-04).

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
- **Health**: `GET /health`, `GET /health/db` (`IDatabaseHealthChecker`).
- **Версионирование API**: `/api/v1/...` (Asp.Versioning, версия в URL).
- **Аутентификация**: `POST /api/v1/auth/login`, `register`, `refresh`, `logout`; access JWT (HMAC), refresh в **Redis** с ротацией; пароли через `IPasswordHasher` (BCrypt).
- **Авторизация**: fallback‑политика «только аутентифицированные»; справочники — чтение для вошедших пользователей, **изменение** — роль `Admin`; `GET` ролей (`/user-roles`) — публично (для регистрации); `ApplicationActions` — пока только «вошёл в систему» (узкие правила — позже).
- **Справочники (CRUD, `/api/v1/...`)**: user-roles, application-statuses, topic-statuses, notification-types, academic-degrees, academic-titles, positions, study-groups, topic-creator-types, application-action-statuses.
- **Доменный read API**: `GET /api/v1/teachers`, `GET …/{id}`; `GET /api/v1/topics`, `GET …/{id}` (фильтры `query`, `statusCodeName`, `createdByUserId`, `creatorTypeCodeName`, `sort`, пагинация); `GET /api/v1/students`, `GET …/{id}` (фильтры `query`, `groupId`). Все под `[Authorize]`; в списках преподавателей и студентов — только **активные** пользователи (`Users.IsActive`).
- **Доменный API (частично)**: `application-actions` — CRUD по действиям заявки; список **только с обязательным** `?applicationId=` (глобального списка нет).
- **Поток 1 (`SupervisorRequests`)**: реализованы endpoint-ы списка/деталей/создания/approve/reject/cancel, ограничения по ролям, атомарная авто-отмена альтернативных `Pending`-запросов студента при approve.
- **Поток 2 (`StudentApplications`)**: реализован с обязательным `SupervisorRequestId`; проверка научрука/заведующего и лимитов через `SupervisorRequest.TeacherUserId`.
- **Тесты**: unit-тесты сервисов (справочники, Auth, ApplicationActions) — **сотни** кейсов (точное число: `dotnet test`); интеграционные тесты API + PostgreSQL/Redis через Testcontainers (нужен Docker).
- **Документация API**: Swagger в Development; при необходимости — `docs/api/`.

### Принцип по БД (источник истины)

Источник истины по схеме — **SQL** в `infra/db/init`. EF Core — ORM поверх готовой схемы; **миграции EF** намеренно не ведём параллельно SQL (один источник истины).

### Что ещё не сделано (крупными блоками)

- Чат (polling): endpoints сообщений, ограничения по статусам заявки, read-механика.
- Архив ВКР + файлы (MinIO/S3): upload/download, права доступа, валидация файлов.
- Уведомления: запись бизнес-событий в `Notifications` + фоновая email-отправка.

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
| Роли и политики авторизации | Внедрено (дальше — уточнение по сущностям) |
| CORS | По необходимости в `Program.cs` |
| Единый формат ошибок (ProblemDetails) | Используется |
| Версионирование API `/api/v1` | Внедрено |
| Swagger / OpenAPI | Внедрено |
| FluentValidation | **Не подключён**; валидация в сервисах / атрибутах — по мере необходимости |
| Serilog / структурированные логи | По желанию (усилить позже) |

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

- Login / register / refresh / logout, роли из `UserRoles`, политики на контроллерах.

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

- **Один активный запрос на студента** (нельзя отправить двум одновременно).
- После одобрения студент может создать заявку на тему (Поток 2).
- Статусы используются из общего справочника `ApplicationStatuses`.

**Поток 2 — Утверждение темы ВКР** (`StudentApplications`)

Требует одобренного `SupervisorRequest`. Тему создаёт **преподаватель** (в разделе тем) или **студент** прямо при подаче заявки (`CreatorType = "Student"`). Научрук в этом потоке берётся из связанного `SupervisorRequest`, а **не** из `Topics.CreatedBy`.

```
Студент → POST /api/v1/applications  (с topicId или предложением новой темы)
  Pending → approve               (Supervisor из SupervisorRequest) → ApprovedBySupervisor
  Pending → reject                (Supervisor)                      → RejectedBySupervisor
  ApprovedBySupervisor → submit-to-department-head (Supervisor)    → PendingDepartmentHead
  PendingDepartmentHead → department-head-approve  (DepartmentHead) → ApprovedByDepartmentHead
  PendingDepartmentHead → department-head-reject   (DepartmentHead) → RejectedByDepartmentHead
  Pending или ApprovedBySupervisor → cancel        (Student)        → Cancelled
  PendingDepartmentHead → cancel                                    → ЗАПРЕЩЕНО
```

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
   - `GET  /api/v1/supervisor-requests` (Student — свои; Teacher — входящие; Admin — все)
   - `GET  /api/v1/supervisor-requests/{id}`
   - `POST /api/v1/supervisor-requests` (Student)
   - `PUT  /api/v1/supervisor-requests/{id}/approve` (Teacher)
   - `PUT  /api/v1/supervisor-requests/{id}/reject`  (Teacher, Comment обязателен)
   - `PUT  /api/v1/supervisor-requests/{id}/cancel`  (Student)
6. **Tests:**
   - Unit: `SupervisorRequestsServiceTests` — approve отменяет остальные; лимит; дубль к тому же преподавателю; reject без комментария → Validation
   - Integration: `SupervisorRequestsIntegrationTests` — полный сценарий; конкурентное одобрение двух преподавателей.

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
PUT    /api/v1/applications/{id}/submit-to-department-head
PUT    /api/v1/applications/{id}/department-head-approve
PUT    /api/v1/applications/{id}/department-head-reject
PUT    /api/v1/applications/{id}/cancel
```

---

### Итерация 4 — «Чат (polling)» — **не начато**

- Endpoints для сообщений по `applicationId`, отметка прочитанного, ограничения по статусу заявки.

### Итерация 5 — «Архив ВКР + файлы (S3/MinIO)» — **не начато**

- Выдача/загрузка ВКР, права (например upload только админ).

### Итерация 6 — «Уведомления (email + таблица Notifications)» — **не начато**

- Запись в БД + фоновая отправка email по событиям.

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
- Healthchecks и логи для диагностики

---

*При существенных изменениях в репозитории обновляйте §1 и таблицу в §3.*
