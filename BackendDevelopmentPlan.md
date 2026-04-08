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
- **Тесты**: unit-тесты сервисов (справочники, Auth, ApplicationActions) — **сотни** кейсов (точное число: `dotnet test`); интеграционные тесты API + PostgreSQL/Redis через Testcontainers (нужен Docker).
- **Документация API**: Swagger в Development; при необходимости — `docs/api/`.

### Принцип по БД (источник истины)

Источник истины по схеме — **SQL** в `infra/db/init`. EF Core — ORM поверх готовой схемы; **миграции EF** намеренно не ведём параллельно SQL (один источник истины).

### Что ещё не сделано (крупными блоками)

- CRUD и бизнес‑операции по **StudentApplications** (заявки) — итерация 3.
- Жизненный цикл заявок (approve/reject/cancel, конкуренция «первый занял тему»).
- Чат (polling), архив ВКР + файлы (MinIO), фоновые уведомления (email + таблица `Notifications`).

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

### Итерация 3 — «Заявки + жизненный цикл статусов» — **не начато (бизнес-логика)**

**Согласовано с `DevelopmentPlan.md` §11 и README (ключевые правила):** отмена студентом **до** `PendingDepartmentHead`; после передачи заведующему отмена запрещена; при отмене тема снова доступна другим (`DevelopmentPlan.md` §11); тема закрепляется за **первым подавшим заявку** (`README`).

**Решения по умолчанию для backend (можно изменить только осознанно):**

1. **Два шага преподавателя, не один:** `PUT …/approve`: `Pending` → `ApprovedBySupervisor`. Отдельный `PUT …/submit-to-department-head` (только научрук темы): `ApprovedBySupervisor` → `PendingDepartmentHead`. Так сохраняются оба статуса из сида и совпадает смысл §10 `DevelopmentPlan` (чат в `ApprovedBySupervisor` и в `PendingDepartmentHead`). В `DevelopmentPlan.md` §6 второго маршрута нет — **добавить в OpenAPI/Swagger**; альтернатива (хуже для прозрачности): один `approve` сразу в `PendingDepartmentHead` — тогда статус `ApprovedBySupervisor` в MVP не используется (не рекомендуется).

2. **Конкуренция за тему (README):** при `POST` заявки — транзакция + `SELECT … FOR UPDATE` по строке `Topics` (или по `StudentApplications` с фильтром по `TopicId`). Проверка: нет другой заявки на эту тему в «активных» статусах (все кроме терминальных отказа/отмены — уточнить список в коде как константу). Partial unique index в SQL — **отдельная задача**, если после нагрузочных тестов остаются гонки.

3. **Заведующий:** пользователь с ролью `DepartmentHead`, у которого `Users.DepartmentId` **не NULL** и **равен** `DepartmentId` пользователя-научрука темы (`Topics.CreatedBy` → `Users.Id` → `DepartmentId`). Если у научрука `DepartmentId` NULL — `submit-to-department-head` и действия заведующего по этой заявке **недоступны** (HTTP **400** с телом ProblemDetails и кодом/сообщением для фронта); в списках заведующего такие заявки не показывать.

4. **Лимит `Teachers.MaxStudentsLimit`:** проверять при переводе в **`ApprovedByDepartmentHead`** (в этот момент студент окончательно закрепляется за научруком). Подсчёт: число заявок с финальным успехом / или связанная бизнес-модель — заложить в репозиторий один метод подсчёта «занятых слотов» по `Teachers.UserId` научрука.

5. **Идентификация пользователя:** `sub` в JWT = `Users.Id`. Студент: найти `Students` по `UserId == sub`. Преподаватель: `Teachers.UserId == sub`. Роль из JWT — вспомогательная; обязательна проверка фактов по БД (владелец темы, владелец заявки).

**Данные:** научрук темы — `Topics.CreatedBy`. Статусы заявки — `ApplicationStatuses` (`02_create_application_statuses.sql`). Журнал — `ApplicationActions` / `ApplicationActionStatuses`; любая смена `StudentApplications.StatusId` + запись в `ApplicationActions` только из сервиса заявок.

