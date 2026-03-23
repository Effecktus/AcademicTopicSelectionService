# Архитектурные решения и рекомендации для проекта "Сервис выбора научного руководителя и темы ВКР"

## 1. Бизнес-логика и требования

### Роли пользователей
- **Студент** — выбор преподавателя, просмотр тем, подача заявки, общение в чате
- **Преподаватель** — управление темами, одобрение/отклонение заявок, общение со студентами
- **Заведующий кафедрой** — утверждение/отклонение заявок своей кафедры
- **Администратор** — управление пользователями, аналитика, экспорт данных

### Процесс выбора темы
1. Студент выбирает одного преподавателя
2. Выбирает тему из предложенных или предлагает свою
3. Преподаватель одобряет/отклоняет (возможна коммуникация в чате для обсуждения темы)
4. При одобрении заявка отправляется заведующему кафедрой
5. Заведующий утверждает/отклоняет
6. При отказе студент может изменить тему и отправить заново
7. **После утверждения заведующим процесс завершается** — дальнейшая работа над ВКР ведется вне системы
8. После успешной защиты ВКР администратор загружает работу в систему как архивную

### Правила
- Тема закрепляется за первым выбравшим студентом
- Преподаватель сам устанавливает лимит студентов
- Студент может отменить заявку до отправки заведующему
- После отправки заведующему отмена недоступна

### Статистика преподавателя
- Количество ВКР прошлых лет (из архива)
- Средняя оценка
- Темы прошлых лет
- Количество активных заявок (в процессе утверждения)

### ВКР (архив защищенных работ)
- **Все ВКР в системе — это архив защищенных работ прошлых лет**
- Загружаются администратором после успешной защиты
- Просмотр доступен всем
- Возможность скачивания файлов
- Метаданные: год, оценка, тема, студент, научный руководитель, комиссия

### Коммуникация
- **Чат между студентом и преподавателем только для обсуждения темы**
- Чат доступен до утверждения заведующим кафедрой
- После утверждения заведующим чат закрывается (процесс завершен)
- История переписки сохраняется
- REST API + polling (без real-time)
- Email уведомления: статус утверждения темы, новые сообщения в чате (если долго не читаются)
- Индикаторы непрочитанных сообщений в UI

### Файлы
- Хранятся: ВКР и презентации (только архивные, после защиты)
- Загружаются администратором после успешной защиты
- Версионность не требуется
- Совместное редактирование не требуется

### Дополнительные функции
- Аналитика для администрации
- Экспорт данных (отчеты, статистика)
- Дедлайны не требуются

---

## 2. Технологический стек

| Компонент | Технология |
|-----------|-----------|
| Frontend | Angular 18 + TypeScript + SCSS |
| Backend | ASP.NET Core 10 (Web API) |
| База данных | PostgreSQL 16 + EF Core |
| Кэширование | Redis 7 |
| Файловое хранилище | MinIO (dev) / AWS S3 (prod) |
| Авторизация | JWT + Refresh Tokens |
| Email | MailKit (SMTP) |
| Мониторинг | Prometheus + Grafana |
| Контейнеризация | Docker + Docker Compose |

---

## 3. Структура базы данных

### Справочники (Dictionaries)

#### UserRoles (Роли пользователей)
```sql
- Id (PK, Guid)
- Name (citext, unique, required) -- System name (e.g., "Student"), регистронезависимо
- DisplayName (string, required) -- UI name (e.g., "Студент")
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable)
```

#### ApplicationStatuses (Статусы заявок)
```sql
- Id (PK, Guid)
- Name (citext, unique, required) -- регистронезависимо
- DisplayName (string, required)
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable)
```

#### TopicStatuses (Статусы тем)
```sql
- Id (PK, Guid)
- Name (citext, unique, required) -- регистронезависимо
- DisplayName (string, required)
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable)
```

#### NotificationTypes (Типы уведомлений)
```sql
- Id (PK, Guid)
- Name (citext, unique, required) -- регистронезависимо
- DisplayName (string, required)
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable)
```

