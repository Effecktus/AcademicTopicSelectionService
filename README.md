# Платформа хранения и совместной работы над ВКР

**Дипломная работа** — Иванов Иван Иванович, группа ХХ-ХХ

![Angular](https://img.shields.io/badge/Angular-DD0031?logo=angular&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?logo=.net&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-336791?logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?logo=redis&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)

## Описание проекта

Веб-приложение для хранения выпускных квалификационных работ студентов с возможностью:

- Загрузки и просмотра ВКР
- Совместной работы студент ↔ научный руководитель (комментарии, версии)
- Формирования рейтинга научных руководителей
- Хранения файлов в объектном хранилище (S3 / MinIO)

## Технологический стек

| Слой               | Технология                         |
| ------------------ | ---------------------------------- |
| Frontend           | Angular 18 + TypeScript + SCSS     |
| Backend            | ASP.NET Core 8 (Minimal API / MVC) |
| База данных        | PostgreSQL 16 + EF Core Migrations |
| Кэширование        | Redis                              |
| Файловое хранилище | AWS S3 (prod) / MinIO (dev)        |
| Контейнеризация    | Docker + Docker Compose            |
| Мониторинг         | Prometheus + Grafana (dev)         |
| CI/CD              | GitHub Actions                     |

## Быстрый запуск (одной командой)

```bash
# Клонируем и заходим в проект
git clone https://github.com/yourname/diploma-vkr-platform.git
cd diploma-vkr-platform

# Запускаем всё (dev-окружение)
cd infra/docker
docker compose -f compose.dev.yml up --build -d
```

## Развертывание контейнера для разработки БД (PostgreSQL)

Если ты хочешь работать только с БД (например, для создания схемы, миграций или тестирования запросов), то можно поднять только PostgreSQL контейнер. Это позволит подключиться к нему из инструментов вроде pgAdmin, DBeaver или через psql в терминале. Не нужно поднимать весь стек — это сэкономит ресурсы.

### Предварительные требования

- Docker установлен и запущен.
- У тебя есть файл `infra/docker/secrets/postgres_password.txt` с паролем (например, запиши туда "your_secure_password").
- Если есть init-скрипты в `infra/db/init/`, они автоматически применятся при первом запуске.

### Инструкции по развертыванию (только PostgreSQL)

1. Перейди в директорию с compose файлом:

```bash
cd infra/docker
```

2. Создай временный compose файл только для postgres (или используй фрагмент из compose.dev.yml). Скопируй это в новый файл `compose.db.yml` в той же папке:

```yaml
version: "3.9"

services:
  postgres:
    image: postgres:16
    container_name: postgres_db
    restart: unless-stopped
    environment:
      POSTGRES_USER: app_user
      POSTGRES_DB: app_database
      POSTGRES_PASSWORD_FILE: /run/secrets/postgres_password
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ../../db/init:/docker-entrypoint-initdb.d:ro # Init скрипты
      - ../../db/backups:/backups
      - ../../db/migrations:/migrations:ro # Для миграций
    secrets:
      - postgres_password
    networks:
      - db_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U app_user -d app_database"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s
    ports:
      - "5432:5432" # Доступ с хоста

volumes:
  postgres_data:

networks:
  db_network:
    driver: bridge

secrets:
  postgres_password:
    file: ./secrets/postgres_password.txt
```

3. Запусти контейнер:

   ```bash
   docker compose -f compose.db.yml up -d
   ```

   - `-d` — в фоне.
   - Проверь статус: `docker ps` (должен видеть postgres_db).

4. Подключись к БД для разработки:

   - Через терминал (psql): `docker exec -it postgres_db psql -U app_user -d app_database`
   - Через GUI (pgAdmin): Укажи хост `localhost`, порт 5432, user `app_user`, db `app_database`, пароль из secrets.
   - Теперь можно создавать таблицы, применять миграции (если используешь EF Core, подключись из Visual Studio или dotnet CLI).

5. Остановка:
   ```bash
   docker compose -f compose.db.yml down
   ```
   - С volumes: `docker compose -f compose.db.yml down -v` (удалит данные, осторожно!).

Это минимальный setup для БД-разработки. Если нужно применить миграции автоматически, добавь скрипт в `db/migrations` и вызови его в entrypoint (но для dev лучше вручную).

## Развертывание backend с необходимыми контейнерами

Для backend нужно поднять зависимости: PostgreSQL (для данных), Redis (кэш), MinIO (файлы). Frontend не обязателен, если тестируешь API (через Postman/Swagger). Это позволит работать с backend в изоляции: запускать миграции, сеедить данные, тестировать endpoints.

### Предварительные требования

- Docker установлен.
- Secrets файлы заполнены: `postgres_password.txt`, `redis_password.txt`, `minio_access_key.txt` (например, "minioadmin"), `minio_secret_key.txt` (минимум 8 символов).
- Dockerfile в `backend/` готов (build backend image).
- Если используешь мониторинг, добавь `prometheus.yml` в `infra/monitoring`.

### Инструкции по развертыванию (backend + зависимости)

1. Перейди в директорию:

   ```bash
   cd infra/docker
   ```

2. Используй `compose.dev.yml` (из предыдущих сообщений), но если хочешь только backend-часть, создай `compose.backend.yml` с этими сервисами (убери frontend, grafana, prometheus если не нужны):

   ```yaml
   version: "3.9"

   services:
     postgres:# Копировать из compose.dev.yml
       # ... (полный блок postgres)

     redis: # ... (полный блок redis)

     minio: # ... (полный блок minio)

     backend: # ... (полный блок backend)

   volumes:
     postgres_data:
     redis_data:
     minio_data:

   networks:
     backend_network:
       driver: bridge

   secrets:
     postgres_password:
       file: ./secrets/postgres_password.txt
     redis_password:
       file: ./secrets/redis_password.txt
     minio_access_key:
       file: ./secrets/minio_access_key.txt
     minio_secret_key:
       file: ./secrets/minio_secret_key.txt
   ```

3. Собери и запусти:

   ```bash
   docker compose -f compose.backend.yml up --build -d
   ```

   - `--build` — пересоберёт images если изменился код.
   - Жди healthchecks (проверь логи: `docker logs backend_app`).

4. Доступ к сервисам:

   - Backend API: `http://localhost:5000` (Swagger: `/swagger` если включен).
   - MinIO console: `http://localhost:9001` (login: из access_key, пароль: из secret_key).
   - Redis: Подключись через redis-cli: `docker exec -it redis_cache redis-cli -a $(cat secrets/redis_password.txt)`.
   - БД: Как выше, порт 5432.
   - Применение миграций: Если EF Core, запусти из хоста `dotnet ef database update` (в backend dir), или добавь в backend entrypoint.

5. Остановка:
   ```bash
   docker compose -f compose.backend.yml down
   ```

Это позволит разрабатывать backend: тестировать API, кэш, файлы без фронта. Для авторизации (JWT/etc.) используй Postman.

## Развертывание frontend с необходимыми компонентами

Ты прав: frontend зависит от backend для данных, авторизации и API. Поднимать frontend в изоляции (без backend) можно только для статического UI (mock данные), но для полноценной работы (ВКР, комментарии, рейтинги) нужен весь стек. Поэтому инструкции для полного проекта, но с фокусом на frontend. Если хочешь mock — используй Angular dev server с proxies.

### Предварительные требования

- Те же, что для backend.
- Dockerfile в `frontend/` (build Angular).
- В `frontend/environment.ts` укажи API_URL: 'http://localhost:5000' (или через env vars).

### Инструкции по развертыванию (frontend + backend + зависимости)

1. Перейди в директорию:

   ```bash
   cd infra/docker
   ```

2. Используй полный `compose.dev.yml` (из предыдущих сообщений) — он включает всё: postgres, redis, minio, backend, frontend.

3. Собери и запусти:

   ```bash
   docker compose -f compose.dev.yml up --build -d
   ```

   - Это поднимет весь стек.
   - Frontend будет доступен на `http://localhost:4200`.
   - Backend на `http://localhost:5000`.

4. Доступ и тестирование:

   - Frontend: Открой браузер на localhost:4200. Он будет обращаться к backend по внутренней сети (API_URL = http://backend:80).
   - Авторизация: Если реализована (JWT), логинись через UI — backend проверит.
   - Данные: Загружай ВКР, комментарии — всё сохранится в БД/файлах.
   - Если нужно мониторинг: Prometheus на 9090, Grafana на 3000 (login: admin/admin по умолчанию).
   - Логи: `docker logs frontend_app` для фронта.

5. Альтернатива: Локальный dev frontend без Docker

   - Если хочешь разрабатывать UI без контейнеров: В `frontend/` запусти `ng serve` (Angular CLI).
   - Но подними backend через compose.backend.yml.
   - В `proxy.conf.json` настрой proxy: `/api` -> `http://localhost:5000`.
   - Запуск: `ng serve --proxy-config proxy.conf.json`.
   - Это быстрее для hot-reload, но данные/авторизация через реальный backend.

6. Остановка:
   ```bash
   docker compose -f compose.dev.yml down
   ```
