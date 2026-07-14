# Main DB

CitizenPlatform ana PostgreSQL + PostGIS veritabanı için bootstrap scriptleri burada tutulur.

- `001_enable_postgis.sql`: PostGIS extension'ını açar.
- `002_indexes.sql`: Spatial, tracking code ve filtered indexleri idempotent şekilde oluşturur.
- `003_seed_demo_municipality.sql`: Demo Belediyesi için örnek kategori, birim ve demo boundary verisi ekler.
- `004_seed_turkiye_municipalities.sql`: 81 il + örnek ilçeler (yaklaşık kutu sınır).
- `005_seed_istanbul_districts.sql`: İstanbul 39 ilçe (sınırsız; adres seçici).
- `006_set_province_center.sql`: mevcut belediyelere il + merkez koordinatı doldurur.
- `007_seed_istanbul_real_boundaries.sql`: İstanbul ilçelerine geoBoundaries ADM2 gerçek poligonları.
- `008_seed_bulk_district_catalog.sql`: toplu içe aktarılan `TR_*` ilçelere kategori/birim/kural.

### Tüm Türkiye ilçeleri (gerçek sınır) — betikle üretilir

12 MB'lık ham SQL repoda tutulmaz; `scripts/gen_turkey_boundaries.js` ile geoBoundaries'ten üretilir:

```powershell
# geoBoundaries gbOpen TUR ADM1 (il) ve ADM2 (ilçe) GeoJSON'larını indir, sonra:
node scripts/gen_turkey_boundaries.js tur_adm1.geojson tur_adm2.geojson turkey_boundaries.sql
Get-Content turkey_boundaries.sql | docker exec -i citizenplatform-main-db psql -U citizen_platform -d citizen_platform
# ardından kategori/birim/kural:
Get-Content database/main-db/008_seed_bulk_district_catalog.sql | docker exec -i citizenplatform-main-db psql -U citizen_platform -d citizen_platform
```

## Sıralama önemli

`002` ve `003` EF Core migrationlarının oluşturduğu tablolara (`municipality_boundaries`, `complaints`, ...) bağımlıdır ve **migrationlar uygulanmadan çalıştırılamaz**. Bu yüzden bunlar `docker-entrypoint-initdb.d` altında otomatik çalıştırılmaz — sadece `database/docker-init/001_enable_postgis.sql` (bu klasördeki `001` ile aynı içerik, sadece extension açar) container ilk açılışında otomatik çalışır.

Doğru sıra:

```powershell
docker compose up -d
dotnet ef database update --project backend/src/CitizenPlatform.Infrastructure/CitizenPlatform.Infrastructure.csproj --startup-project backend/src/CitizenPlatform.Api/CitizenPlatform.Api.csproj
psql "Host=localhost;Port=5432;Database=citizen_platform;Username=citizen_platform;Password=change-me-local" -f database/main-db/002_indexes.sql
psql "Host=localhost;Port=5432;Database=citizen_platform;Username=citizen_platform;Password=change-me-local" -f database/main-db/003_seed_demo_municipality.sql
```

`001_enable_postgis.sql` bu klasörde referans/dokümantasyon amaçlı duruyor; gerçek otomatik çalıştırılan kopyası `database/docker-init/001_enable_postgis.sql`.