#### AcademicDegrees (Ученые степени)
```sql
- Id (PK, Guid)
- Name (citext, unique, required) -- System name (e.g., "CandidateOfBiologicalSciences"), регистронезависимо
- DisplayName (string, required) -- UI name (e.g., "Кандидат биологических наук")
- ShortName (string, nullable) -- Сокращённое название (e.g., "канд. биол. наук")
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable)
```

#### AcademicTitles (Ученые звания)
```sql
- Id (PK, Guid)
- Name (citext, unique, required) -- System name (e.g., "AssociateProfessor"), регистронезависимо
- DisplayName (string, required) -- UI name (e.g., "Доцент")
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable)
```

#### Positions (Должности)
```sql
- Id (PK, Guid)
- Name (citext, unique, required) -- System name (e.g., "Assistant"), регистронезависимо
- DisplayName (string, required) -- UI name (e.g., "Ассистент")
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable)
```

### Основные сущности

#### Users (Пользователи)
```sql
- Id (PK, Guid)
- Email (citext, unique, required) -- регистронезависимо
- PasswordHash (string, required)
- FirstName (string, required)
- LastName (string, required)
- MiddleName (string, nullable)
- RoleId (FK -> UserRoles, required) -- Ссылка на роль
- DepartmentId (FK -> Departments, nullable)
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable) -- Обновляется автоматически триггером
- IsActive (bool)
```

#### Departments (Кафедры)
```sql
- Id (PK, Guid)
- Name (string, required)
- HeadId (FK -> Users, nullable) -- Заведующий кафедрой
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable) -- Обновляется автоматически триггером
```

#### Teachers (Преподаватели)
```sql
- Id (PK, Guid)
- UserId (FK -> Users, unique, required)
- MaxStudentsLimit (int, nullable) -- Лимит студентов
- AcademicDegreeId (FK -> AcademicDegrees, required) -- Ученая степень (если нет — выбрать "None")
- AcademicTitleId (FK -> AcademicTitles, required) -- Ученое звание (если нет — выбрать "None")
- PositionId (FK -> Positions, required) -- Должность
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable) -- Обновляется автоматически триггером
```

#### Students (Студенты)
```sql
- Id (PK, Guid)
- UserId (FK -> Users, unique, required)
- Group (int) -- Группа
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable) -- Обновляется автоматически триггером
```

#### Topics (Темы ВКР)
```sql
- Id (PK, Guid)
- Title (string, required)
- Description (string, nullable)
- Year (int, required) -- Учебный год
- TeacherId (FK -> Teachers, required)
- StatusId (FK -> TopicStatuses, required) -- Ссылка на статус
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable) -- Обновляется автоматически триггером
```

#### StudentApplications (Заявки студентов)
```sql
- Id (PK, Guid)
- StudentId (FK -> Students, required)
- TopicId (FK -> Topics, nullable) -- null если своя тема
- ProposedTitle (string, nullable) -- Если своя тема
- ProposedDescription (string, nullable)
- StatusId (FK -> ApplicationStatuses, required) -- Ссылка на статус
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable)
- TeacherApprovedAt (DateTime, nullable)
- TeacherRejectedAt (DateTime, nullable)
- TeacherRejectionReason (string, nullable)
- DepartmentHeadApprovedAt (DateTime, nullable)
- DepartmentHeadRejectedAt (DateTime, nullable)
- DepartmentHeadRejectionReason (string, nullable)
- CancelledAt (DateTime, nullable)
```

#### GraduateWorks (Выпускные квалификационные работы)
```sql
- Id (PK, Guid)
- Title (string, required)
- StudentId (FK -> Students, required)
- TeacherId (FK -> Teachers, required)
- Year (int, required)
- Grade (int, required) -- Оценка (0..100)
- CommissionMembers (string, required) -- JSON или строка с членами комиссии
- FilePath (string, required) -- Путь/ключ в S3 (не пустой)
- PresentationPath (string, nullable) -- Путь к презентации в S3
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable)
```

#### ChatMessages (Сообщения чата)
```sql
- Id (PK, Guid)
- ApplicationId (FK -> StudentApplications, required)
- SenderId (FK -> Users, required)
- Content (string, required)
- SentAt (DateTime, required)
- ReadAt (DateTime, nullable)
```

