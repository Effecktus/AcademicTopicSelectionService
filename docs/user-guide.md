## Быстрый запуск (одной командой)

```bash
# Клонируем и заходим в проект
git clone https://github.com/yourname/diploma-vkr-platform.git
cd diploma-vkr-platform

# Запускаем всё (dev-окружение)
cd infra/docker
docker compose -f compose.dev.yml up --build -d
```
