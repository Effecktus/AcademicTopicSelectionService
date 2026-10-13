# Сервис по выбору научного руководителя и темы ВКР

**Дипломный проект** — Ильин Айдар Альбертович

[![GitHub](https://img.shields.io/badge/GitHub-Effecktus%2FAcademicTopicSelectionService-181717?logo=github)](https://github.com/Effecktus/AcademicTopicSelectionService)
![.NET 10](https://img.shields.io/badge/.NET%2010-512BD4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL%2016-336791?logo=postgresql&logoColor=white)
![Angular](https://img.shields.io/badge/Angular%2020-DD0031?logo=angular&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)
![Status](https://img.shields.io/badge/статус-MVP%20завершён-brightgreen)

---

## О проекте

Веб-приложение для автоматизации процесса выбора научного руководителя и темы выпускной квалификационной работы (ВКР) в университете.

Система охватывает полный цикл — от публикации тем преподавателями до финального утверждения заявки заведующим кафедрой, а также хранит архив защищённых ВКР прошлых лет.

### Роли пользователей

| Роль | Возможности |
|------|-------------|
| **Студент** | Выбор преподавателя и темы, подача заявки, общение с руководителем в чате, просмотр архива ВКР |
| **Преподаватель** | Управление темами, одобрение/отклонение заявок, общение со студентами в чате |
| **Заведующий кафедрой** | Финальное утверждение или отклонение заявок кафедры, аналитика |
| **Администратор** | Управление пользователями, загрузка ВКР в архив, аналитика и экспорт |

### Процесс выбора темы

```
Студент выбирает научного руководителя → Преподаватель принимает/отклоняет запрос
        ↓ (при принятии)
Студент создаёт заявку на тему (OnEditing) → редактирует, выбирает или предлагает тему
        ↓ (подача научруку)
Студент отправляет заявку научруку (Pending) → Научрук одобряет, отклоняет или возвращает на доработку
        ↓ (при одобрении)
Заявка сразу попадает к заведующему (PendingDepartmentHead) → Заведующий утверждает, отклоняет или возвращает на доработку
        ↓ (при утверждении)
    Процесс завершён. Дальнейшая работа над ВКР — вне системы.
        ↓ (после защиты)
Администратор загружает ВКР в архив
```

---

## Технологический стек

| Компонент | Технология |
|-----------|-----------|
| Backend | ASP.NET Core 10 (Web API, Clean Architecture) |
| База данных | PostgreSQL 16 + EF Core |
| Кэширование | Redis 7 (refresh-токены) + `IMemoryCache` |
| Файловое хранилище | MinIO (dev) / AWS S3 (prod), presigned URL |
| Авторизация | JWT + Refresh Tokens (httpOnly cookie) |
| Email-уведомления | SMTP + BackgroundService + Channel |
| Frontend | Angular 20 + TypeScript + SCSS + PrimeNG |
| Контейнеризация | Docker + Docker Compose |

---

## Архитектура

Clean Architecture, четыре слоя:

```
backend/src/
├── Domain/          # Доменные сущности, IAuditableEntity
├── Application/     # Бизнес-логика, сервисы, DTO, абстракции репозиториев
├── Infrastructure/  # EF Core, репозитории, S3, Redis, Email
├── Migrations.Postgres/  # Миграции EF Core для PostgreSQL
└── API/             # Контроллеры, Swagger, DI-конфигурация
```

Источник истины по схеме БД — модель EF Core (`Infrastructure/Data/ApplicationDbContext.cs`) и миграции в `Migrations.Postgres`. Подробнее — в разделе [База данных: миграции и данные](#база-данных-миграции-и-данные).

---

## Запуск для разработки

### Требования

- [Docker](https://docs.docker.com/get-docker/) установлен и запущен
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 1. Подготовка секретов

```bash
echo "your_secure_password" > infra/docker/secrets/postgres_password.txt
echo "your_redis_password"  > infra/docker/secrets/redis_password.txt
echo "minioadmin"           > infra/docker/secrets/minio_access_key.txt
echo "minioadmin123"        > infra/docker/secrets/minio_secret_key.txt
echo "your_admin_password"  > infra/docker/secrets/admin_password.txt
```

`admin_password.txt` — пароль администратора `z_admin@example.com`. Он задаётся при первом старте API и потом не перезаписывается.

### 2. Запуск Backend (API + PostgreSQL + Redis + MinIO)

Перед запуском заполните `infra/docker/.env` для SMTP:

```env
EMAIL_PROVIDER=Smtp
SMTP_HOST=smtp.yandex.ru
SMTP_PORT=587
SMTP_ENABLE_SSL=true
SMTP_USERNAME=your_mail@yandex.ru
SMTP_PASSWORD=your_external_app_password
SMTP_FROM_ADDRESS=your_mail@yandex.ru
```

```bash
cd infra/docker
docker compose -f compose.backend.yml up --build -d
```

Доступные адреса:

| Сервис | Адрес |
|--------|-------|
| API + Swagger | `http://localhost:5001/swagger` |
| Health | `http://localhost:5001/health` |
| PostgreSQL | `localhost:5433` |
| Redis | `localhost:6380` |
| MinIO Console | `http://localhost:9001` |

### 3. Быстрый dev-флоу (hot-reload без пересборки образа)

```bash
# Поднять только БД + Redis + MinIO
cd infra/docker && docker compose -f compose.db.yml up -d

# Запустить API с watch
.\backend\run-watch.ps1
```

### 4. Полный стек (backend + frontend)

```bash
cd infra/docker
docker compose -f compose.dev.yml up --build -d
```

Frontend будет доступен на `http://localhost:4200`.

### База данных: миграции и данные

Данные в БД делятся на два вида:

| | Что входит | Как попадает в БД |
|---|---|---|
| **Обязательное наполнение** | Схема, справочники (статусы, роли, степени и т.д.), учётная запись администратора. Id фиксированы (`Infrastructure/Data/SystemSeed.cs`) | Миграциями EF Core |
| **Тестовые данные** | Кафедры, группы, пользователи, темы, заявки, ВКР | Только отдельной командой |

**Миграции в dev** применяются автоматически при старте API (`Db:ApplyMigrationsOnStartup = true` в Development).
При самом первом старте на пустой БД EF пишет в лог `Failed executing DbCommand ... __EFMigrationsHistory` — это нормально: таблицы истории миграций ещё нет, EF создаёт её следующим шагом.

**Пароль администратора** берётся из `Admin:PasswordFile` (в Docker — секрет `admin_password`) или `Admin:Password`. Миграция создаёт администратора без пароля, API устанавливает пароль при первом старте и больше его не трогает.

**Тестовые данные** (запускать после того, как API применил миграции; повторный запуск пересоздаёт тестовые данные, справочники и администратор не затрагиваются):

```bash
cd infra/docker
docker compose -f compose.dev.yml --profile seed run --rm db-seed   # или compose.backend.yml / compose.db.yml
```

Учётные записи из тестовых данных перечислены в шапке `infra/db/test_data/99_seed_test_data.sql`.

**Новая миграция** (из каталога `backend/`, после изменения модели в `ApplicationDbContext`):

```bash
dotnet tool restore
dotnet ef migrations add <ИмяМиграции> -p src/AcademicTopicSelectionService.Migrations.Postgres -s src/AcademicTopicSelectionService.API
```

**Production.** Миграции не применяются при старте (backend запускается в нескольких репликах). Перед выкладкой новой версии сгенерировать идемпотентный скрипт и выполнить его на БД:

```bash
dotnet ef migrations script --idempotent -p src/AcademicTopicSelectionService.Migrations.Postgres -s src/AcademicTopicSelectionService.API -o migrate.sql
psql -h <host> -U app_user -d app_database -v ON_ERROR_STOP=1 -f migrate.sql
```

**Переход со старой схемы.** БД, созданную прежними SQL-скриптами (`infra/db/init`), нужно пересоздать: `docker compose -f compose.dev.yml down -v` удалит volume с данными, при следующем `up` API создаст схему миграциями.

### Пересоздание БД с нуля

```bash
.\infra\docker\recreate-database.ps1
```

---

## API

Все бизнес-эндпоинты: `/api/v1/...`

Swagger: `http://localhost:5001/swagger`  
Markdown-документация: [`docs/api/`](docs/api/)

### Основные эндпоинты

| Группа | Эндпоинты |
|--------|-----------|
| Auth | `POST /api/v1/auth/login`, `/refresh`, `/logout` |
| Пользователи | `GET/POST /api/v1/users` (Admin) |
| Преподаватели / Студенты | `GET /api/v1/teachers`, `/students` (и `/{id}`) |
| Темы | `GET/POST/PUT/PATCH/DELETE /api/v1/topics` |
| Запросы на научрука | `GET/POST/PUT /api/v1/supervisor-requests` |
| Заявки на тему | `GET/POST/PUT /api/v1/applications` |
| Чат | `GET/POST/PUT /api/v1/applications/{id}/messages` |
| Архив ВКР | `GET/POST/PUT/DELETE /api/v1/graduate-works` (+ upload/download URL) |
| Уведомления | `GET/PUT /api/v1/notifications` |
| Аналитика и экспорт | `GET /api/v1/admin/analytics`, `/admin/export` (Admin) |
| Справочники (10 шт.) | `/api/v1/user-roles`, `/application-statuses`, `/topic-statuses` и др. |

Подробнее: [`docs/api/v1.endpoints.md`](docs/api/v1.endpoints.md)

---

## Статусы заявок

```
                    ┌──────────────────────────────────────────┐
                    │         вернуть на доработку             │
                    │         [Научрук / Завкаф]               │
                    ▼                                          │
[создать]→ OnEditing → [отправить] → Pending ──────────────── ┘
               │                       │
          [отменить]              [отменить]
               │         ┌────── [отклонить Научрук] → RejectedBySupervisor
               ▼         │
           Cancelled     └────── [одобрить Научрук] → PendingDepartmentHead
                                                              │
                          ┌─────────────────────────────── [отклонить Завкаф]
                          ▼                                   │
              ApprovedByDepartmentHead ✅      RejectedByDepartmentHead
```

Студент может отменить заявку из статусов `OnEditing` и `Pending`.  
Чат доступен в статусах `OnEditing`, `Pending`, `ApprovedBySupervisor`, `PendingDepartmentHead`. После решения заведующего чат закрывается, история сохраняется.

---

## Лицензия

[MIT](LICENSE)