#### Notifications (Уведомления)
```sql
- Id (PK, Guid)
- UserId (FK -> Users, required)
- TypeId (FK -> NotificationTypes, required) -- Ссылка на тип
- Title (string, required)
- Content (string, required)
- IsRead (bool, default: false)
- CreatedAt (DateTime)
```

#### RefreshTokens (Refresh токены для JWT)
```sql
- Id (PK, Guid)
- UserId (FK -> Users, required)
- Token (string, required, unique)
- ExpiresAt (DateTime, required)
- CreatedAt (DateTime)
- IsRevoked (bool, default: false)
```

### Индексы
- `Users.Email` — unique index
- `StudentApplications.StudentId` — index
- `StudentApplications.TopicId` — index
- `StudentApplications.StatusId` — index
- `ChatMessages.ApplicationId` — index
- `ChatMessages.SentAt` — index
- `Notifications.UserId, IsRead` — composite index

### Триггеры и функции для автоматического обновления UpdatedAt

Для таблиц с полем `UpdatedAt` необходимо создать функцию и триггеры для автоматического обновления этого поля при изменении записи.

#### Функция update_updated_at_column()
```sql
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```

#### Триггеры для таблиц
Триггеры создаются для следующих таблиц:
- `Users` — триггер `update_users_updated_at`
- `Departments` — триггер `update_departments_updated_at`
- `Teachers` — триггер `update_teachers_updated_at`
- `Students` — триггер `update_students_updated_at`
- `Topics` — триггер `update_topics_updated_at`
- `Applications` — триггер `update_applications_updated_at`
- `GraduateWorks` — триггер `update_graduate_works_updated_at`
- `UserRoles` — триггер `update_user_roles_updated_at`
- `ApplicationStatuses` — триггер `update_application_statuses_updated_at`
- `TopicStatuses` — триггер `update_topic_statuses_updated_at`
- `NotificationTypes` — триггер `update_notification_types_updated_at`
- `AcademicDegrees` — триггер `update_academic_degrees_updated_at`
- `AcademicTitles` — триггер `update_academic_titles_updated_at`
- `Positions` — триггер `update_positions_updated_at`

### Связи
- User 1:1 Student/Teacher
- Department 1:N Users
- Teacher 1:N Topics
- Teacher 1:N StudentApplications (через Topics)
- Student 1:N StudentApplications
- Topic 1:N StudentApplications
- Application 1:N ChatMessages
- User 1:N ChatMessages (как отправитель)
- User 1:N Notifications

---

## 4. Структура Backend (ASP.NET Core)

### Организация проектов

```
backend/src/
├── AcademicTopicSelectionService.API/          # Главный проект (Web API)
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── TopicsController.cs
│   │   ├── ApplicationsController.cs
│   │   ├── TeachersController.cs
│   │   ├── ChatController.cs
│   │   ├── VKRController.cs
│   │   └── AdminController.cs
│   ├── Middleware/                    # планируется: ErrorHandling, JWT и др.
│   ├── Swagger/
│   ├── Program.cs
│   └── appsettings.json
│
├── AcademicTopicSelectionService.Application/  # Бизнес-логика
│   ├── Abstractions/                  # интерфейсы репозиториев
│   ├── Dictionaries/                  # эталон CRUD (UserRoles) и др. справочники
│   └── DependencyInjection.cs         # регистрация сервисов Application
│
├── AcademicTopicSelectionService.Infrastructure/ # EF Core, S3, Redis
│   ├── Data/
│   │   ├── ApplicationDbContext.cs     # scaffold контекст (OnModelCreating)
│   │   └── Entities/                   # scaffold сущности таблиц
│   ├── Repositories/
│   │   └── ...Repository.cs
│   ├── Services/
│   │   ├── IS3Service.cs
│   │   ├── S3Service.cs
│   │   ├── IRedisService.cs
│   │   └── RedisService.cs
│   └── DependencyInjection.cs          # регистрация инфраструктуры (DbContext, репозитории)
│
└── AcademicTopicSelectionService.Shared/       # Общие утилиты
    ├── Constants/
    ├── Helpers/
    └── Extensions/
```

