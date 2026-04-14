# API документация

Описание HTTP API приложения (v1).  
Ошибки 4xx/5xx возвращаются в формате [ProblemDetails](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails) (JSON).

## Быстрые ссылки

| Раздел | Описание |
|--------|----------|
| [health.md](health.md) | Health-check endpoints (`/health`, `/health/db`) |
| [v1.endpoints.md](v1.endpoints.md) | Полный список актуальных endpoint-ов v1 |
| [v1.applications.md](v1.applications.md) | Поток 2: заявки на утверждение темы |
| [v1.supervisor-requests.md](v1.supervisor-requests.md) | Поток 1: выбор научного руководителя |
| [v1.notifications.md](v1.notifications.md) | Inbox уведомлений: список и отметка прочитанного |
| [v1.user-roles.md](v1.user-roles.md) | Справочник ролей пользователей — CRUD |
| [v1.application-statuses.md](v1.application-statuses.md) | Справочник статусов заявки — CRUD |

## Замечания

- Источник истины по контрактам и схемам ответов — Swagger (`/swagger`) в текущей сборке API.
- Markdown-документация синхронизируется вручную и описывает ключевые сценарии/правила.
