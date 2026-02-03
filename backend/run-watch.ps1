param(
  [int]$ApiPort = 5000,
  [int]$DbPort = 5433
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$composePath = Join-Path $repoRoot "infra\\docker\\compose.db.yml"
$dbPasswordFile = Join-Path $repoRoot "infra\\docker\\secrets\\postgres_password.txt"
$apiProject = Join-Path $repoRoot "backend\\src\\DirectoryOfGraduates.API\\DirectoryOfGraduates.API.csproj"

if (-not (Test-Path $composePath)) {
  throw "Compose file not found: $composePath"
}

if (-not (Test-Path $dbPasswordFile)) {
  throw "DB password file not found: $dbPasswordFile"
}

# Пароль в ENV не кладём: будем читать из файла через Db__PasswordFile
$dbPassword = (Get-Content -Raw $dbPasswordFile).Trim()
if ([string]::IsNullOrWhiteSpace($dbPassword)) {
  throw "DB password file is empty: $dbPasswordFile"
}

Write-Host "Starting Postgres (compose.db.yml)..." -ForegroundColor Cyan

# Быстрая диагностика: если Docker не запущен — скажем сразу понятную причину
try {
  docker info *> $null
} catch {
  throw "Docker недоступен. Запусти Docker Desktop и повтори."
}

# Поднимаем БД (по возможности ждём healthcheck)
try {
  docker compose -f $composePath up -d --wait | Out-Host
} catch {
  docker compose -f $composePath up -d | Out-Host
}

Write-Host "Setting environment variables for watch run..." -ForegroundColor Cyan
$env:ASPNETCORE_ENVIRONMENT = "Development"
# Порт задаём через --urls ниже, чтобы не цеплялся launchSettings.json
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=$DbPort;Database=app_database;Username=app_user"
$env:Db__PasswordFile = $dbPasswordFile

# Optional: don't auto open browser
$env:DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER = "1"

Write-Host "Running API with dotnet watch on http://localhost:$ApiPort" -ForegroundColor Green
Write-Host "Swagger: http://localhost:$ApiPort/swagger" -ForegroundColor Green
Write-Host ""

dotnet watch --project $apiProject run --no-launch-profile --urls "http://localhost:$ApiPort"