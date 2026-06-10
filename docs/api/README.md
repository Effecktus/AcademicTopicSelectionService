# API документация

Описание HTTP API приложения (v1). Актуализировано: **2026-05-19** (MVP завершён).  
Ошибки 4xx/5xx возвращаются в формате [ProblemDetails](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails) (JSON).

## Быстрые ссылки

| Раздел | Описание |
|--------|----------|
| [v1.auth-and-users.md](v1.auth-and-users.md) | Login / refresh / logout, создание пользователя (`POST /users`), httpOnly cookie, Bearer |
| [health.md](health.md) | Health-check endpoints (`/health`, `/health/db`) |
| [v1.endpoints.md](v1.endpoints.md) | Полный список актуальных endpoint-ов v1 |
| [v1.topics.md](v1.topics.md) | Темы ВКР: список с фильтрами (в т.ч. по дате создания), CRUD |
| [v1.supervisor-requests.md](v1.supervisor-requests.md) | Поток 1: выбор научного руководителя |
| [v1.applications.md](v1.applications.md) | Поток 2: заявки на утверждение темы (включая чат) |
| [v1.notifications.md](v1.notifications.md) | Inbox уведомлений: список и отметка прочитанного |
| [v1.user-roles.md](v1.user-roles.md) | Справочник ролей пользователей — CRUD |
| [v1.application-statuses.md](v1.application-statuses.md) | Справочник статусов заявки — CRUD |

## Замечания

- Источник истины по контрактам и схемам ответов — Swagger (`/swagger`) в текущей сборке API.
- Refresh-токен передаётся через **httpOnly-cookie** `refreshToken`; тело `POST /auth/refresh` и `POST /auth/logout` — пустое; подробнее см. [v1.auth-and-users.md](v1.auth-and-users.md).
- Поток заявки на тему: после одобрения научруком (`PUT .../applications/{id}/approve`) заявка сразу переходит к заведующему (`PendingDepartmentHead`); отдельного эндпоинта для «ручной передачи» заведующему нет (см. [v1.applications.md](v1.applications.md)).
- Все `/api/` запросы с фронта должны отправляться с `withCredentials: true` для корректной передачи cookie.
