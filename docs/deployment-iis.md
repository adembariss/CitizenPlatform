# Production Dağıtımı — Ayrı Domainler + IIS

Bu doküman uygulamayı **üç ayrı domainde** IIS üzerinde yayınlamayı anlatır:

| Site | Örnek domain | İçerik |
|---|---|---|
| Vatandaş portalı | `belediyem.gov.tr` | `citizen-web` (statik SPA) |
| Belediye paneli | `panel.belediyem.gov.tr` | `admin-web` (statik SPA) |
| API | `api.belediyem.gov.tr` | `CitizenPlatform.Api` (Kestrel + ANCM) |
| Worker | (site değil) | `CitizenPlatform.Worker` — Windows Service |
| Veritabanı / depolama | (ayrı sunucu/servis) | PostgreSQL+PostGIS, MinIO/S3 |

Neden ayrı: farklı kullanıcı kitleleri, güvenlik izolasyonu, panelin herkese açık olmaması.

## 1. Sunucu ön gereksinimleri (IIS makinesi)

- **IIS** (Web Server rolü).
- **.NET 8 ASP.NET Core Hosting Bundle** — API'yi IIS altında çalıştıran ASP.NET Core Module (ANCM) v2'yi kurar. (Kurulumdan sonra `iisreset`.)
- **URL Rewrite** modülü — SPA client-side routing + /api proxy için.
- **Application Request Routing (ARR)** — /api'yi API domainine reverse-proxy'lemek için. (ARR kurduktan sonra IIS Manager → Application Request Routing Cache → Server Proxy Settings → **Enable proxy** işaretlenmeli.)
- Build makinesinde (aynı olabilir): **.NET 8 SDK** + **Node.js 20+**.
- **PostgreSQL 16 + PostGIS** ve (fotoğraf için) **MinIO/S3** ayrı kurulu/erişilebilir olmalı.

## 2. Paketleme (build makinesinde)

```powershell
cd C:\path\to\CitizenPlatform
npm install
./scripts/publish.ps1
```

Çıktı `publish/` altında:
```
publish/
  api/          -> API (web.config + CitizenPlatform.Api.dll ...)
  worker/       -> Worker (Windows Service)
  citizen-web/  -> vatandaş SPA (index.html + assets + web.config)
  panel-web/    -> panel SPA (index.html + assets + web.config)
```

## 3. Veritabanı (ilk kurulum)

```powershell
# Şema:
dotnet ef database update --project backend/src/CitizenPlatform.Infrastructure/CitizenPlatform.Infrastructure.csproj --startup-project backend/src/CitizenPlatform.Api/CitizenPlatform.Api.csproj
# Index + seed'ler (002..009) — psql ya da Get-Content | psql ile sırayla uygulayın.
# Tüm Türkiye ilçe sınırları scripts/gen_turkey_boundaries.js ile üretilir (bkz. database/main-db/README.md).
```

## 4. API sitesini kur (`api.belediyem.gov.tr`)