### Основные сервисы

#### AuthService
- Регистрация (только для админа)
- Логин/логин с JWT
- Обновление refresh token
- Выход (отзыв токенов)

#### TopicService
- CRUD для тем
- Получение тем преподавателя
- Получение доступных тем

#### ApplicationService
- Создание заявки
- Одобрение/отклонение преподавателем
- Утверждение/отклонение заведующим (завершение процесса)
- Отмена заявки студентом
- Получение заявок пользователя
- Проверка доступности чата для заявки

#### ChatService
- Отправка сообщения (только для заявок до утверждения заведующим)
- Получение сообщений заявки
- Отметка прочитанным
- Получение непрочитанных сообщений
- Проверка доступности чата (только до статуса ApprovedByDepartmentHead)

#### NotificationService
- Создание уведомления
- Отправка email
- Получение уведомлений пользователя
- Отметка прочитанным

---

## 5. Структура Frontend (Angular)

```
frontend/src/
├── app/
│   ├── core/                          # Ядро приложения
│   │   ├── auth/
│   │   │   ├── auth.service.ts
│   │   │   ├── auth.guard.ts
│   │   │   └── role.guard.ts
│   │   ├── services/
│   │   │   ├── api.service.ts
│   │   │   ├── http-interceptor.service.ts
│   │   │   └── notification.service.ts
│   │   └── models/
│   │       └── user.model.ts
│   │
│   ├── shared/                        # Общие компоненты
│   │   ├── components/
│   │   │   ├── header/
│   │   │   ├── footer/
│   │   │   ├── loading-spinner/
│   │   │   └── notification-badge/
│   │   ├── pipes/
│   │   └── directives/
│   │
│   ├── features/                      # Функциональные модули
│   │   ├── auth/
│   │   │   ├── login/
│   │   │   └── components/
│   │   │
│   │   ├── teachers/
│   │   │   ├── teacher-list/
│   │   │   ├── teacher-detail/
│   │   │   └── teacher-statistics/
│   │   │
│   │   ├── topics/
│   │   │   ├── topic-list/
│   │   │   ├── topic-create/
│   │   │   └── topic-select/
│   │   │
│   │   ├── applications/
│   │   │   ├── application-list/
│   │   │   ├── application-create/
│   │   │   ├── application-detail/
│   │   │   └── application-status/
│   │   │
│   │   ├── chat/
│   │   │   ├── chat-window/
│   │   │   ├── message-list/
│   │   │   └── message-input/
│   │   │
│   │   ├── vkr/
│   │   │   ├── vkr-list/
│   │   │   ├── vkr-detail/
│   │   │   └── vkr-download/
│   │   │
│   │   └── admin/
│   │       ├── user-management/
│   │       ├── analytics/
│   │       └── export/
│   │
│   ├── layouts/                       # Шаблоны страниц
│   │   ├── main-layout/
│   │   └── auth-layout/
│   │
│   └── models/                        # TypeScript модели
│       ├── application.model.ts
│       ├── topic.model.ts
│       ├── chat-message.model.ts
│       └── ...
│
├── assets/
├── environments/
│   ├── environment.ts
│   └── environment.prod.ts
└── styles/
```

---

## 6. API Endpoints (основные)

Все бизнес-endpoints версионируются через URL: **`/api/v1/...`**.

Документация “как эталон” дополнительно ведётся в `docs/api/*.md` (помимо Swagger).

### Auth
- `POST /api/v1/auth/login` — вход
- `POST /api/v1/auth/refresh` — обновление токена
- `POST /api/v1/auth/logout` — выход

### Teachers
- `GET /api/v1/teachers` — список преподавателей
- `GET /api/v1/teachers/{id}` — детали преподавателя
- `GET /api/v1/teachers/{id}/statistics` — статистика
- `GET /api/v1/teachers/{id}/topics` — темы преподавателя

