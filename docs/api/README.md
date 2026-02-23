# API документация

Описание HTTP API приложения (v1). Ошибки 4xx/5xx возвращаются в формате [ProblemDetails](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails) (JSON).

| Раздел | Описание |
|--------|----------|
| [health.md](health.md) | Health-check endpoints (`/health`, `/health/db`) |
| [v1.user-roles.md](v1.user-roles.md) | Справочник ролей пользователей — CRUD |
| [v1.application-statuses.md](v1.application-statuses.md) | Справочник статусов заявки — CRUD |

Справочники (User Roles, Application Statuses) поддерживают: список с пагинацией и поиском, получение по id, создание (POST), полное обновление (PUT), частичное обновление (PATCH), удаление (DELETE).
