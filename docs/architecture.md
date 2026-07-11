# Architecture

## Dual-write değil, Outbox ile güvenli senkronizasyon

CitizenPlatform API request sirasinda belediye veritabanina dogrudan yazmaz. Sikayet once ana veritabanina kaydedilir ve ayni transaction icinde `integration_outbox` tablosuna `ComplaintCreated` mesaji eklenir. Boylece ana complaint kaydi ile entegrasyon mesaji birbirinden kopmaz.

Worker tarafindaki `OutboxProcessorService`, due durumdaki `Pending` outbox mesajlarini arka planda okur. Mesaj `Processing` durumuna alinir, `integration_attempts` icinde yeni deneme kaydi acilir ve ilgili belediyenin aktif `MunicipalityDatabaseConnection` kaydi cozumlenir. Demo implementasyonda `PostgreSqlMunicipalityComplaintWriter`, sample belediye PostgreSQL veritabanina yazar.

Belediye veritabanina yazim idempotent tasarlanir. Sample DB tarafinda `municipal_complaints.main_complaint_id` unique oldugu icin ayni `ComplaintCreated` mesaji tekrar islenirse duplicate complaint uretilmez. Status log insert'i de ayni ana complaint ve status icin tekrar kayit olusturmayacak sekilde korunur.

Hata durumunda Worker API cevabini etkilemez; vatandasin sikayeti ana veritabaninda kalir. Attempt kaydi hata detayi ile kapatilir, outbox `Pending` durumuna geri alinir ve exponential backoff ile `next_retry_at` atanir. `MaxRetryCount` asilinca mesaj `Failed` durumuna gecer ve operasyonel inceleme icin `failure_reason` saklanir.

Bu yaklasim dual-write riskini ortadan kaldirir:

- Belediye DB gecici olarak erisilemezse vatandas kaydi kaybolmaz.
- Retry ve backoff merkezi olarak Worker tarafinda yonetilir.
- Idempotency belediye DB seviyesinde unique constraint ile garanti edilir.
- API hizli cevap verir ve harici sistem gecikmesine baglanmaz.

CitizenPlatform, Clean Architecture prensiplerini Modular Monolith yaklaşımıyla birleştirir.

- Domain katmanı iş kurallarını ve domain eventleri içerir.
- Application katmanı use case, DTO, validation ve port arayüzlerini tutar.
- Infrastructure katmanı persistence, identity, storage, geospatial ve notification adaptörlerini içerir.
- Integrations katmanı belediye veritabanı ve outbox entegrasyon sınırlarını barındırır.
- Api ve Worker uç projeleri Application katmanını dış dünyaya açar.

## Konumdan Belediye Tespiti

Vatandaş şikayet oluştururken ana konum kaynağı kullanıcının seçtiği veya cihazdan gelen `lat/lng` değeridir. API tarafında `GET /api/public/municipalities/resolve?lat=...&lng=...` endpoint'i bu koordinatı `IGeoMunicipalityResolver` abstraction'ına iletir.

Infrastructure katmanındaki `GeoMunicipalityResolver` önce koordinatı `GeoCoordinate` value object ile doğrular. Ardından SRID 4326 bir `Point` geometry oluşturur ve PostGIS destekli boundary lookup ile `municipality_boundaries` tablosunda `ST_Contains(boundary_geometry, point)` sorgusu çalıştırır.

Birden fazla aktif belediye sınırı noktayı içerirse bugün deterministik ilk aktif kayıt seçilir. Gelecekte boundary priority veya en küçük alan gibi daha spesifik seçim kuralları eklenebilir.

Hiç sınır bulunamazsa servis exception fırlatmaz; `MunicipalityResolveResult.IsSuccess = false` ve açıklayıcı `FailureReason` döner.

## Fotoğraf EXIF Konumu

Fotoğraf EXIF konumu yardımcı veridir, belediye tespitinde birincil kaynak değildir. Bunun nedeni EXIF bilgisinin her cihazda bulunmaması, kullanıcı tarafından temizlenebilmesi, fotoğrafın farklı zamanda veya farklı yerde çekilmiş olabilmesi ve şikayet konumunu her zaman temsil etmemesidir.

Bu nedenle belediye tespiti kullanıcı seçimi veya cihazdan gelen şikayet konumu üzerinden yapılır. EXIF konumu doğrulama, tutarlılık kontrolü, operatör incelemesi veya sahtecilik sinyali gibi ikincil akışlarda kullanılabilir.

## PostGIS Kullanım Nedeni

Belediye sınırları polygon/multipolygon verisidir ve noktanın hangi sınır içinde kaldığını doğru hesaplamak spatial index ve geometry fonksiyonları gerektirir. PostGIS bu iş için olgun, açık kaynaklı ve PostgreSQL ile doğal çalışan bir spatial altyapı sağlar.

`municipality_boundaries.boundary_geometry` kolonu `geometry(MultiPolygon,4326)` olarak tutulur ve GIST index ile hızlandırılır. Şikayet konumu ise `geometry(Point,4326)` olarak saklanır. Böylece hem belediye tespiti hem de ileride harita, yakınlık ve bölgesel raporlama sorguları veritabanı seviyesinde verimli çalışır.
