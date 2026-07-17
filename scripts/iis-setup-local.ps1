#Requires -RunAsAdministrator
<#
  CitizenPlatform'u YEREL IIS'te üç ayrı hostname ile ayağa kaldırır:
    http://belediyem.local        -> vatandaş SPA
    http://panel.belediyem.local  -> belediye panel SPA
    http://api.belediyem.local    -> API (Kestrel/ANCM)

  ÖN KOŞULLAR (bu script'ten ÖNCE, yönetici olarak kur):
    1) IIS + "IIS Management Scripts and Tools":
         Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-ManagementScriptingTools -All
    2) ASP.NET Core 8 Hosting Bundle:  https://dotnet.microsoft.com/download/dotnet/8.0  (ASP.NET Core Runtime -> Hosting Bundle)
    3) URL Rewrite:                    https://www.iis.net/downloads/microsoft/url-rewrite
    4) ARR (App Request Routing):      https://www.iis.net/downloads/microsoft/application-request-routing
    Kurulumlardan sonra:  iisreset

  ÖNCE:  ./scripts/publish.ps1   (publish/ klasörünü üretir)
  SONRA (YÖNETİCİ PowerShell):  ./scripts/iis-setup-local.ps1
#>
param(
    [string]$Root = "C:\Users\ADEM\source\repos\CitizenPlatform",
    [string]$ApiHost = "api.belediyem.local",
    [string]$CitizenHost = "belediyem.local",
    [string]$PanelHost = "panel.belediyem.local",
    [string]$JwtSecret = ""
)

$ErrorActionPreference = "Stop"
function Info($m) { Write-Host $m -ForegroundColor Cyan }
function Ok($m)   { Write-Host $m -ForegroundColor Green }
function Fail($m) { Write-Host $m -ForegroundColor Red; exit 1 }

# --- Ön koşul kontrolü ---
# ANCM v2 modülü: yeni Hosting Bundle "Program Files\IIS\...", eskiler "System32\inetsrv" altına koyar.
$ancm = (Test-Path "$env:windir\System32\inetsrv\aspnetcorev2.dll") `
    -or (Test-Path "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll") `
    -or (Test-Path "${env:ProgramFiles(x86)}\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll")
if (-not $ancm) { Fail "EKSİK: ASP.NET Core Hosting Bundle. https://dotnet.microsoft.com/download/dotnet/8.0" }
if (-not (Test-Path "$env:windir\System32\inetsrv\rewrite.dll"))      { Fail "EKSİK: URL Rewrite. https://www.iis.net/downloads/microsoft/url-rewrite" }
$arr = (Test-Path "$env:ProgramFiles\IIS\Application Request Routing\requestRouter.dll") -or (Test-Path "${env:ProgramFiles(x86)}\IIS\Application Request Routing\requestRouter.dll")
if (-not $arr) { Fail "EKSİK: ARR. https://www.iis.net/downloads/microsoft/application-request-routing" }
try { Import-Module WebAdministration -ErrorAction Stop } catch { Fail "EKSİK: IIS Management Scripts and Tools. Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementScriptingTools -All" }

$pub = Join-Path $Root "publish"
if (-not (Test-Path (Join-Path $pub "api\CitizenPlatform.Api.dll"))) { Fail "publish/ yok. Önce ./scripts/publish.ps1 çalıştır." }

if ([string]::IsNullOrWhiteSpace($JwtSecret)) {
    $JwtSecret = -join ((48..57)+(65..90)+(97..122) | Get-Random -Count 48 | ForEach-Object { [char]$_ })
}

# --- ARR reverse-proxy'yi etkinleştir ---
Info "ARR proxy etkinleştiriliyor..."
Set-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST" -Filter "system.webServer/proxy" -Name "enabled" -Value "True"

# --- hosts kayıtları ---
$hostsFile = "$env:windir\System32\drivers\etc\hosts"
$hostsContent = Get-Content $hostsFile -Raw
foreach ($h in @($ApiHost, $CitizenHost, $PanelHost)) {
    # Tam host eşleşmesi (satır sonu) — aksi halde "belediyem.local" zaten "api.belediyem.local"
    # satırının içinde geçtiği için yanlışlıkla atlanır (substring hatası).
    $pattern = "(?m)^\s*\d{1,3}(\.\d{1,3}){3}\s+" + [regex]::Escape($h) + "\s*$"
    if (-not (Select-String -Path $hostsFile -Pattern $pattern -Quiet)) {
        Add-Content $hostsFile "127.0.0.1`t$h"
        Ok "hosts eklendi: $h"
    }
}

