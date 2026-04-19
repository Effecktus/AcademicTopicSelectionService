# Сценарий проверки архива ВКР (Swagger)

Проверка цепочки: создание записи ВКР → presigned upload → подтверждение → ссылка на скачивание. Базовый префикс API: `/api/v1`.

## 0) Предусловия

- Swagger открыт, выбрана версия API **v1**.
- В Swagger нажата **Authorize**: заголовок `Authorization: Bearer <accessToken>`.
- База данных инициализирована init-скриптами; для готовых тестовых пользователей и заявок удобно наличие **`99_seed_test_data.sql`**.
- **Создание записи ВКР, выдача upload-url и confirm-upload** доступны только роли **Admin**.
- **Скачивание** (`download-url`) — любой авторизованный пользователь.

### Учётные данные из сида (если используется `99_seed_test_data.sql`)

Пароль для всех: `TestPassword123!`

| Роль   | Email                 |
|--------|------------------------|
| Admin  | `z_admin@example.com` |
| Student| `student01@example.com` (для проверки download под не-админом) |

### Провайдер объектного хранилища

- **`S3__Provider=Development`**: см. **п. 4.2** — PUT не нужен, после `upload-url` сразу `confirm-upload`.
- **`S3__Provider=S3`** (MinIO / AWS): см. **п. 4.1** — обязательный **PUT** файла по `url` из ответа, затем `confirm-upload`.

## 1) Логин администратора

- `POST /api/v1/auth/login`

```json
{
  "email": "z_admin@example.com",
  "password": "TestPassword123!"
}
```

Сохранить из ответа: `accessToken` → подставить в **Authorize** как `Bearer <accessToken>`.

## 2) Подготовка: удалить существующую запись ВКР (опционально)

В сиде у заявок уже могут быть строки `GraduateWorks`. Чтобы прогнать полный цикл «с нуля»:

- `GET /api/v1/graduate-works` — выбрать `id` записи, привязанной к нужной заявке (смотреть `applicationId`), либо любую тестовую запись.
- `DELETE /api/v1/graduate-works/{id}`

Ожидание: `204 No Content`.

Если тестируете только скачивание существующего файла — шаг можно пропустить.

## 3) Создать запись архива ВКР

- `POST /api/v1/graduate-works`

Тело (подставить реальный `applicationId` заявки, у которой ещё нет записи ВКР; UUID из `GET /api/v1/applications` под студентом или из БД):

```json
{
  "applicationId": "00000000-0000-0000-0000-000000000000",
  "title": "Тема ВКР — тест Swagger",
  "year": 2025,
  "grade": 85,
  "commissionMembers": "Иванов И.И.; Петров П.П."
}
```

Ожидание: `201 Created`, в теле — объект с полем `id` (сохранить как `graduateWorkId`).

## 4) Получить URL для загрузки файла

- `POST /api/v1/graduate-works/{graduateWorkId}/upload-url/{fileType}`

В пути `fileType` допустимо только:

- `thesis` — текст ВКР;
- `presentation` — презентация.

Ожидание: `200 OK`, тело с `url` и `expiresAt`.

Дальше см. **п. 4.1** (реальная загрузка) или **п. 4.2** (режим Development).

## 4.1) Реальная загрузка файла в хранилище (S3 / MinIO, `S3__Provider=S3`)

Бэкенд отдаёт **presigned URL** на **HTTP PUT**: объект в бакете уже зафиксирован по ключу вида  
`graduate-works/{graduateWorkId}/{fileType}` — вручную ключ подбирать не нужно, он «зашит» в подпись ссылки.

**Swagger UI обычно не умеет** выполнить произвольный `PUT` на внешний URL из ответа. Загрузку делайте **curl**, **Postman**, **Insomnia** или скриптом.

### Вариант A: curl

1. Скопируйте из ответа шага 4 полное значение **`url`** (вся строка, включая `?…` с query-параметрами подписи).
2. Выполните **PUT** с **сырым телом файла** (бинарный поток):

```bash
curl -X PUT "ВСТАВЬТЕ_URL_ИЗ_ОТВЕТА_API_ЦЕЛИКОМ" \
  --data-binary @/путь/к/файлу/Диплом_Иванов.pdf
```

Рекомендации:

- Оборачивайте URL в **одинарные кавычки** в shell, если в нём есть `&`, иначе оболочка обрежет query.
- По возможности **не добавляйте лишние заголовки** (`Content-Type` и т.д.): для presigned URL подпись завязана на набор параметров; лишнее часто даёт `403 SignatureDoesNotMatch`.
- Если MinIO доступен по `http://localhost:9000`, а ссылка с другим хостом — используйте тот хост, который вернул API (он должен совпадать с `S3__PublicEndpoint` / настройкой presign).

