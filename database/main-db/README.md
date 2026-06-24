# Main DB

CitizenPlatform ana PostgreSQL + PostGIS veritabanı için manuel bootstrap scriptleri burada tutulur.

- `001_enable_postgis.sql`: PostGIS extension'ını açar.
- `002_indexes.sql`: Spatial, tracking code ve filtered indexleri idempotent şekilde oluşturur.
- `003_seed_demo_municipality.sql`: Demo Belediyesi için örnek kategori, birim ve demo boundary verisi ekler.

EF Core migrationları Infrastructure projesindeki `Persistence/Migrations` klasöründedir. Seed scripti migrationlar uygulandıktan sonra çalıştırılmalıdır.
