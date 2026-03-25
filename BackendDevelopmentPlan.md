# План разработки Backend (ASP.NET Core 10) для проекта «AcademicTopicSelectionService»

Документ — **отдельный, backend-ориентированный план**, составленный на базе `DevelopmentPlan.md` и текущей структуры репозитория.

## 0) Цель и границы backend

### Цель
- Реализовать Web API для сервиса выбора научного руководителя/темы ВКР, чата (polling), архива ВКР и админ‑функций.
- Обеспечить авторизацию (JWT + refresh), роли, аудит, валидацию, стабильную работу с PostgreSQL и инфраструктурные интеграции.

### Границы (что именно делает backend)
- **REST API** для фронта/клиентов
- **Бизнес‑логика** (правила статусов заявок, доступность чата, ограничения по темам и т.п.)
- **Доступ к данным** (PostgreSQL)
- Интеграции: **Redis** (кэш/сессии/отзыв токенов), **S3/MinIO** (файлы), **SMTP** (уведомления)
- Тех.обвязка: логирование, healthchecks, swagger, метрики (опционально)

### Вне scope на старте (можно отложить)
- Полный мониторинг Prometheus/Grafana и дашборды
- Полноценная аналитика/экспорт (можно сделать после базовых сущностей)
- Тонкая оптимизация запросов (после появления профиля нагрузки)

## 1) Текущий статус репозитория (важно)

### Уже есть
- SQL‑скрипты создания схемы и справочников: `infra/db/init/*.sql`
- Тестовые данные: `infra/db/test_data/99_seed_test_data.sql`
- Compose‑файлы общего стека: `infra/docker/compose.*.yml`
- Каркас backend (5 проектов, Clean Architecture): `backend/src/*` и Solution `backend/AcademicTopicSelectionService.slnx`
  - `Domain` — доменные сущности, `IAuditableEntity`
  - `Application` — сервисы, DTO, абстракции (интерфейсы репозиториев, `IDatabaseHealthChecker`)
  - `Infrastructure` — EF Core `ApplicationDbContext`, реализации репозиториев
  - `API` — контроллеры, Swagger, DI-конфигурация
- Проверки здоровья API:
  - `GET /health`
  - `GET /health/db` (через абстракцию `IDatabaseHealthChecker`)
- Версионирование API в URL: `/api/v1/...`
- CRUD всех 7 справочников (`user-roles`, `application-statuses`, `topic-statuses`, `notification-types`, `academic-degrees`, `academic-titles`, `positions`)
  - Swagger: `http://localhost:5000/swagger`
  - Markdown-документация: `docs/api/`
- Unit-тесты (255 тестов) и интеграционные тесты

### Принцип по БД (источник истины)
Сейчас источник истины по схеме БД — **SQL‑скрипты** в `infra/db/init` (БД поднимается через Docker и применяет init‑скрипты при первом старте volume).

EF Core используется как ORM поверх существующей схемы:
- модель (`DbContext` + сущности) генерируется scaffold’ом из реальной БД в `AcademicTopicSelectionService.Infrastructure`;
- миграции EF Core пока **не используем**, чтобы не вести 2 параллельных источника истины.

## 2) Архитектура решения и слоёв (Clean Architecture)

Структура проекта (совпадает по смыслу с `DevelopmentPlan.md`):

```
backend/
  AcademicTopicSelectionService.slnx
  Dockerfile
  src/
    AcademicTopicSelectionService.Domain/         # Ядро: доменные сущности (POCO), IAuditableEntity
    AcademicTopicSelectionService.Application/    # Use-cases/сервисы, DTO/контракты, абстракции репозиториев
    AcademicTopicSelectionService.Infrastructure/ # EF Core (DbContext, Fluent API), репозитории, внешние интеграции
    AcademicTopicSelectionService.API/            # Web API (Controllers, Swagger, health, DI-конфигурация)
```

### Правило зависимостей (Dependency Rule — стрелки направлены внутрь)
- `Domain` — не зависит ни от чего (самый внутренний слой)
- `Application` → `Domain`
- `Infrastructure` → `Application`, `Domain`
- `API` → `Application`, `Infrastructure` (Infrastructure — только для DI-регистрации)

## 3) Базовые нефункциональные требования

- **Безопасность**: bcrypt/argon2 для паролей (если будет хранение), JWT, ограничение ролей, CORS.
- **Ошибки**: единый формат ошибок (ProblemDetails), трассировка request-id.
- **Валидация**: FluentValidation на входных DTO.
- **Версионирование API**: URL‑версионирование `/api/v{version}/...` (сейчас используем `v1`).
- **Логи**: структурированные (Serilog рекомендовано), уровни логирования.
- **Документация**: Swagger/OpenAPI обязателен.
- **Middleware (планируется)**: ErrorHandling, JWT/Authorization и др. (контроллеры должны оставаться тонкими).

