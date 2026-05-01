# Архитектура проекта

![Архитектура](media/Architecture.png)

## Текущее состояние (на 2026-04-27)

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
- Покрытие тестами: unit-тесты сервисов + интеграционные тесты контроллеров; для списков тем и заявок научрука добавлены сценарии фильтрации по `createdFromUtc` / `createdToUtc`.
- **Frontend (Angular 20):** реализованы ключевые списки (`/topics`, `/teachers`, `/supervisor-requests`) с серверной сортировкой и пагинацией, фильтрами (в т.ч. диапазон даты создания для тем и заявок; для заявок по умолчанию — текущий календарный год), общие утилиты дат (`core/utils/date-utils.ts`), единый UI-блок выбора периода в `styles.scss`.
- Следующие крупные блоки: остальной функционал SPA по `FrontendDevelopmentPlan.md`, **мониторинг** (Prometheus + Grafana).

## Ссылки на детальные документы

- Backend roadmap: `BackendDevelopmentPlan.md`
- Frontend roadmap: `FrontendDevelopmentPlan.md`
- Общий план проекта: `DevelopmentPlan.md`
- API index: `docs/api/README.md`
