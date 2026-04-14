## Чеклист проверки уведомлений (актуальный)

### 0) Предусловия
- Есть 3 пользователя: `Student`, `Teacher`, `DepartmentHead`.
- `Teacher` привязан к кафедре (`Users.DepartmentId` не `NULL`).
- Для этой кафедры назначен заведующий (`Departments.HeadId = UserId` заведующего).
- Email-провайдер настроен через конфиг:
  - `Email:Provider = "Smtp"` для `Development/Production`;
  - `Email:Provider = "Log"` только для `Testing`.
- SMTP параметры (`Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__FromAddress`, `Smtp__EnableSsl`) заданы в переменных окружения.

### 1) Логин под тремя ролями
Для каждого пользователя выполнить:
- `POST /api/v1/auth/login`

```json
{
  "email": "user@example.com",
  "password": "password"
}
```

Сохранить токены:
- `studentToken`
- `teacherToken`
- `deptHeadToken`

### 2) Студент создаёт запрос научруку
- `POST /api/v1/supervisor-requests` (Bearer `studentToken`)

```json
{
  "teacherUserId": "<teacher_user_id>",
  "comment": "Прошу принять меня на научное руководство"
}
```

Ожидание:
- `201 Created`
- в ответе есть `id` (сохранить как `supervisorRequestId`)

Проверка уведомления научрука:
- `GET /api/v1/notifications?isRead=false` (Bearer `teacherToken`)
- есть уведомление с заголовком: `Новый запрос на научное руководство`

### 2.1) Студент отменяет запрос (доп. кейс)
- `PUT /api/v1/supervisor-requests/{supervisorRequestId}/cancel` (Bearer `studentToken`)

Ожидание:
- `200 OK`

Проверка уведомления научрука:
- `GET /api/v1/notifications?isRead=false` (Bearer `teacherToken`)
- есть уведомление с заголовком: `Запрос на научное руководство отменён`

Для продолжения основного сценария создать новый `SupervisorRequest` заново (повтор шага 2) и использовать новый `supervisorRequestId`.

### 3) Научрук одобряет запрос
- `PUT /api/v1/supervisor-requests/{supervisorRequestId}/approve` (Bearer `teacherToken`)

Ожидание:
- `200 OK`

### 4) Студент создаёт заявку на ВКР
- `POST /api/v1/applications` (Bearer `studentToken`)

```json
{
  "topicId": null,
  "supervisorRequestId": "<supervisorRequestId>",
  "proposedTitle": "Тестовая тема ВКР",
  "proposedDescription": "Проверка уведомлений"
}
```

Ожидание:
- `201 Created`
- в ответе есть `id` (сохранить как `applicationId`)

### 5) Научрук доводит заявку до завкафа
1. `PUT /api/v1/applications/{applicationId}/approve` (Bearer `teacherToken`)
2. `PUT /api/v1/applications/{applicationId}/submit-to-department-head` (Bearer `teacherToken`)

Ожидание:
- оба запроса возвращают `200 OK`

Проверка уведомления завкафа:
- `GET /api/v1/notifications?isRead=false` (Bearer `deptHeadToken`)
- есть уведомление с заголовком: `Новая заявка на рассмотрение`

### 6) Дополнительная проверка уведомлений студента
После шага 5:
- `GET /api/v1/notifications?isRead=false` (Bearer `studentToken`)

Ожидание:
- есть уведомление о смене статуса заявки (например, одобрение научруком)

### 7) Проверка доставки email (SMTP)

Проверки:
- в логах backend есть строки вида `Email sent via SMTP to ...`;
- на реальных почтовых ящиках получателей приходят письма по событиям выше.

Пример для Docker:

```bash
docker logs backend_app --since 10m
```

### 8) Критерий успешной проверки
Сценарий считается успешным, если:
- у `Teacher` появилось уведомление после создания `SupervisorRequest`;
- у `Teacher` появилось уведомление после отмены `SupervisorRequest` студентом;
- у `DepartmentHead` появилось уведомление после `submit-to-department-head`;
- у `Student` продолжают приходить уведомления по смене статусов;
- письма фактически отправляются через SMTP (есть лог отправки и письмо в почтовом ящике);
- в корректном сценарии нет неожиданных `4xx/5xx`.
