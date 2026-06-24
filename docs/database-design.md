# Database and Domain Design

CitizenPlatform çoklu belediye destekli bir şikayet ve bildirim platformudur. Vatandaş web veya mobil uygulamadan konum içeren bir şikayet oluşturur. Application katmanı konumdan belediyeyi çözer, ana veritabanına yazar ve ilgili belediye veritabanına aktarım için outbox mesajı üretir.

## Ana Aggregate ve Entity'ler

- `Municipality`: Belediye tenant kaydıdır. Kod, ad ve aktiflik bilgisini tutar.
- `MunicipalityBoundary`: Belediyenin servis alanı geometrisini tutar. Konumdan belediye bulma akışında kullanılır.
- `MunicipalityDatabaseConnection`: Belediyeye ait harici veritabanı sağlayıcısı ve şifrelenmiş bağlantı bilgisini temsil eder.
- `User`, `Role`, `UserRole`: Sistem admini, belediye admini, belediye çalışanı ve vatandaş kullanıcıları için yetki modelidir.
- `Citizen`: Vatandaş profilini temsil eder. Kullanıcı hesabı opsiyoneldir.
- `Department`: Belediye içindeki müdürlük veya ekip birimidir.
- `ComplaintCategory`: Şikayet kategorisidir. Global veya belediye özelinde tanımlanabilir.
- `CategoryDepartmentRule`: Kategoriye göre varsayılan departman ve öncelik yönlendirme kuralıdır.
- `Complaint`: Şikayet aggregate root'udur. Belediye, kategori, tracking code, konum, EXIF konum, status, priority, source ve belediye senkronizasyon alanlarını tutar.
- `ComplaintAttachment`, `ComplaintStatusHistory`, `ComplaintComment`, `ComplaintAssignment`: Şikayetin dosya, durum geçmişi, yorum ve atama alt kayıtlarıdır.
- `IntegrationOutboxMessage`, `IntegrationAttempt`: Ana veritabanından belediye veritabanına aktarım için outbox ve deneme kayıtlarıdır.
- `AuditLog`: Kritik entity değişikliklerinin denetlenebilir kaydıdır.
- `RefreshToken`: Kimlik doğrulama oturum yenileme token kaydıdır.
- `Notification`: Email, SMS, push ve uygulama içi bildirim kayıtlarıdır.

## Complaint Tasarımı

`Complaint` oluşturulurken `municipalityId`, `categoryId`, `trackingCode`, başlık, açıklama, kaynak ve koordinat zorunludur. `trackingCode` Domain içinde üretilmez; Application katmanındaki tracking code servisi tarafından üretilip aggregate'e verilir. Veritabanında `trackingCode` için unique index tasarlanmalıdır.

Konum iki biçimde tutulur:

- `Location`: `GeoCoordinate` value object olarak latitude/longitude.
- `LocationGeometry`: PostGIS tarafında geometry kolonuna eşlenecek WKT karşılığı.

Fotoğraftan gelen EXIF konumu opsiyoneldir:

- `PhotoExifLocation`
- `PhotoExifGeometry`

Belediye entegrasyon senkronizasyonu için:

- `ExternalMunicipalityComplaintId`
- `ExternalMunicipalityStatus`
- `LastSyncAttemptAt`
- `SyncedAt`

## Domain Davranışları

`Complaint` aggregate'i invalid state oluşmasını engelleyen factory ve domain metotlarıyla yönetilir:

- `Create(...)`: Yeni şikayeti `New` status ile oluşturur.
- `ChangeStatus(...)`: Yeni status atar ve `ComplaintStatusHistory` üretir.
- `AssignToDepartment(...)`: Departman ve opsiyonel kullanıcı ataması yapar, assignment kaydı oluşturur.
- `AddComment(...)`: İç veya vatandaş görünür yorumu ekler.
- `AddAttachment(...)`: Storage sağlayıcısı ve object key içeren attachment kaydı ekler.

## Ortak Kurallar

- Tüm entity'lerde `Guid Id` kullanılır.
- `AuditableEntity` base class `CreatedAt`, `UpdatedAt`, `DeletedAt` ve `IsDeleted` alanlarını yönetir.
- Soft delete `MarkDeleted()` ile desteklenir.
- Domain EF Core'a bağımlı değildir; entity'lerde EF materialization için private/protected parameterless constructor bulunur.
- `GeoCoordinate` latitude için `-90..90`, longitude için `-180..180` aralığını doğrular.

