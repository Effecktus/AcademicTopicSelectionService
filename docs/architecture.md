# Архитектура проекта

![Архитектура](media/Architecture.png)

## Текущее состояние (на 2026-04-20)

- Архитектурный стиль: Clean Architecture (`Domain` → `Application` → `Infrastructure` → `API`).
- Источник истины по БД: SQL-скрипты в `infra/db/init` (25 файлов, порядок 00–24 + seed).
- Реализованы потоки:
  - выбор научного руководителя (`SupervisorRequests`);
  - утверждение темы (`StudentApplications`) с полным жизненным циклом статусов;
  - чат между студентом и преподавателем (`ApplicationChatMessages`, REST + polling);
  - архив ВКР (`GraduateWorks`) с загрузкой/скачиванием файлов через presigned URL (S3/MinIO).
- Авторизация: JWT + refresh-токены в Redis (ротация/отзыв при каждом refresh).
- Уведомления (in-app + email через `EmailBackgroundService`) реализованы для всех бизнес-потоков.
- 10 справочников с полным CRUD: `UserRoles`, `ApplicationStatuses`, `ApplicationActionStatuses`, `TopicStatuses`, `TopicCreatorTypes`, `NotificationTypes`, `StudyGroups`, `AcademicDegrees`, `AcademicTitles`, `Positions`.
- Покрытие тестами: unit-тесты всех сервисов + интеграционные тесты всех контроллеров.
- Следующие крупные блоки в работе: **Frontend** (Angular 18), **мониторинг** (Prometheus + Grafana).

## Ссылки на детальные документы

- Backend roadmap: `BackendDevelopmentPlan.md`
- Frontend roadmap: `FrontendDevelopmentPlan.md`
- Общий план проекта: `DevelopmentPlan.md`
- API index: `docs/api/README.md`