Ожидание от S3/MinIO: **`200`** или **`204`** на `PUT` (успешная загрузка объекта).

### Вариант B: Postman / Insomnia

1. Метод **PUT**, в поле URL вставьте **`url`** из ответа API.
2. Body → **binary** / **file** → выберите файл на диске.
3. Не добавляйте произвольные заголовки, если не уверены в требованиях подписи.
4. Отправьте запрос; при успехе статус обычно **200** или **204**.

### Проверка: файл действительно лежит в объектном хранилище

Имеет смысл только при **`S3__Provider=S3`** (MinIO, AWS и т.п.). В **`Development`** объекта в реальном бакете **нет** — см. п. 4.2.

Ожидаемый **ключ объекта** (папка в UI MinIO — по префиксу):

```text
graduate-works/{graduateWorkId}/{fileType}
```

Подставьте свой UUID записи ВКР из шага 3 и `thesis` или `presentation` вместо `{fileType}`. Пример: `graduate-works/3fa85f64-5717-4562-b3fc-2c963f66afa6/thesis`.

**Через веб-консоль MinIO** (типичный `compose.backend.yml`):

1. Откройте в браузере консоль, например `http://localhost:9001` (порт смотрите в compose: `9001:9001`).
2. Войдите под **root** (логин и пароль из `infra/docker/secrets/minio_access_key.txt` и `minio_secret_key.txt`).
3. Откройте бакет из настройки **`S3__BucketName`** (по умолчанию `graduate-works`).
4. Найдите префикс `graduate-works/<ваш-guid>/` — внутри должен быть объект `thesis` или `presentation` (в зависимости от `fileType`).

**Через MinIO Client (`mc`)** с хоста (если установлен `mc`):

```bash
mc alias set local http://localhost:9000 "$(cat infra/docker/secrets/minio_access_key.txt)" "$(cat infra/docker/secrets/minio_secret_key.txt | tr -d '\n')"
mc ls local/graduate-works/graduate-works/<graduateWorkId>/
```

Путь к секретам подставьте свой; при запуске `mc` с другой машины вместо `localhost` укажите доступный хост MinIO.

**Через AWS CLI** (если настроен профиль под тот же endpoint и path-style):

```bash
aws s3 ls "s3://graduate-works/graduate-works/<graduateWorkId>/" --endpoint-url http://localhost:9000
```

Если объекта **нет**, шаг **5** (`confirm-upload`) в API вернёт ошибку валидации («Object not found in storage») — это тоже косвенная проверка, но визуально удобнее смотреть бакет до вызова `confirm-upload`.

### После успешного PUT и проверки в бакете

Вернитесь в Swagger и выполните шаг **5** (`confirm-upload`) с тем же `fileName`, что и у загруженного файла (имя с расширением). Бэкенд ещё раз проверит наличие объекта в хранилище и запишет метаданные в БД.

## 4.2) Режим `S3__Provider=Development` (заглушка)

Загрузка по сети **не требуется**: `ObjectExistsAsync` для ключа всегда успешен. Достаточно шагов **4** → сразу **5** (`confirm-upload`) с желаемым `fileName`.

**Файл в реальном MinIO/S3 при этом не появляется** — проверить «физическое» наличие байтов в бакете в этом режиме нельзя; для такой проверки запускайте API с **`S3__Provider=S3`** и реальным MinIO (как в Docker compose backend).

## 5) Подтвердить успешную загрузку

- `POST /api/v1/graduate-works/{graduateWorkId}/confirm-upload/{fileType}`

Тот же `fileType`, что в шаге 4.

Тело:

```json
{
  "fileName": "Диплом_Иванов.pdf"
}
```

Ожидание: `204 No Content`.

Проверка: `GET /api/v1/graduate-works/{graduateWorkId}` — обновились поля `hasFile` / `hasPresentation`, `fileName` / `presentationFileName` (в зависимости от типа).

## 6) Получить ссылку на скачивание

- `GET /api/v1/graduate-works/{graduateWorkId}/download-url/{fileType}`

Допустимо под любым валидным JWT (например, повторный логин под `student01@example.com`).

Ожидание: `200 OK`, временная ссылка в поле `url`.

## 7) Негативные проверки (по желанию)

| Действие | Ожидание |
|----------|----------|
| `POST .../upload-url/...` без роли Admin | `403` |
| Неверный `fileType` (например `pdf`) | `400` |
| Несуществующий `graduateWorkId` | `404` |
| Запросы без Bearer-токена | `401` |