## EF Core Persistence

Infrastructure katmanında ana context `CitizenPlatformDbContext` olarak tanımlıdır. PostgreSQL provider olarak Npgsql, spatial provider olarak NetTopologySuite kullanılır. Schema adı `public` olarak sabitlenmiştir.

Persistence mapping'leri Fluent API ile `CitizenPlatform.Infrastructure/Persistence/Configurations` altında tutulur. Domain katmanı NetTopologySuite'a bağımlı değildir; domain'de WKT string olarak tutulan geometry karşılıkları EF mapping içinde PostGIS geometry kolonlarına dönüştürülür.

- `municipality_boundaries.boundary_geometry`: `geometry(MultiPolygon,4326)`
- `complaints.location_geometry`: `geometry(Point,4326)`
- `complaints.photo_exif_geometry`: `geometry(Point,4326)`, nullable
- `complaint_attachments.photo_exif_geometry`: `geometry(Point,4326)`, nullable

Spatial sorgular için GIST indexleri oluşturulur:

- `ix_municipality_boundaries_boundary_geometry_gist`
- `ix_complaints_location_geometry_gist`

`complaints.tracking_code` için `ux_complaints_tracking_code` unique index'i bulunur. Nullable unique alanlarda filtered index kullanılır; örnek olarak `citizens.email` için `email IS NOT NULL AND is_deleted = false` filtresi vardır.

`AuditableEntity` türevi tüm entity'lerde global query filter `is_deleted = false` koşulunu uygular. `SaveChanges` ve `SaveChangesAsync` sırasında:

- Yeni entity'lerde `created_at` otomatik set edilir.
- Güncellenen entity'lerde `updated_at` otomatik set edilir.
- Delete operasyonları fiziksel silmeye gitmeden soft delete'e çevrilir.

Audit kayıtları şimdilik manuel `audit_logs` entity'si üzerinden yazılır.

## Migration ve SQL Scriptleri

İlk migration:

```powershell
dotnet ef migrations add InitialCreate --project backend/src/CitizenPlatform.Infrastructure/CitizenPlatform.Infrastructure.csproj --startup-project backend/src/CitizenPlatform.Api/CitizenPlatform.Api.csproj --output-dir Persistence/Migrations --context CitizenPlatformDbContext
```

Yerel EF tool manifest repoda bulunur. Yeni makinede önce şu komut çalıştırılabilir:

```powershell
dotnet tool restore
```

`database/main-db` altında manuel/veritabanı bootstrap scriptleri bulunur:

- `001_enable_postgis.sql`: PostGIS extension'ını açar.
- `002_indexes.sql`: Temel GIST, tracking code ve filtered indexleri idempotent olarak oluşturur.
- `003_seed_demo_municipality.sql`: Demo Belediyesi, demo boundary, kategori, birim ve kategori-birim kurallarını ekler.

Demo boundary İstanbul civarında basit bir örnek polygon'dur; gerçek belediye sınırı olarak kullanılmamalıdır.

## Enumlar

- `UserType`: `SystemAdmin`, `MunicipalityAdmin`, `MunicipalityEmployee`, `Citizen`
- `ComplaintStatus`: `New`, `UnderReview`, `Assigned`, `InProgress`, `WaitingForCitizen`, `Resolved`, `Closed`, `Rejected`, `Duplicate`, `OutOfScope`
- `ComplaintPriority`: `Low`, `Normal`, `High`, `Critical`
- `ComplaintSource`: `CitizenWeb`, `CitizenMobile`, `AdminPanel`, `Integration`
- `OutboxStatus`: `Pending`, `Processing`, `Completed`, `Failed`, `Cancelled`
- `NotificationChannel`: `Email`, `Sms`, `Push`, `InApp`
- `NotificationStatus`: `Pending`, `Sent`, `Failed`
- `StorageProvider`: `Local`, `Minio`, `S3`, `AzureBlob`
- `MunicipalityDbProvider`: `PostgreSql`, `SqlServer`, `Oracle`, `Unknown`
