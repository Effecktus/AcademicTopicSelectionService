# Сценарий проверки чата по заявке (Swagger)

Чат привязан к **заявке** (`StudentApplication`). Участники: студент-владелец и преподаватель из связанного **одобренного или ожидающего** запроса на научрука (`SupervisorRequest` в статусах `Pending` / `ApprovedBySupervisor`). Обновление списка сообщений — **polling** (повторные `GET` с опциональным `afterId`).

Базовый префикс API: `/api/v1`.

## 0) Предусловия

- Swagger, версия **v1**, кнопка **Authorize** с `Bearer <accessToken>`.
- Init-скрипты БД; для готовой пары студент–научрук–заявка удобно **`99_seed_test_data.sql`**.

### Учётные данные из сида (если используется `99_seed_test_data.sql`)

Пароль: `TestPassword123!`

| Роль    | Email                   |
|---------|--------------------------|
| Student | `student01@example.com` |
| Teacher | `teacher01@example.com` |
| DepartmentHead | `head01@example.com` (для негативного кейса) |
| Другой студент | `student02@example.com` (для негативного кейса) |

## 1) Логин студента

- `POST /api/v1/auth/login`

```json
{
  "email": "student01@example.com",
  "password": "TestPassword123!"
}
```

Сохранить `accessToken`, авторизоваться в Swagger.

## 2) Узнать идентификатор заявки

- `GET /api/v1/applications`

Из массива `items` взять `id` нужной заявки → сохранить как `applicationId`.

При необходимости детали: `GET /api/v1/applications/{applicationId}` — только для заявок, которые эта же роль видит в списке; иначе **404**.

## 3) Получить список сообщений (начальная выборка)

- `GET /api/v1/applications/{applicationId}/messages`

Опциональные query-параметры:

- `limit` — ограничение количества (по умолчанию до 50 последних сообщений в хронологическом порядке);
- `afterId` — UUID сообщения-курсора: вернуть только более новые сообщения (для polling).

Ожидание: `200 OK`, массив объектов с полями `id`, `senderId`, `senderFullName`, `content`, `sentAt`, `readAt`.

## 4) Отправить сообщение (студент)

- `POST /api/v1/applications/{applicationId}/messages`

Тело:

```json
{
  "content": "Здравствуйте, проверка чата из Swagger."
}
```

Ожидание: `201 Created`, в ответе новое сообщение, у входящих для собеседника `readAt` обычно `null`.

## 5) Логин преподавателя и ответ в чате

1. **Authorize** — выйти из сессии студента, выполнить логин:

- `POST /api/v1/auth/login`

```json
{
  "email": "teacher01@example.com",
  "password": "TestPassword123!"
}
```

2. Тот же `POST /api/v1/applications/{applicationId}/messages` с другим текстом в `content`.

Ожидание: `201 Created`.

## 6) Отметить все входящие прочитанными

Под токеном **преподавателя** (получатель сообщений студента):

- `PUT /api/v1/applications/{applicationId}/messages/read-all`

Тело запроса не требуется.

Ожидание: `204 No Content`.

Проверка: снова `GET /api/v1/applications/{applicationId}/messages` — у сообщений, отправленных студентом, поле **`readAt`** заполнено (для преподавателя они были входящими).

## 7) Polling новых сообщений

После известного последнего `id` сообщения:

- `GET /api/v1/applications/{applicationId}/messages?afterId=<guid>`

Ожидание: только сообщения новее курсора; если новых нет — пустой массив.

## 8) Негативные проверки

| Действие | Ожидание |
|----------|----------|
| `GET .../messages` под `head01@example.com` для чужой заявки студента | `403 Forbidden` |
| `POST .../messages` под `student02@example.com` к заявке `student01` | `403 Forbidden` |
| Пустой или пробельный `content` | `400 Bad Request` |
| `content` длиннее 4000 символов | `400 Bad Request` |
| Несуществующий `applicationId` | `404 Not Found` |
| Запросы без Bearer | `401 Unauthorized` |
