## 0) Подготовка окружения

### Шаг 0.1 — Поднять сервисы
**Действие:** запустить backend + БД + Redis (обычным способом).  
**Ожидание:** API отвечает на health-check (`200 OK`).

### Шаг 0.2 — Подготовить пользователей и токены
**Действие:** получить JWT для:
- студента (`studentToken`)
- преподавателя (`teacherToken`)
- зав. кафедрой (`deptHeadToken`)

**Ожидание:** каждый токен валиден, запросы с ними проходят авторизацию.

### Шаг 0.3 — Проверить справочники
**Действие:** убедиться, что в БД/справочниках есть:
- статусы заявок: `Pending`, `ApprovedBySupervisor`, `PendingDepartmentHead`, `ApprovedByDepartmentHead`, `RejectedBySupervisor`, `RejectedByDepartmentHead`, `Cancelled`;
- `TopicCreatorType = Student`;
- `TopicStatus = Active`.

**Ожидание:** все значения существуют (иначе создание заявки может падать по validation).

## 1) Подготовка обязательной предпосылки — SupervisorRequest

### Шаг 1.1 — Студент создает запрос на научрука
**Действие:** `POST /api/v1/supervisor-requests` от студента:
```json
{
  "teacherUserId": "<teacherUserId>",
  "comment": "Прошу стать научруком"
}
```
**Ожидание:** `201 Created`, в ответе есть `id` (сохранить как `supervisorRequestId`), статус `Pending`.

### Шаг 1.2 — Преподаватель одобряет запрос
**Действие:** `PUT /api/v1/supervisor-requests/{supervisorRequestId}/approve` от преподавателя.  
**Ожидание:** `200 OK`, статус запроса `ApprovedBySupervisor`.

## 2) Сценарий A — подача заявки на существующую тему

### Шаг 2.1 — Создать активную тему
**Действие:** `POST /api/v1/topics` от преподавателя:
```json
{
  "title": "Тема A",
  "description": "Описание A",
  "creatorTypeCodeName": "Teacher",
  "statusCodeName": "Active"
}
```
**Ожидание:** `201 Created`, сохранить `topicId`.

### Шаг 2.2 — Подать заявку студентом
**Действие:** `POST /api/v1/applications` от студента:
```json
{
  "topicId": "<topicId>",
  "supervisorRequestId": "<supervisorRequestId>"
}
```
**Ожидание:**
- `201 Created`;
- `status.codeName = "Pending"`;
- `topicId` совпадает с переданным;
- `supervisorRequestId` совпадает с переданным.

### Шаг 2.3 — Проверить детали заявки
**Действие:** `GET /api/v1/applications/{applicationId}`.  
**Ожидание:**
- заявка существует;
- есть действие в истории со статусом `Pending`;
- корректно подтянуты студент/тема/научрук.

## 3) Сценарий B — one-step: студент предлагает новую тему прямо в заявке

### Шаг 3.1 — Создать второй одобренный SupervisorRequest (для чистоты сценария)
**Действие:** повторить шаги 1.1–1.2.  
**Ожидание:** новый `supervisorRequestId2` в статусе `ApprovedBySupervisor`.

### Шаг 3.2 — Подать заявку с `proposedTitle`
**Действие:** `POST /api/v1/applications` от студента:
```json
{
  "supervisorRequestId": "<supervisorRequestId2>",
  "proposedTitle": "Тема от студента одним запросом",
  "proposedDescription": "Описание от студента"
}
```
**Ожидание:**
- `201 Created`;
- `status.codeName = "Pending"`;
- `topicTitle = "Тема от студента одним запросом"`;
- `topicCreatedByUserId = <studentUserId>`.

### Шаг 3.3 — Проверить, что тема реально создана
**Действие:**
- взять `topicId` из ответа заявки;
- вызвать `GET /api/v1/topics/{topicId}` (или проверить через `GET /topics` фильтрами/БД).
**Ожидание:**
- тема существует;
- `creatorType.codeName = "Student"`;
- `status.codeName = "Active"`.

## 4) Негативные проверки валидации `POST /applications`

Для каждого кейса: отправить запрос и проверить код/ошибку.

### Шаг 4.1 — Не передали ни `topicId`, ни `proposedTitle`
```json
{
  "supervisorRequestId": "<id>"
}
```
**Ожидание:** `400 BadRequest` (validation).

### Шаг 4.2 — Передали и `topicId`, и `proposedTitle` одновременно
```json
{
  "topicId": "<id>",
  "supervisorRequestId": "<id>",
  "proposedTitle": "Лишнее поле"
}
```
**Ожидание:** `400 BadRequest`.

### Шаг 4.3 — `supervisorRequestId = Guid.Empty`
**Ожидание:** `400 BadRequest`.

### Шаг 4.4 — `topicId` не существует
**Ожидание:** `404 NotFound` (`Topic not found`).

### Шаг 4.5 — Тема не активна (`Inactive`)
**Ожидание:** `400 BadRequest` (тема не принимает заявки).

### Шаг 4.6 — Пустой `proposedTitle` (пробелы)
**Ожидание:** `400 BadRequest`.

### Шаг 4.7 — Длинный `proposedTitle` (>500)
**Ожидание:** `400 BadRequest`.

## 5) Негативные проверки бизнес-ограничений

### Шаг 5.1 — Пользователь не Student
**Действие:** тот же `POST /applications` от teacher/deptHead.  
**Ожидание:** `403 Forbidden`.

### Шаг 5.2 — Нет профиля студента
**Действие:** студент с ролью есть, записи в `Students` нет.  
**Ожидание:** `400 BadRequest` (`Student profile not found`).

### Шаг 5.3 — `SupervisorRequest` чужой
**Ожидание:** `400 BadRequest` (не принадлежит студенту).

### Шаг 5.4 — `SupervisorRequest` не одобрен
**Ожидание:** `400 BadRequest` (`Approved supervisor request not found...`).

### Шаг 5.5 — У студента уже есть активная заявка
**Ожидание:** `400 BadRequest`.

### Шаг 5.6 — Тема уже занята активной заявкой другого студента
**Ожидание:** `409 Conflict`.

## 6) Мини-проверка полного флоу после подачи

### Шаг 6.1 — Одобрение научруком
`PUT /applications/{id}/approve` от teacher.  
**Ожидание:** `200`, статус `ApprovedBySupervisor`.

### Шаг 6.2 — Отправка зав. кафедрой
`PUT /applications/{id}/submit-to-department-head` от teacher.  
**Ожидание:** `200`, статус `PendingDepartmentHead`.

### Шаг 6.3 — Одобрение зав. кафедрой
`PUT /applications/{id}/department-head-approve` от deptHead.  
**Ожидание:** `200`, статус `ApprovedByDepartmentHead`.

### Шаг 6.4 — Проверка запрета отмены из финального статуса
`PUT /applications/{id}/cancel` от student.  
**Ожидание:** `409 Conflict`.

## 7) Критерий «функционал принят»

Считать подачу заявок принятой, если:
- оба сценария создания (`topicId` и `proposedTitle`) стабильно дают `201`;
- валидации «либо `topicId`, либо `proposedTitle`» работают строго;
- бизнес-ограничения (роль, supervisor request, uniqueness, active-topic) отрабатывают корректными кодами;
- история действий содержит стартовое `Pending`;
- дальнейший workflow по статусам проходит без регрессий.
