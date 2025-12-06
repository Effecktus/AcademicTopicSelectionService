# create-project-structure.ps1
# Создает структуру проекта для дипломной работы

$projectRoot = $PWD.Path  # Текущая директория как root

# Функция для создания директории, если не существует
function Create-Directory {
    param ([string]$path)
    if (-not (Test-Path $path)) {
        New-Item -Path $path -ItemType Directory | Out-Null
        Write-Host "Created directory: $path"
    }
}

# Функция для создания пустого файла, если не существует
function Create-EmptyFile {
    param ([string]$path)
    if (-not (Test-Path $path)) {
        New-Item -Path $path -ItemType File | Out-Null
        Write-Host "Created file: $path"
    }
}

# frontend/
Create-Directory "$projectRoot\frontend"
Create-Directory "$projectRoot\frontend\src"
Create-Directory "$projectRoot\frontend\tests"
Create-Directory "$projectRoot\frontend\e2e"
Create-EmptyFile "$projectRoot\frontend\Dockerfile"
Create-EmptyFile "$projectRoot\frontend\nginx.conf"

# backend/
Create-Directory "$projectRoot\backend"
Create-Directory "$projectRoot\backend\src"
Create-Directory "$projectRoot\backend\tests"
Create-Directory "$projectRoot\backend\Migrations"
Create-EmptyFile "$projectRoot\backend\appsettings.Development.json"
Create-EmptyFile "$projectRoot\backend\appsettings.Production.json"
Create-EmptyFile "$projectRoot\backend\Dockerfile"
Create-EmptyFile "$projectRoot\backend\swagger.json"

# infra/
Create-Directory "$projectRoot\infra"
Create-Directory "$projectRoot\infra\docker"
Create-Directory "$projectRoot\infra\docker\secrets"
Create-EmptyFile "$projectRoot\infra\docker\compose.dev.yml"
Create-EmptyFile "$projectRoot\infra\docker\compose.prod.yml"
Create-EmptyFile "$projectRoot\infra\docker\secrets\postgres_password.txt"
Create-EmptyFile "$projectRoot\infra\docker\secrets\redis_password.txt"
Create-EmptyFile "$projectRoot\infra\docker\secrets\minio_access_key.txt"
Create-EmptyFile "$projectRoot\infra\docker\secrets\minio_secret_key.txt"  # Добавил для полноты

Create-Directory "$projectRoot\infra\db"
Create-Directory "$projectRoot\infra\db\init"
Create-Directory "$projectRoot\infra\db\migrations"
Create-Directory "$projectRoot\infra\db\backups"

Create-Directory "$projectRoot\infra\monitoring"
Create-EmptyFile "$projectRoot\infra\monitoring\prometheus.yml"
Create-Directory "$projectRoot\infra\monitoring\grafana"

Create-Directory "$projectRoot\infra\terraform"
Create-EmptyFile "$projectRoot\infra\terraform\main.tf"
Create-EmptyFile "$projectRoot\infra\terraform\variables.tf"

# docs/
Create-Directory "$projectRoot\docs"
Create-Directory "$projectRoot\docs\api"
Create-EmptyFile "$projectRoot\docs\architecture.md"
Create-EmptyFile "$projectRoot\docs\user-guide.md"

# .github/
Create-Directory "$projectRoot\.github"
Create-Directory "$projectRoot\.github\workflows"
Create-EmptyFile "$projectRoot\.github\workflows\ci.yml"
Create-EmptyFile "$projectRoot\.github\workflows\cd.yml"

# Root files
Create-EmptyFile "$projectRoot\.gitignore"
Create-EmptyFile "$projectRoot\README.md"
Create-EmptyFile "$projectRoot\LICENSE"

Write-Host "Структура проекта создана успешно!"