### Topics
- `GET /api/v1/topics` — список тем (с фильтрами)
- `GET /api/v1/topics/{id}` — детали темы
- `POST /api/v1/topics` — создание темы (преподаватель)
- `PUT /api/v1/topics/{id}` — обновление темы
- `DELETE /api/v1/topics/{id}` — удаление темы

### Applications
- `GET /api/v1/applications` — список заявок (с фильтрами по роли)
- `GET /api/v1/applications/{id}` — детали заявки
- `POST /api/v1/applications` — создание заявки (студент)
- `PUT /api/v1/applications/{id}/approve` — одобрение (преподаватель)
- `PUT /api/v1/applications/{id}/reject` — отклонение (преподаватель)
- `PUT /api/v1/applications/{id}/department-head-approve` — утверждение (заведующий)
- `PUT /api/v1/applications/{id}/department-head-reject` — отклонение (заведующий)
- `PUT /api/v1/applications/{id}/cancel` — отмена (студент)

### Chat
- `GET /api/v1/chat/applications/{applicationId}/messages` — сообщения заявки
- `POST /api/v1/chat/messages` — отправка сообщения
- `PUT /api/v1/chat/messages/{id}/read` — отметка прочитанным
- `GET /api/v1/chat/unread-count` — количество непрочитанных

### VKR (архив защищенных работ)
- `GET /api/v1/vkr` — список ВКР (архив защищенных работ)
- `GET /api/v1/vkr/{id}` — детали ВКР
- `GET /api/v1/vkr/{id}/download` — скачивание файла
- `POST /api/v1/vkr` — загрузка ВКР в архив (только администратор, после защиты)

### Admin
- `GET /api/v1/admin/users` — список пользователей
- `POST /api/v1/admin/users` — создание пользователя
- `GET /api/v1/admin/analytics` — аналитика
- `GET /api/v1/admin/export` — экспорт данных

---

## 7. JWT авторизация

### Токены
- Access Token: короткоживущий (~15 минут), в заголовке Authorization
- Refresh Token: долгоживущий (~7 дней), в cookie или body

### Хранение
- Refresh токены в Redis (для отзыва)
- Access токены не хранятся на сервере (stateless)

### Guards
- `AuthGuard` — проверка авторизации
- `RoleGuard` — проверка роли

---

## 8. Интеграция с S3/MinIO

### Структура хранения
```
vkr/
  └── {year}/
      └── {vkrId}/
          ├── thesis.pdf
          └── presentation.pptx
```

### Сервис S3Service
- `UploadFileAsync(bucket, key, stream)` — загрузка
- `DownloadFileAsync(bucket, key)` — скачивание
- `DeleteFileAsync(bucket, key)` — удаление
- `GetFileUrlAsync(bucket, key)` — получение URL

### Политики доступа
- Все ВКР в системе (архив защищенных работ): публичное чтение для всех авторизованных пользователей

---

## 9. Email уведомления

### Типы уведомлений
1. Изменение статуса заявки
2. Новое сообщение в чате (если долго не читается, только для активных заявок)

### Реализация
- MailKit для отправки через SMTP
- BackgroundService для очереди писем
- Шаблоны писем (Razor или простой текст)

### Триггеры
- При одобрении/отклонении преподавателем
- При утверждении/отклонении заведующим (финальное уведомление о завершении процесса)
- При новом сообщении в чате (если не прочитано > 1 часа, только для активных заявок)

---

## 10. Чат (REST API + Polling)

### Реализация
- REST API для отправки/получения сообщений
- Polling на клиенте каждые 5–10 секунд
- Индикатор непрочитанных сообщений
- **Чат доступен только для заявок со статусами до `ApprovedByDepartmentHead`**

### Endpoints
- `GET /api/v1/chat/applications/{id}/messages?page=1&pageSize=50` — получение сообщений (проверка доступности чата)
- `POST /api/v1/chat/messages` — отправка сообщения (проверка, что заявка не завершена)
- `PUT /api/v1/chat/messages/{id}/read` — отметка прочитанным
- `GET /api/v1/chat/applications/{id}/is-available` — проверка доступности чата