# --- API web.config (yerel değerlerle) ---
$apiConn = "Host=localhost;Port=5432;Database=citizen_platform;Username=citizen_platform;Password=change-me-local"
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\CitizenPlatform.Api.dll" stdoutLogEnabled="false" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="JWT__SECRET" value="$JwtSecret" />
          <environmentVariable name="Database__ConnectionString" value="$apiConn" />
          <environmentVariable name="CORS__ALLOWED_ORIGINS" value="http://$CitizenHost,http://$PanelHost" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@ | Set-Content (Join-Path $pub "api\web.config") -Encoding UTF8

# --- SPA web.config'leri (yerel proxy hedefi) ---
$spaConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="api-proxy" stopProcessing="true">
          <match url="^api/(.*)" />
          <action type="Rewrite" url="http://$ApiHost/api/{R:1}" />
        </rule>
        <rule name="spa-fallback" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
"@
$spaConfig | Set-Content (Join-Path $pub "citizen-web\web.config") -Encoding UTF8
$spaConfig | Set-Content (Join-Path $pub "panel-web\web.config") -Encoding UTF8

# --- App pool + site oluştur ---
function New-CpSite($name, $hostname, $physical) {
    $pool = "CP_$name"
    if (Get-Item "IIS:\AppPools\$pool" -ErrorAction SilentlyContinue) { Remove-WebAppPool $pool }
    New-WebAppPool $pool | Out-Null
    Set-ItemProperty "IIS:\AppPools\$pool" -Name managedRuntimeVersion -Value ""   # No Managed Code
    Set-ItemProperty "IIS:\AppPools\$pool" -Name startMode -Value "AlwaysRunning"

    if (Get-Website -Name $name -ErrorAction SilentlyContinue) { Remove-Website -Name $name }
    New-Website -Name $name -PhysicalPath $physical -ApplicationPool $pool -HostHeader $hostname -Port 80 | Out-Null

    # Anonim istekler uygulama havuzu kimliğini kullansın (publish, kullanıcı profili
    # altında olduğundan IUSR'ın erişimi yok; boş userName = app pool identity).
    Set-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST" -Location $name -Filter "system.webServer/security/authentication/anonymousAuthentication" -Name userName -Value "" -ErrorAction SilentlyContinue

    # Hem app pool kimliği (IIS_IUSRS) hem de IUSR için okuma izni ver (garanti).
    icacls $physical /grant "IIS_IUSRS:(OI)(CI)RX" "IUSR:(OI)(CI)RX" /T /Q | Out-Null
    Ok "Site: http://$hostname  ($physical)"
}

Info "Siteler oluşturuluyor..."
New-CpSite -name "CP-Api"     -hostname $ApiHost     -physical (Join-Path $pub "api")
New-CpSite -name "CP-Citizen" -hostname $CitizenHost -physical (Join-Path $pub "citizen-web")
New-CpSite -name "CP-Panel"   -hostname $PanelHost   -physical (Join-Path $pub "panel-web")

# API'nin yerel disk depolamasına yazabilmesi için:
icacls (Join-Path $pub "api") /grant "IIS_IUSRS:(OI)(CI)M" /T /Q | Out-Null

Ok "`nHazir!"
Write-Host "  Vatandas : http://$CitizenHost" -ForegroundColor Green
Write-Host "  Panel    : http://$PanelHost   (admin@demo.local / Demo123!)" -ForegroundColor Green
Write-Host "  API      : http://$ApiHost/health" -ForegroundColor Green
Write-Host "`nNot: HTTP oldugu icin tarayici konum servisi (Konumumu kullan) yalnizca HTTPS/localhost'ta calisir;" -ForegroundColor Yellow
Write-Host "     haritadan/adresten secim ve diger her sey calisir." -ForegroundColor Yellow
