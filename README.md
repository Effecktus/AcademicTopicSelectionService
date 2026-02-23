# Сервис по выбору научного руководителя и темы ВКР

**Дипломный проект** — Ильин Айдар Альбертович

[![GitHub](https://img.shields.io/badge/GitHub-Effecktus%2FDirectoryOfGraduates-181717?logo=github)](https://github.com/Effecktus/DirectoryOfGraduates)
![.NET 10](https://img.shields.io/badge/.NET%2010-512BD4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL%2016-336791?logo=postgresql&logoColor=white)
![Angular](https://img.shields.io/badge/Angular%2018-DD0031?logo=angular&logoColor=white)
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
Студент выбирает тему → Преподаватель одобряет/отклоняет
        ↓ (при одобрении)
Заявка идёт заведующему → Заведующий утверждает/отклоняет
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
| Backend | ASP.NET Core 10 (Web API) | В разработке |
| База данных | PostgreSQL 16 + EF Core | Схема готова |
| Кэширование | Redis 7 | Запланировано |
| Файловое хранилище | MinIO (dev) / AWS S3 (prod) | Запланировано |
| Авторизация | JWT + Refresh Tokens | Запланировано |
| Email-уведомления | MailKit (SMTP) | Запланировано |
| Frontend | Angular 18 + TypeScript + SCSS | Запланировано |
| Контейнеризация | Docker + Docker Compose | Готово |
| Мониторинг | Prometheus + Grafana | Запланировано |

---

## Архитектура

Проект разделён на четыре слоя по принципу Clean Architecture:

```
backend/src/
├── DirectoryOfGraduates.API/           # Web API: контроллеры, Swagger, health-checks
├── DirectoryOfGraduates.Application/   # Бизнес-логика: сервисы, DTO, абстракции репозиториев
├── DirectoryOfGraduates.Infrastructure/# EF Core, репозитории, интеграции (S3, Redis)
└── DirectoryOfGraduates.Shared/        # Общие утилиты, константы, хелперы
```

**Правило зависимостей:**
- `API` → `Application`, `Infrastructure`, `Shared`
- `Infrastructure` → `Application`, `Shared`
- `Application` → `Shared`

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
- **pgAdmin / DBeaver:** хост `localhost`, порт `5432`, пользователь `app_user`, БД `app_database`

### 3. Запуск Backend (API + PostgreSQL)

```bash
cd infra/docker
docker compose -f compose.backend.yml up --build -d
```

Доступные адреса:
- **API + Swagger:** `http://localhost:5000/swagger`
- **Health:** `http://localhost:5000/health`
- **Health DB:** `http://localhost:5000/health/db`
- **PostgreSQL:** `localhost:5432`

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
| Backend API | `http://localhost:5000` |
| MinIO Console | `http://localhost:9001` |
| Prometheus | `http://localhost:9090` |
| Grafana | `http://localhost:3000` |

### Остановка

```bash
docker compose -f compose.dev.yml down          # сохранить данные
docker compose -f compose.dev.yml down -v       # удалить данные (volumes)
```

---

## API

Все бизнес-эндпоинты версионируются через URL: `/api/v1/...`

Полная документация доступна через **Swagger** при запущенном API: `http://localhost:5000/swagger`

Дополнительная Markdown-документация: [`docs/api/`](docs/api/)

### Реализованные эндпоинты

| Метод | Путь | Описание |
|-------|------|----------|
| `GET` | `/health` | Проверка доступности API |
| `GET` | `/health/db` | Проверка подключения к PostgreSQL |
| `GET` | `/api/v1/user-roles` | Список ролей пользователей |
| `GET` | `/api/v1/user-roles/{id}` | Роль по ID |
| `POST` | `/api/v1/user-roles` | Создание роли |
| `PUT` | `/api/v1/user-roles/{id}` | Обновление роли |
| `DELETE` | `/api/v1/user-roles/{id}` | Удаление роли |
| `GET` | `/api/v1/application-statuses` | Список статусов заявок |
| `GET` | `/api/v1/application-statuses/{id}` | Статус по ID |
| `POST` | `/api/v1/application-statuses` | Создание статуса |
| `PUT` | `/api/v1/application-statuses/{id}` | Обновление статуса |
| `DELETE` | `/api/v1/application-statuses/{id}` | Удаление статуса |

### Запланированные эндпоинты

<details>
<summary>Показать</summary>

**Auth**
- `POST /api/v1/auth/login` — вход, получение JWT
- `POST /api/v1/auth/refresh` — обновление access-токена
- `POST /api/v1/auth/logout` — выход, отзыв токена

**Преподаватели**
- `GET /api/v1/teachers` — список преподавателей
- `GET /api/v1/teachers/{id}` — профиль и статистика преподавателя
- `GET /api/v1/teachers/{id}/topics` — темы преподавателя

**Темы ВКР**
- `GET /api/v1/topics` — список тем (фильтры, пагинация)
- `POST /api/v1/topics` — создание темы (преподаватель)
- `PUT /api/v1/topics/{id}` — обновление темы
- `DELETE /api/v1/topics/{id}` — удаление темы

**Заявки**
- `POST /api/v1/applications` — подача заявки (студент)
- `PUT /api/v1/applications/{id}/approve` — одобрение (преподаватель)
- `PUT /api/v1/applications/{id}/reject` — отклонение (преподаватель)
- `PUT /api/v1/applications/{id}/department-head-approve` — утверждение (заведующий)
- `PUT /api/v1/applications/{id}/department-head-reject` — отклонение (заведующий)
- `PUT /api/v1/applications/{id}/cancel` — отмена (студент)

**Чат**
- `GET /api/v1/chat/applications/{id}/messages` — история сообщений
- `POST /api/v1/chat/messages` — отправка сообщения
- `PUT /api/v1/chat/messages/{id}/read` — отметить прочитанным

**Архив ВКР**
- `GET /api/v1/vkr` — список защищённых работ
- `GET /api/v1/vkr/{id}` — детали ВКР
- `GET /api/v1/vkr/{id}/download` — скачать файл
- `POST /api/v1/vkr` — загрузить ВКР в архив (администратор)

**Администрирование**
- `POST /api/v1/admin/users` — создание пользователя
- `GET /api/v1/admin/analytics` — аналитика
- `GET /api/v1/admin/export` — экспорт данных (Excel, CSV)

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

**Справочники:** `UserRoles`, `ApplicationStatuses`, `TopicStatuses`, `NotificationTypes`, `AcademicDegrees`, `AcademicTitles`, `Positions`

**Основные сущности:** `Users`, `Departments`, `Teachers`, `Students`, `Topics`, `StudentApplications`, `ChatMessages`, `GraduateWorks`, `Notifications`, `RefreshTokens`

Поля `CreatedAt` / `UpdatedAt` обновляются автоматически через PostgreSQL-триггеры. Для регистронезависимых полей (email, системные имена) используется тип `citext`.

---

## Текущее состояние проекта

| Компонент | Статус |
|-----------|--------|
| Схема БД (SQL-скрипты, все таблицы) | ✅ Готово |
| Docker Compose (dev / db / backend / prod) | ✅ Готово |
| Каркас Backend (4 проекта, Clean Architecture) | ✅ Готово |
| Health-checks (`/health`, `/health/db`) | ✅ Готово |
| API-версионирование (`/api/v1/...`) | ✅ Готово |
| Swagger / OpenAPI | ✅ Готово |
| CRUD справочников: `user-roles` | ✅ Готово |
| CRUD справочников: `application-statuses` | ✅ Готово |
| JWT-авторизация + роли | 🔄 Запланировано |
| Преподаватели, студенты, темы (API) | 🔄 Запланировано |
| Жизненный цикл заявок | 🔄 Запланировано |
| Чат (REST + polling) | 🔄 Запланировано |
| Redis (кэш, blacklist токенов) | 🔄 Запланировано |
| Архив ВКР + S3/MinIO | 🔄 Запланировано |
| Email-уведомления | 🔄 Запланировано |
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