### Правила доступа к чату
- Чат доступен только для заявок со статусами: `Pending`, `ApprovedBySupervisor`, `PendingDepartmentHead`
- После статуса `ApprovedByDepartmentHead` или `RejectedByDepartmentHead` чат закрывается
- После закрытия чата новые сообщения отправлять нельзя, история сохраняется

### Оптимизация
- Пагинация сообщений
- Кэширование последних сообщений в Redis
- Загрузка по требованию (lazy loading)

---

## 11. Статусы заявок и логика отмены

### Статусы
1. `Pending` — ожидает ответа преподавателя
2. `ApprovedBySupervisor` — одобрено преподавателем (чат доступен для обсуждения темы)
3. `RejectedBySupervisor` — отклонено преподавателем
4. `PendingDepartmentHead` — отправлено заведующему (чат доступен)
5. `ApprovedByDepartmentHead` — **утверждено заведующим (ФИНАЛЬНЫЙ СТАТУС, процесс завершен, чат закрывается)**
6. `RejectedByDepartmentHead` — отклонено заведующим
7. `Cancelled` — отменено студентом

### Правила отмены
- Студент может отменить до статуса `PendingDepartmentHead`
- После отправки заведующему отмена недоступна
- При отмене тема освобождается для других студентов

### Завершение процесса
- После статуса `ApprovedByDepartmentHead` процесс выбора темы и научного руководителя завершен
- Дальнейшая работа над ВКР ведется вне системы
- После успешной защиты администратор загружает ВКР в архив системы

---

## 12. Аналитика и экспорт

### Аналитика
- Количество заявок по статусам
- Статистика по кафедрам
- Средние оценки преподавателей (из архива ВКР)
- Количество ВКР в архиве по годам
- Активность пользователей
- Количество завершенных заявок (утвержденных заведующим)

### Экспорт
- Excel (ClosedXML)
- CSV
- PDF отчеты

### Endpoints
- `GET /api/admin/analytics/dashboard`
- `GET /api/admin/analytics/teachers`
- `GET /api/admin/analytics/departments`
- `GET /api/admin/export/applications?format=excel`
- `GET /api/admin/export/statistics?format=csv`

---

## 13. Docker конфигурация

### Compose файлы
- `compose.dev.yml` — полный стек для разработки
- `compose.db.yml` — только БД
- `compose.prod.yml` — production с nginx

### Сервисы
- PostgreSQL 16
- Redis 7
- MinIO (dev) / AWS S3 (prod)
- Backend (ASP.NET Core)
- Frontend (Angular + Nginx)
- Prometheus + Grafana (мониторинг)

---

## 14. Порядок реализации

### Этап 1: База данных
1. Создать структуру проектов
2. Настроить EF Core
3. Создать модели сущностей
4. Настроить конфигурации (Fluent API)
5. Создать миграции
6. Применить миграции

### Этап 2: Backend (базовый)
1. Настроить проект API
2. Настроить DbContext
3. Реализовать JWT авторизацию
4. Создать базовые контроллеры
5. Настроить Swagger

### Этап 3: Backend (функциональность)
1. Реализовать сервисы (Topics, Applications, Chat)
2. Интеграция с S3/MinIO
3. Email уведомления
4. Аналитика и экспорт

### Этап 4: Frontend
1. Создать Angular проект
2. Настроить routing и guards
3. Реализовать компоненты авторизации
4. Реализовать основные модули
5. Интеграция с API

### Этап 5: Тестирование и доработка
1. Unit тесты
2. Integration тесты
3. E2E тесты
4. Оптимизация и рефакторинг

---

## 15. Важные замечания

### Безопасность
- Хеширование паролей (BCrypt)
- Валидация входных данных
- Защита от SQL injection (EF Core)
- CORS настройки
- Rate limiting для API

### Производительность
- Кэширование в Redis (статистика, часто запрашиваемые данные)
- Индексы в БД
- Пагинация для больших списков
- Оптимизация запросов (Include, AsNoTracking)

### Мониторинг
- Логирование (Serilog)
- Метрики Prometheus
- Дашборды Grafana
- Health checks

---

Этот документ можно использовать как руководство при разработке. Начинаем с БД: создадим структуру проектов и настроим EF Core с моделями.