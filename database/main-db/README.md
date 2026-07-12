# Main DB

CitizenPlatform ana PostgreSQL + PostGIS veritabanı için bootstrap scriptleri burada tutulur.

- `001_enable_postgis.sql`: PostGIS extension'ını açar.
- `002_indexes.sql`: Spatial, tracking code ve filtered indexleri idempotent şekilde oluşturur.
- `003_seed_demo_municipality.sql`: Demo Belediyesi için örnek kategori, birim ve demo boundary verisi ekler.

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
