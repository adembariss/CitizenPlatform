# CitizenPlatform

CitizenPlatform, belediyeler için vatandaş şikayet ve bildirim süreçlerini tek çatı altında toplamayı hedefleyen bir monorepo projesidir. İlk fazda vatandaş web uygulaması, vatandaş mobil uygulaması, belediye yönetim paneli, .NET backend ve lokal geliştirme altyapısı için temel iskelet hazırlandı.

## Teknoloji Yığını

- Backend: .NET 8 LTS Web API
- Mimari: Clean Architecture + Modular Monolith
- Veritabanı: PostgreSQL + PostGIS
- ORM hedefi: EF Core
- Background worker: .NET Worker Service
- Admin web: React + TypeScript + Vite
- Citizen web: React + TypeScript + Vite
- Citizen mobile: React Native + Expo
- Lokal servisler: Docker Compose, PostGIS, MinIO
- Object storage: local/MinIO uyumlu soyutlama
- Harita hedefi: web tarafında MapLibre GL veya Leaflet adaptörü, mobilde native location API soyutlaması

## Kurulum

Ön koşullar:

- .NET 8 SDK
- Docker Desktop veya Docker Compose uyumlu runtime
- Node.js ve npm

Lokal servisleri başlatmak için:

```powershell
Copy-Item .env.example .env
docker compose up -d
```

Backend build:

```powershell
dotnet build backend/CitizenPlatform.sln
```

Frontend bağımlılıkları:

```powershell
npm install
npm run dev:admin
npm run dev:citizen-web
npm run dev:citizen-mobile
```

## Klasör Yapısı

```text
backend/
  src/
    CitizenPlatform.Api/
    CitizenPlatform.Application/
    CitizenPlatform.Domain/
    CitizenPlatform.Infrastructure/
    CitizenPlatform.Integrations/
    CitizenPlatform.Worker/
  tests/
    CitizenPlatform.UnitTests/
    CitizenPlatform.IntegrationTests/
  CitizenPlatform.sln
apps/
  admin-web/
  citizen-web/
  citizen-mobile/
database/
  main-db/
  municipality-sample-db/
  seed/
docs/
docker/
docker-compose.yml
.env.example
```

## Backend Notları

- Nullable reference types aktiftir.
- Treat warnings as errors şu an kapalıdır.
- `GlobalExceptionMiddleware` tüm beklenmeyen hataları standart `ApiResponse` formatında döndürür.
- Domain katmanı dış framework ve altyapı bağımlılığı almaz.
- Infrastructure ve Integrations katmanları şimdilik adaptör sınırlarını ve konfigürasyon modellerini içerir.
- Domain ve veritabanı model özeti için `docs/database-design.md` dosyasına bakın.
- Auth (JWT login, roller, policy'ler, development demo kullanıcılar) için `docs/auth.md` dosyasına bakın.
- Belediye yönetim paneli admin API'leri için `docs/admin-api.md` dosyasına bakın.
- Şikayet yaşam döngüsü ve outbox senkronizasyonu için `docs/complaint-flow.md` dosyasına bakın.
- Projenin güncel durumu, build/test sonuçları ve öncelikli eksikler için `docs/project-status.md` dosyasına bakın.

## Geliştirme Notları

- Secret değerleri repoya eklemeyin.
- Gerçek ortam değerlerini `.env` veya deployment secret yönetimi üzerinden verin.
- `JWT__SECRET` development placeholder'ı prod'da mutlaka değiştirilmeli (bkz. `docs/auth.md`).
- EF Core migrationları ve PostGIS modellemeleri `Infrastructure/Persistence/Migrations` altında genişlemeye devam ediyor.
