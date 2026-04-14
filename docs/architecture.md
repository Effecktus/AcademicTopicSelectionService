# Архитектура проекта

![Архитектура](media/Architecture.png)

## Текущее состояние (на 2026-04-14)

- Архитектурный стиль: Clean Architecture (`Domain` -> `Application` -> `Infrastructure` -> `API`).
- Источник истины по БД: SQL-скрипты в `infra/db/init`.
- Реализованы потоки:
  - выбор научного руководителя (`SupervisorRequests`);
  - утверждение темы (`StudentApplications`).
- Авторизация: JWT + refresh-токены в Redis (ротация/отзыв).
- Следующие крупные блоки в работе: чат (polling), архив ВКР + файлы.
- Уведомления (in-app + email через фонового воркера) реализованы для потоков `SupervisorRequests` и `StudentApplications`.

## Ссылки на детальные документы

- Backend roadmap: `BackendDevelopmentPlan.md`
- Общий план проекта: `DevelopmentPlan.md`
- API index: `docs/api/README.md`