**Переходы (`(текущий CodeName, команда, актор)` → новый статус):**

| Было | Команда | Актор | Стало |
|------|---------|-------|--------|
| — | create | Student | `Pending` |
| `Pending` | approve | научрук (`Topics.CreatedBy`) | `ApprovedBySupervisor` |
| `Pending` | reject | научрук | `RejectedBySupervisor` |
| `ApprovedBySupervisor` | submit-to-department-head | научрук | `PendingDepartmentHead` |
| `PendingDepartmentHead` | department-head-approve | заведующий (п.3) | `ApprovedByDepartmentHead` |
| `PendingDepartmentHead` | department-head-reject | заведующий | `RejectedByDepartmentHead` |
| `Pending` или `ApprovedBySupervisor` | cancel | студент (`Students.UserId` = `sub`) | `Cancelled` |
| `PendingDepartmentHead` | cancel | — | запрет (`DevelopmentPlan.md` §11) |

Терминальные (команды не принимать): `RejectedBySupervisor`, `RejectedByDepartmentHead`, `Cancelled`, `ApprovedByDepartmentHead`.  
Каждый переход: одна транзакция — `UPDATE StudentApplications` + `INSERT ApplicationActions`; для reject — непустой `Comment` (уже есть CHECK в БД на непустоту при заполнении).

**HTTP:** базово `DevelopmentPlan.md` §6; **добавить** `PUT /api/v1/applications/{id}/submit-to-department-head` под п.1 (или задокументировать отказ от статуса `ApprovedBySupervisor` в MVP).

---

**Шаги (порядок работ):**

1. `Application`: DTO списка/карточки/команд; загрузка `ApplicationStatuses` по `CodeName` из БД (кэш в памяти на запрос или справочник в сервисе).
2. `Application`: абстракция репозитория заявок (фильтры для списка по роли: join `Students`/`Topics`/`Users`).
3. `Application`: сервис — методы проверки прав (`IsSupervisor`, `IsOwnerStudent`, `IsDepartmentHeadForTopic`) без смены статуса.
4. `Infrastructure`: реализация репозитория + `IDbContextTransaction` вокруг команд изменения.
5. `API`: `ApplicationsController` — только `GET` list + `GET` id; `[Authorize]`; проверки в п.3. Ручная проверка Swagger.
6. `Application`: `CreateAsync` — транзакция, проверки темы и гонки, insert заявки + первое `ApplicationAction`; зарегистрировать в DI.
7. `API`: `POST /api/v1/applications` — роль Student; интеграционный тест на успех.
8. По одному: метод сервиса + PUT + unit-тест на недопустимый переход для каждой строки таблицы переходов; коды 400/403/409 по смыслу.
9. Интеграционные: два `POST` на одну тему; полный сценарий до `ApprovedByDepartmentHead`; `cancel`.
10. Ограничить или убрать публичный CRUD `application-actions` из общего API (оставить только цепочку из сервиса заявок / Admin).

**Итерация 4:** разрешить отправку сообщений, если `StatusId` заявки ∈ {`Pending`, `ApprovedBySupervisor`, `PendingDepartmentHead`} — те же `CodeName`, что в БД.

---

### Итерация 4 — «Чат (polling)» — **не начато**

- Endpoints для сообщений по `applicationId`, отметка прочитанного, ограничения по статусу заявки.

### Итерация 5 — «Архив ВКР + файлы (S3/MinIO)» — **не начато**

- Выдача/загрузка ВКР, права (например upload только админ).

### Итерация 6 — «Уведомления (email + таблица Notifications)» — **не начато**

- Запись в БД + фоновая отправка email по событиям.

## 5) Данные и доступ к данным (PostgreSQL)

- **EF Core** поверх SQL-схемы; при изменении схемы — правки в `infra/db/init` и синхронизация модели.
- **Конкурентность** для заявок/тем: транзакции, уникальные ограничения, при необходимости `FOR UPDATE` / оптимистичная блокировка (см. `DevelopmentPlan.md` и индексы в `22_create_indexes.sql`).

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
