# CitizenPlatform production paketleme:
#   API + Worker'ı Release publish eder, iki SPA'yı build eder, IIS web.config'lerini kopyalar.
# Çıktı: publish/api, publish/worker, publish/citizen-web, publish/panel-web
#
# SPA'lar /api'yi GÖRELI çağırır; ayrı domainde SPA sitesi /api'yi API'ye reverse-proxy'ler
# (deploy/spa/web.config, ARR). Bu yüzden build sırasında API URL'i gerekmez.
#
# Kullanım:  ./scripts/publish.ps1

param(
    [string]$OutDir = "publish"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "== API publish ==" -ForegroundColor Cyan
dotnet publish "$root/backend/src/CitizenPlatform.Api/CitizenPlatform.Api.csproj" -c Release -o "$root/$OutDir/api"
Copy-Item "$root/deploy/api/web.config" "$root/$OutDir/api/web.config" -Force

Write-Host "== Worker publish ==" -ForegroundColor Cyan
dotnet publish "$root/backend/src/CitizenPlatform.Worker/CitizenPlatform.Worker.csproj" -c Release -o "$root/$OutDir/worker"

Write-Host "== Vatandas (citizen-web) build ==" -ForegroundColor Cyan
npm --workspace "@citizen-platform/citizen-web" run build
Remove-Item "$root/$OutDir/citizen-web" -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item "$root/apps/citizen-web/dist" "$root/$OutDir/citizen-web" -Recurse
Copy-Item "$root/deploy/spa/web.config" "$root/$OutDir/citizen-web/web.config" -Force

Write-Host "== Panel (admin-web) build ==" -ForegroundColor Cyan
npm --workspace "@citizen-platform/admin-web" run build
Remove-Item "$root/$OutDir/panel-web" -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item "$root/apps/admin-web/dist" "$root/$OutDir/panel-web" -Recurse
Copy-Item "$root/deploy/spa/web.config" "$root/$OutDir/panel-web/web.config" -Force

Write-Host "`nTamamlandi. Cikti: $root/$OutDir" -ForegroundColor Green
Write-Host "  api/         -> API IIS sitesi (ASP.NET Core Hosting Bundle)" -ForegroundColor Green
Write-Host "  worker/      -> arka plan servisi (Windows Service)" -ForegroundColor Green
Write-Host "  citizen-web/ -> vatandas SPA sitesi" -ForegroundColor Green
Write-Host "  panel-web/   -> belediye panel SPA sitesi" -ForegroundColor Green
