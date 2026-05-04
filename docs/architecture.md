# Архитектура проекта

![Архитектура](media/Architecture.png)

## Текущее состояние (на 2026-05-02)

- Архитектурный стиль: Clean Architecture (`Domain` → `Application` → `Infrastructure` → `API`).
- Источник истины по БД: SQL-скрипты в `infra/db/init` (порядок `00_…` и выше + seed в `test_data/`).
- Реализованы потоки:
  - выбор научного руководителя (`SupervisorRequests`);
  - утверждение темы (`StudentApplications`) с полным жизненным циклом статусов;
  - чат между студентом и преподавателем (`ApplicationChatMessages`, REST + polling);
  - архив ВКР (`GraduateWorks`) с загрузкой/скачиванием файлов через presigned URL (S3/MinIO).
- Авторизация: JWT + refresh-токены в Redis (ротация/отзыв при каждом refresh).
- Уведомления (in-app + email через `EmailBackgroundService`) реализованы для всех бизнес-потоков.
- 10 справочников с полным CRUD: `UserRoles`, `ApplicationStatuses`, `ApplicationActionStatuses`, `TopicStatuses`, `TopicCreatorTypes`, `NotificationTypes`, `StudyGroups`, `AcademicDegrees`, `AcademicTitles`, `Positions`.
- Покрытие тестами: unit-тесты сервисов + интеграционные тесты контроллеров; для списков тем и заявок научрука добавлены сценарии фильтрации по `createdFromUtc` / `createdToUtc`.
- **Frontend (Angular 20):** списки `/topics`, `/teachers`, `/supervisor-requests`, `/applications` (серверная сортировка и пагинация где поддержано API), фильтры по дате создания для тем и заявок научрука; карточки `/applications/:id`, `/supervisor-requests/:id` с действиями через модальные окна (одобрение с необязательным комментарием, отклонение с обязательным); форма `/applications/new`; на списке заявок студента кнопка «Создать заявку» скрывается при активной заявке (согласовано с правилами backend). Чат в UI — по-прежнему не полный цикл (см. `FrontendDevelopmentPlan.md`).
- Следующие крупные блоки: доработка SPA (чат, админка), **мониторинг** (Prometheus + Grafana).

## Ссылки на детальные документы

- Backend roadmap: `BackendDevelopmentPlan.md`
- Frontend roadmap: `FrontendDevelopmentPlan.md`
- Общий план проекта: `DevelopmentPlan.md`
- API index: `docs/api/README.md`