## 4) MVP-итерация (чтобы API уже было полезно)

### 4.1. Итерация 0 — «Скелет + подключение к БД»
Готово/цель:
- `GET /health`, `GET /health/db`
- Dockerfile для сборки API
- Compose для поднятия `backend + postgres` (для smoke тестов)

Проверка:
- **Dev-флоу (рекомендовано)**: Postgres в Docker + API на хосте через watch:
  - `.\backend\run-watch.ps1`
  - `GET http://localhost:5000/health`
  - `GET http://localhost:5000/health/db`
  - Swagger: `http://localhost:5000/swagger`
- **Smoke в Docker**: `docker compose -f infra/docker/compose.backend.yml up --build -d`

### 4.2. Итерация 1 — «Справочники + чтение доменных данных»
Сделать:
- Модели/маппинг (EF Core или Dapper) для ключевых таблиц: Users, Departments, Teachers, Students, Topics
- Read-only endpoints:
  - `GET /api/teachers`
  - `GET /api/teachers/{id}`
  - `GET /api/topics` (фильтры/пагинация)
  - `GET /api/topics/{id}`
- Пагинация (page/pageSize) и сортировка везде, где есть списки

Тест-план:
- запустить БД с тестовыми данными (ручной прогон seed скрипта или отдельная dev-инициализация)
- проверить, что списки возвращаются и фильтры работают

### 4.3. Итерация 2 — «JWT Auth + роли»
Сделать:
- Логин (`POST /api/auth/login`) + выпуск access/refresh
- Refresh (`POST /api/auth/refresh`) и logout (`POST /api/auth/logout`)
- Роли из `UserRoles` + политики авторизации

Вопрос хранения refresh:
- В БД уже есть таблица `RefreshTokens`. Для MVP можно хранить токены в БД.
- Redis — как следующий шаг (кэш/отзыв/blacklist), чтобы не ходить в БД на каждую проверку.

### 4.4. Итерация 3 — «Заявки + жизненный цикл статусов»
Сделать:
- CRUD/операции для `StudentApplications` (API-ресурс может называться `/applications`) по правилам из общего плана:
  - создание студентом
  - approve/reject преподавателем
  - approve/reject заведующим
  - cancel студентом (до отправки заведующему)
- Инварианты:
  - тема закрепляется за первым студентом (конкурентность!)
  - чат доступен только до финального статуса заведующего

### 4.5. Итерация 4 — «Чат (polling)»
Сделать:
- `GET /api/chat/applications/{applicationId}/messages` (пагинация)
- `POST /api/chat/messages`
- `PUT /api/chat/messages/{id}/read`
- ограничения по статусу заявки

### 4.6. Итерация 5 — «Архив ВКР + файлы (S3/MinIO)»
Сделать:
- `GET /api/vkr`, `GET /api/vkr/{id}`
- upload/download (только админ на upload)
- Интеграция с MinIO в dev (S3 in prod)

### 4.7. Итерация 6 — «Уведомления (email + внутр.таблица Notifications)»
Сделать:
- запись уведомлений в БД
- отложенная отправка email (BackgroundService + очередь)
- триггеры: смена статуса заявки, непрочитанные сообщения > N минут

## 5) Данные и доступ к данным (PostgreSQL)

### 5.1. Стратегия доступа к данным (текущее состояние): EF Core + scaffold модель
Плюсы:
- быстро получить рабочую модель всех таблиц (учитывая `citext`, кавычки и связи)
- меньше ручного маппинга на старте
Минусы:
- scaffold может перегенерировать файлы при изменениях схемы (нужно дисциплинированно обновлять модель)

### 5.2. Конкурентность и блокировки (обязательно для «первый занял тему»)
Минимум:
- транзакции на уровне создания заявки/закрепления темы
- уникальные ограничения/проверки в БД (если нужно — добавить индекс/constraint)
Опционально:
- `SELECT ... FOR UPDATE` или оптимистичная конкуренция (rowversion/updated_at + проверка)

## 6) Контейнеризация и локальная проверка

### Backend + DB compose (smoke)
- Файл: `infra/docker/compose.backend.yml`
- Сервисы: `postgres`, `backend`
- Порты:
  - API: `http://localhost:5000`
  - Postgres: `localhost:5433`

### Секреты
- `infra/docker/secrets/postgres_password.txt` — пароль БД

## 7) Тестирование

Минимальный набор:
- Unit: бизнес‑правила статусов (ApplicationService)
- Integration: API endpoints + real Postgres (через Testcontainers или docker compose)
- Контрактные: проверить JSON-схемы ответов для фронта

## 8) Definition of Done (чек‑лист готовности)

- API поднимается через Dockerfile/compose
- Swagger доступен и отражает актуальные контракты
- Для критических операций есть валидация и авторизация
- Для операций изменения статуса — транзакции/инварианты не ломаются
- Есть healthchecks и базовые логи

