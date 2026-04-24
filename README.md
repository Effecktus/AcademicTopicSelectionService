# Сервис по выбору научного руководителя и темы ВКР

**Дипломный проект** — Ильин Айдар Альбертович

[![GitHub](https://img.shields.io/badge/GitHub-Effecktus%2FAcademicTopicSelectionService-181717?logo=github)](https://github.com/Effecktus/AcademicTopicSelectionService)
![.NET 10](https://img.shields.io/badge/.NET%2010-512BD4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL%2016-336791?logo=postgresql&logoColor=white)
![Angular](https://img.shields.io/badge/Angular%2020-DD0031?logo=angular&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)
![Status](https://img.shields.io/badge/статус-в%20разработке-yellow)

---

## О проекте

Веб-приложение для автоматизации процесса выбора научного руководителя и темы выпускной квалификационной работы (ВКР) в университете.

Система охватывает полный цикл — от публикации тем преподавателями до финального утверждения заявки заведующим кафедрой, а также хранит архив защищённых ВКР прошлых лет.

### Роли пользователей

| Роль | Возможности |
|------|-------------|
| **Студент** | Выбор преподавателя и темы, подача заявки, общение с руководителем в чате, просмотр архива ВКР |
| **Преподаватель** | Управление темами, одобрение/отклонение заявок, общение со студентами в чате |
| **Заведующий кафедрой** | Финальное утверждение или отклонение заявок кафедры |
| **Администратор** | Управление пользователями, загрузка ВКР в архив, аналитика и экспорт |

### Процесс выбора темы

```
Студент выбирает научного руководителя → Преподаватель принимает/отклоняет запрос
        ↓ (при принятии)
Студент подаёт заявку на тему → Научрук одобряет/отклоняет
        ↓ (при одобрении)
Заявка идёт заведующему кафедрой → Заведующий утверждает/отклоняет
        ↓ (при утверждении)
    Процесс завершён. Дальнейшая работа над ВКР — вне системы.
        ↓ (после защиты)
Администратор загружает ВКР в архив системы
```

**Ключевые правила:**
- Тема закрепляется за первым подавшим заявку студентом
- Преподаватель сам устанавливает лимит студентов
- Студент может отменить заявку только до передачи заведующему
- Чат между студентом и преподавателем доступен только до финального утверждения заведующим

---

## Технологический стек

| Компонент | Технология | Статус |
|-----------|-----------|--------|
| Backend | ASP.NET Core 10 (Web API) | ✅ Реализован |
| База данных | PostgreSQL 16 + EF Core | Схема готова |
| Кэширование | Redis 7 | ✅ Внедрено (refresh-токены) |
| Файловое хранилище | MinIO (dev) / AWS S3 (prod) | ✅ Внедрено (presigned URL) |
| Авторизация | JWT + Refresh Tokens | Внедрено |
| Email-уведомления | SMTP + BackgroundService + Channel | Внедрено |
| Frontend | Angular 18 + TypeScript + SCSS | 🔄 Запланировано |
| Контейнеризация | Docker + Docker Compose | ✅ Готово |
| Мониторинг | Prometheus + Grafana | 🔄 Запланировано |

---

## Архитектура

Проект построен по принципу **Clean Architecture** с четырьмя слоями:

```
backend/src/
├── AcademicTopicSelectionService.Domain/         # Ядро: доменные сущности, базовые интерфейсы (IAuditableEntity)
├── AcademicTopicSelectionService.Application/    # Бизнес-логика: сервисы, DTO, абстракции репозиториев
├── AcademicTopicSelectionService.Infrastructure/ # EF Core, репозитории, интеграции (S3, Redis)
└── AcademicTopicSelectionService.API/            # Web API: контроллеры, Swagger, DI-конфигурация
```

**Правило зависимостей** (стрелки направлены внутрь):
- `Domain` — не зависит ни от чего (центр архитектуры)
- `Application` → `Domain`
- `Infrastructure` → `Application`, `Domain`
- `API` → `Application`, `Infrastructure` (только для DI-регистрации)

**Источник истины по схеме БД** — SQL-скрипты в `infra/db/init/`. База поднимается через Docker и применяет скрипты автоматически при первом старте. EF Core используется как ORM поверх готовой схемы (scaffold).

### Структура инфраструктуры

```
infra/
├── db/
│   ├── init/           # SQL-скрипты создания схемы (00..19)
│   └── test_data/      # Тестовые данные для разработки
├── docker/
│   ├── compose.dev.yml         # Полный стек для разработки
│   ├── compose.db.yml          # Только PostgreSQL
│   ├── compose.backend.yml     # Backend + зависимости
│   ├── compose.prod.yml        # Production
│   └── secrets/                # Файлы с паролями (не коммитить!)
├── monitoring/
│   └── prometheus.yml
└── terraform/                  # IaC (планируется)
```

---

## Запуск для разработки

### Требования

- [Docker](https://docs.docker.com/get-docker/) установлен и запущен
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 1. Подготовка секретов

Создать файлы с паролями (не коммитить в Git):

```bash
echo "your_secure_password" > infra/docker/secrets/postgres_password.txt
echo "your_redis_password"  > infra/docker/secrets/redis_password.txt
echo "minioadmin"           > infra/docker/secrets/minio_access_key.txt
echo "minioadmin123"        > infra/docker/secrets/minio_secret_key.txt
```

### 2. Запуск только PostgreSQL (рекомендуется на старте)

Удобно для разработки схемы и тестирования запросов без поднятия всего стека:

```bash
cd infra/docker
docker compose -f compose.db.yml up -d
```

Подключение к БД:
- **psql:** `docker exec -it postgres_db psql -U app_user -d app_database`
- **pgAdmin / DBeaver:** хост `localhost`, порт `5433`, пользователь `app_user`, БД `app_database`

### 3. Запуск Backend (API + PostgreSQL + Redis)

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
- **API + Swagger:** `http://localhost:5001/swagger`
- **Health:** `http://localhost:5001/health`
- **Health DB:** `http://localhost:5001/health/db`
- **PostgreSQL:** `localhost:5433`
- **Redis:** `localhost:6380`
- **MinIO API:** `http://localhost:9000`
- **MinIO Console:** `http://localhost:9001`

Примечание: для корректных presigned URL в браузере используем `S3__PublicEndpoint` (по умолчанию `http://localhost:9000` в `compose.backend.yml`).

### 4. Рекомендуемый dev-флоу (быстрее, без пересборки образа)

PostgreSQL в Docker, API на хосте с hot-reload:

```bash
# Поднять только БД
cd infra/docker && docker compose -f compose.db.yml up -d

# Запустить API с watch (из корня проекта)
.\backend\run-watch.ps1
```

### 5. Полный стек (все сервисы)

```bash
cd infra/docker
docker compose -f compose.dev.yml up --build -d
```

| Сервис | Адрес |
|--------|-------|
| Frontend | `http://localhost:4200` |
| Backend API | `http://localhost:5001` (в `compose.dev.yml`, как в `compose.backend.yml`) |
| MinIO Console | `http://localhost:9001` |
| Prometheus / Grafana | временно отключены в `compose.dev.yml` |

### Остановка

```bash
docker compose -f compose.dev.yml down          # сохранить данные
docker compose -f compose.dev.yml down -v       # удалить данные (volumes)
```

---

## API

Все бизнес-эндпоинты версионируются через URL: `/api/v1/...`

Полная документация доступна через **Swagger** при запущенном API:
- `http://localhost:5001/swagger` (`compose.backend.yml` и `compose.dev.yml`)

Дополнительная Markdown-документация: [`docs/api/`](docs/api/)

### Реализованные эндпоинты

| Метод | Путь | Описание |
|-------|------|----------|
| `GET` | `/health` | Проверка доступности API |
| `GET` | `/health/db` | Проверка подключения к PostgreSQL |
| `POST` | `/api/v1/auth/login` | Вход |
| `POST` | `/api/v1/users` | Создание пользователя (Admin) |
| `POST` | `/api/v1/auth/refresh` | Обновление access-токена |
| `POST` | `/api/v1/auth/logout` | Выход (инвалидация refresh-токена) |
| `GET` | `/api/v1/teachers`, `/api/v1/teachers/{id}` | Каталог преподавателей |
| `GET` | `/api/v1/students`, `/api/v1/students/{id}` | Каталог студентов |
| `GET/POST/PUT/PATCH/DELETE` | `/api/v1/topics...` | Управление темами |
| `GET/POST/PUT` | `/api/v1/supervisor-requests...` | Поток выбора научрука |
| `GET/POST/PUT` | `/api/v1/applications...` | Поток утверждения темы |
| `GET/POST/PATCH/DELETE` | `/api/v1/application-actions...` | Действия по заявкам |
| `GET/POST/PUT` | `/api/v1/applications/{applicationId}/messages` | Чат студент ↔ преподаватель |
| `GET/POST/PUT/DELETE` | `/api/v1/graduate-works...` | Архив ВКР (список, создание, удаление) |
| `POST` | `/api/v1/graduate-works/{id}/upload-url/{fileType}` | Presigned URL для загрузки файла ВКР |
| `POST` | `/api/v1/graduate-works/{id}/confirm-upload/{fileType}` | Подтверждение загрузки файла ВКР |
| `GET` | `/api/v1/graduate-works/{id}/download-url/{fileType}` | Presigned URL для скачивания файла ВКР |
| `GET/PUT` | `/api/v1/notifications...` | Inbox уведомлений (список, read, read-all) |

Для каждого из 10 справочников реализован полный CRUD (GET список, GET по ID, POST, PUT, PATCH, DELETE):

| Справочник | Базовый путь |
|-----------|-------------|
| Роли пользователей | `/api/v1/user-roles` |
| Статусы заявок | `/api/v1/application-statuses` |
| Статусы действий по заявкам | `/api/v1/application-action-statuses` |
| Статусы тем | `/api/v1/topic-statuses` |
| Типы создателей тем | `/api/v1/topic-creator-types` |
| Типы уведомлений | `/api/v1/notification-types` |
| Учебные группы | `/api/v1/study-groups` |
| Учёные степени | `/api/v1/academic-degrees` |
| Учёные звания | `/api/v1/academic-titles` |
| Должности | `/api/v1/positions` |

### В разработке (следующие итерации)

<details>
<summary>Показать</summary>

**Аналитика и экспорт (Администратор)**
- `GET /api/v1/admin/analytics` 
—
 аналитика по кафедре
- `GET /api/v1/admin/export` 
—
 экспорт данных (Excel, CSV)

**Мониторинг**
- Подключение метрик к Prometheus / Grafana

</details>

---

## Статусы заявок

```
Pending → ApprovedBySupervisor → PendingDepartmentHead → ApprovedByDepartmentHead ✅
                ↓                                                  ↓
         RejectedBySupervisor                        RejectedByDepartmentHead
                                                           ↓
                                               (студент меняет тему и подаёт заново)

На любом этапе до PendingDepartmentHead → Cancelled (отмена студентом)
```

**Чат доступен** пока заявка в статусах: `Pending`, `ApprovedBySupervisor`, `PendingDepartmentHead`.  
После финального решения заведующего — чат закрывается, история сохраняется.

---

## Схема базы данных

PostgreSQL 16, схема создаётся SQL-скриптами при первом запуске контейнера.

**Справочники:** `UserRoles`, `ApplicationStatuses`, `ApplicationActionStatuses`, `TopicStatuses`, `TopicCreatorTypes`, `NotificationTypes`, `StudyGroups`, `AcademicDegrees`, `AcademicTitles`, `Positions`

**Основные сущности:** `Users`, `Departments`, `Teachers`, `Students`, `Topics`, `SupervisorRequests`, `StudentApplications`, `ApplicationActions`, `ChatMessages`, `GraduateWorks`, `Notifications`

Поля `CreatedAt` / `UpdatedAt` обрабатываются на уровне БД/EF Core в зависимости от сущности. Для регистронезависимых полей (email, системные имена) используется тип `citext`.

---

## Текущее состояние проекта

| Компонент | Статус |
|-----------|--------|
| Схема БД (SQL-скрипты, все таблицы) | ✅ Готово |
| Docker Compose (dev / db / backend / prod) | ✅ Готово |
| Каркас Backend (5 проектов, Clean Architecture) | ✅ Готово |
| Health-checks (`/health`, `/health/db`) | ✅ Готово |
| API-версионирование (`/api/v1/...`) | ✅ Готово |
| Swagger / OpenAPI | ✅ Готово |
| CRUD справочников (все 10: roles, statuses, degrees, titles, positions, study-groups, ...) | ✅ Готово |
| Unit-тесты справочников (255 тестов) | ✅ Готово |
| Интеграционные тесты справочников | ✅ Готово |
| JWT-авторизация + роли | ✅ Готово |
| Преподаватели, студенты, темы (API) | ✅ Готово |
| Жизненный цикл заявок (SupervisorRequests + Applications) | ✅ Готово |
| Чат студент ↔ преподаватель (REST polling) | ✅ Готово |
| Redis (refresh-токены, ротация/отзыв) | ✅ Готово |
| Архив ВКР + файловое хранилище S3/MinIO (presigned URL) | ✅ Готово |
| Email-уведомления (Inbox + SMTP) | ✅ Готово |
| Frontend (Angular 18) | 🔄 Запланировано |
| Мониторинг (Prometheus + Grafana) | 🔄 Запланировано |

---

## Frontend (планируется)

Angular 18 SPA с ролевой навигацией. Каждая роль видит собственный набор разделов:

- **Студент:** каталог преподавателей и тем, подача заявки, статус заявки, чат, архив ВКР
- **Преподаватель:** управление темами, входящие заявки, чат со студентами
- **Заведующий кафедрой:** список заявок кафедры на утверждение
- **Администратор:** управление пользователями, загрузка ВКР, аналитика и экспорт

Взаимодействие с API через HTTP-interceptor с автоматическим обновлением JWT.  
Реализация чата — REST + polling каждые 5–10 секунд с индикатором непрочитанных сообщений.

---

## Лицензия

[MIT](LICENSE)