1. `publish/api` klasörünü sunucuya kopyalayın (örn. `C:\inetpub\citizenplatform\api`).
2. IIS Manager → yeni **Application Pool**: **No Managed Code** (ANCM Kestrel'i çalıştırır), 32-bit kapalı.
3. Yeni **Site**: fiziksel yol = api klasörü, binding = `https` + `api.belediyem.gov.tr` + sertifika.
4. `web.config` içindeki ortam değişkenlerini **gerçek değerlerle** doldurun (deploy/api/web.config şablonundan geldi):
   - `JWT__SECRET` (en az 32 karakter rastgele — **Production'da default/kısa secret ile uygulama açılmaz**)
   - `Database__ConnectionString`
   - `CORS__ALLOWED_ORIGINS = https://belediyem.gov.tr,https://panel.belediyem.gov.tr`
   - (Opsiyonel) `ObjectStorage__*` (MinIO/S3), `SMS__PROVIDER=Netgsm` + `SMS__NETGSM__*`
5. App Pool kimliğine publish klasörü için okuma + `logs` için yazma izni verin.
6. `https://api.belediyem.gov.tr/health` → `Healthy` dönmeli.

> Not: `ASPNETCORE_ENVIRONMENT=Production` olduğundan Swagger kapalı, geliştirici seed'i çalışmaz, JWT secret zorunludur.

## 5. SPA sitelerini kur (vatandaş + panel)

Her ikisi için (citizen-web → `belediyem.gov.tr`, panel-web → `panel.belediyem.gov.tr`):

1. Klasörü kopyalayın (örn. `C:\inetpub\citizenplatform\citizen-web`).
2. Yeni **Site**: fiziksel yol = SPA klasörü, `https` binding + ilgili domain + sertifika. App Pool **No Managed Code** olabilir (statik içerik).
3. Klasördeki `web.config` (deploy/spa/web.config):
   - **`api-proxy`** kuralındaki `https://api.belediyem.gov.tr` adresini kendi API domaininizle değiştirin. Bu kural `/api/*` isteklerini API'ye proxy'ler (ARR gerekli) → **CORS gerekmez, frontend kodu değişmez**.
   - `spa-fallback` kuralı client-side route'ları `index.html`'e düşürür (panelin react-router'ı için gerekli).

**Alternatif (CORS):** proxy yerine SPA'nın API'yi mutlak URL ile çağırmasını isterseniz, fetch katmanına bir API taban URL'i eklemeniz (küçük kod değişikliği) ve API tarafında `CORS__ALLOWED_ORIGINS`'e SPA domainlerini eklemeniz gerekir. Önerilen yol reverse-proxy'dir.

## 6. Worker'ı Windows Service olarak çalıştır

Worker bir web sitesi değildir; sürekli çalışan arka plan işçisidir (outbox → belediye dış DB senkronu).

```powershell
# publish/worker'ı sunucuya kopyalayın, sonra:
New-Service -Name "CitizenPlatformWorker" `
  -BinaryPathName '"C:\Program Files\dotnet\dotnet.exe" "C:\inetpub\citizenplatform\worker\CitizenPlatform.Worker.dll"' `
  -DisplayName "CitizenPlatform Worker" -StartupType Automatic
# Ortam değişkenleri (DB bağlantısı vb.) makine düzeyinde tanımlanmalı; sonra:
Start-Service CitizenPlatformWorker
```
(Alternatif: NSSM ile servisleştirme. Worker'a `ASPNETCORE_ENVIRONMENT=Production` ve `Database__ConnectionString` verin.)

## 7. HTTPS ve güvenlik

- Üç domain için de geçerli TLS sertifikaları bağlayın (Let's Encrypt/kurumsal). **HTTPS ayrıca tarayıcı konum servisini de etkinleştirir** (nöbetçi eczane "konumumu kullan" ve şikayet konumu prod'da HTTPS ister).
- HTTP→HTTPS yönlendirmesi (URL Rewrite) ekleyin.
- Gerçek `JWT__SECRET`, DB parolası, MinIO/Netgsm anahtarları yalnızca sunucuda; repoya asla yazılmaz.
- `CORS__ALLOWED_ORIGINS` yalnızca gerçek SPA domainleriyle daraltılır.

## 8. Özet akış

```
Tarayıcı → belediyem.gov.tr (SPA)  ──/api──(ARR proxy)──►  api.belediyem.gov.tr (Kestrel/ANCM)
Tarayıcı → panel.belediyem.gov.tr (SPA) ─/api─(ARR proxy)─►  api.belediyem.gov.tr
                                                              │
                                                              ├─► PostgreSQL + PostGIS
                                                              ├─► MinIO/S3 (fotoğraf)
                                                              └─► integration_outbox ──► Worker (Windows Service) ──► belediye dış DB
```
