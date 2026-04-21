# API документация

Описание HTTP API приложения (v1).  
Ошибки 4xx/5xx возвращаются в формате [ProblemDetails](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails) (JSON).

## Быстрые ссылки

| Раздел | Описание |
|--------|----------|
| **[v1.auth-and-users.md](v1.auth-and-users.md)** | **P0 для фронта:** login / refresh / logout, создание пользователя (`POST /users`), Bearer, health |
| [health.md](health.md) | Health-check endpoints (`/health`, `/health/db`) |
| [v1.endpoints.md](v1.endpoints.md) | Полный список актуальных endpoint-ов v1 |
| [v1.supervisor-requests.md](v1.supervisor-requests.md) | Поток 1: выбор научного руководителя |
| [v1.applications.md](v1.applications.md) | Поток 2: заявки на утверждение темы (включая чат) |
| [v1.notifications.md](v1.notifications.md) | Inbox уведомлений: список и отметка прочитанного |
| [v1.user-roles.md](v1.user-roles.md) | Справочник ролей пользователей — CRUD |
| [v1.application-statuses.md](v1.application-statuses.md) | Справочник статусов заявки — CRUD |
| [swagger-scenario-application-chat.md](swagger-scenario-application-chat.md) | Сценарий: чат студент ↔ преподаватель |
| [swagger-scenario-graduate-works.md](swagger-scenario-graduate-works.md) | Сценарий: загрузка и скачивание файлов ВКР |

## Замечания

- Источник истины по контрактам и схемам ответов — Swagger (`/swagger`) в текущей сборке API.
- Для старта интеграции с фронтендом сначала см. **[v1.auth-and-users.md](v1.auth-and-users.md)**.
- Markdown-документация синхронизируется вручную и описывает ключевые сценарии/правила.
