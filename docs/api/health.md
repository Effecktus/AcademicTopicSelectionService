## Health

Базовые endpoints для smoke‑проверок и мониторинга.

### GET `/health`

- **Описание**: проверка доступности API (процесс запущен и отвечает).
- **Проверяет БД**: нет
- **Ответ 200**:

```json
{
  "status": "ok",
  "environment": "Development",
  "utc": "2026-02-03T12:34:56.789+00:00"
}
```

### GET `/health/db`

- **Описание**: проверка доступности PostgreSQL из API.
- **Реализация**: `Database.CanConnectAsync()`
- **Ответ 200**:

```json
{
  "status": "ok",
  "db": "postgres",
  "canConnect": true
}
```

