#Requires -RunAsAdministrator
<#
  Güncel kodu yerel IIS'e YENIDEN yayınlar (kurumlar özelliği + tüm düzeltmeler dahil).
  belediyem.local / panel.belediyem.local / api.belediyem.local eski build'i gösterdiğinde çalıştır.

  Sırayla:
    1) IIS uygulama havuzlarını durdurur (yayın klasöründeki DLL kilidini bırakır),
    2) ./scripts/publish.ps1 ile API + iki SPA'yı yeniden paketler,
    3) ./scripts/iis-setup-local.ps1 ile web.config + siteleri tazeler ve başlatır.

  YÖNETİCİ PowerShell'de:  ./scripts/iis-redeploy-local.ps1
  Sonra tarayıcıda Ctrl+F5 (hard refresh). Yeni JWT secret üretildiğinden panele yeniden giriş yap.
#>
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Import-Module WebAdministration

Write-Host "1/3  IIS uygulama havuzları durduruluyor..." -ForegroundColor Cyan
foreach ($pool in "CP_CP-Api", "CP_CP-Citizen", "CP_CP-Panel") {
    if (Test-Path "IIS:\AppPools\$pool") {
        try { Stop-WebAppPool -Name $pool -ErrorAction Stop } catch {}
    }
}
Start-Sleep -Seconds 3

Write-Host "2/3  Yeniden paketleniyor (publish.ps1)..." -ForegroundColor Cyan
& "$root\scripts\publish.ps1"

Write-Host "3/3  IIS siteleri tazeleniyor (setup)..." -ForegroundColor Cyan
& "$root\scripts\iis-setup-local.ps1"

Write-Host "`nYeniden yayın tamam." -ForegroundColor Green
Write-Host "  • Tarayıcıda Ctrl+F5 ile hard refresh yap." -ForegroundColor Yellow
Write-Host "  • Panele yeniden giriş yap (yeni JWT secret): belediye admin@... / kurum admin@el-dicle.kurum.tr vb." -ForegroundColor Yellow
