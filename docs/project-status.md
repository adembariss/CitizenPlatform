# CitizenPlatform Project Status

_Son güncelleme: 2026-07-12_
_Doğrulanan commit: `52f9f41` (main, "Add outbox based municipality sync worker")_

## 1. Genel Durum

Proje **erken iskelet (early scaffold) aşamasında**. Backend tarafı beklenenden daha olgun: Clean Architecture katmanları gerçek anlamda ayrılmış, tek bir uçtan uca akış (vatandaş şikayeti oluşturma → outbox → belediye DB'sine senkronizasyon) gerçekten çalışır durumda ve test edilmiş. Buna karşılık kimlik doğrulama, belediye admin paneli API'leri, takip sorgulama endpoint'i ve üç frontend uygulamasının hiçbiri henüz gerçek işlevsellik içermiyor — sadece statik/mock arayüz iskeletleri var.

Kısaca: **"Vatandaş şikayet gönderir" akışının ilk %60'ı (backend tarafı) sağlam, geri kalan her şey (auth, admin, tracking, frontend entegrasyonu) henüz yok.**

## 2. Build/Test Durumu

Bu bilgisayarda ortam eksikleri tespit edildi ve kullanıcı onayıyla tamamlandı:

- `global.json` .NET SDK `8.0.404` istiyordu, makinede sadece `8.0.119` ve `9.0.304` kuruluydu (feature-band uyuşmazlığı yüzünden roll-forward çalışmadı). **winget ile `Microsoft.DotNet.SDK.8` (8.0.422) kuruldu.**
- Docker Desktop bu makinede kurulu değil. Kullanıcı onayı ile kurulum **atlandı**; `docker-compose.yml` bu yüzden `docker compose config` ile değil, manuel dosya incelemesiyle doğrulandı (bkz. bölüm 6 ve 11).

| Komut | Sonuç |
|---|---|
| `dotnet restore backend/CitizenPlatform.sln` | ✅ Başarılı |
| `dotnet build backend/CitizenPlatform.sln` | ✅ Başarılı — 0 uyarı, 0 hata |
| `dotnet test backend/CitizenPlatform.sln` | ✅ Başarılı — 24/24 test geçti (23 unit + 1 integration) |
| `npm install` (repo kökü, workspaces) | ✅ Başarılı — 1141 paket (28 audit uyarısı, hepsi transitive dev-dependency, kritik değil) |
| `npm run build:web` | ✅ Başarılı — hem `admin-web` hem `citizen-web` `tsc -b && vite build` ile derlendi |
| `docker compose config` | ⚠️ **Çalıştırılamadı** — Docker bu makinede kurulu değil, kullanıcı kurulumu istemedi. `docker-compose.yml` manuel incelendi (syntax olarak geçerli görünüyor). |
| `npm --workspace @citizen-platform/citizen-mobile run typecheck` | ⚠️ **Script yok** — `apps/citizen-mobile/package.json` içinde `typecheck` script'i tanımlı değil (sadece `start`, `android`, `ios`, `web` var). Not edildi, işi durdurmadı. |

Build'i bozan kod hatası **bulunamadı** — bu yüzden bu turda kod değişikliği yapılmadı.

## 3. Çalışan Kısımlar

Gerçekten uçtan uca çalışan (test edilmiş) akış:

- **Konumdan belediye tespiti**: `GET /api/public/municipalities/resolve?lat=&lng=` → `GeoMunicipalityResolver` → PostGIS `ST_Contains` sorgusu (`GeoMunicipalityResolverTests` ile test edilmiş).
- **Şikayet oluşturma (JSON + multipart)**: `POST /api/public/complaints` → validation (FluentValidation) → belediye tespiti → kategori/departman kural çözümü → `Complaint` aggregate oluşturma → dosya yükleme → **tek transaction içinde** complaint + outbox mesajı yazma (`CreateComplaintCommandHandlerTests` ile test edilmiş, dual-write yapılmıyor — bkz. bölüm 7).
- **Ek fotoğraf yükleme**: `POST /api/public/complaints/{trackingCode}/attachments`.
- **Dosya güvenliği**: content-type + dosya imzası kontrolü, rastgele dosya adı, SHA256 hash, `wwwroot` dışında saklama (`LocalFileStorageServiceTests` ile test edilmiş).
- **Outbox işleme**: `OutboxProcessingService` + Worker'daki `OutboxProcessorService` — retry, exponential backoff, `MaxRetryCount` sonrası `Failed` durumu (`OutboxProcessingServiceTests` ile test edilmiş).
- **Belediye DB'sine idempotent yazım**: `PostgreSqlMunicipalityComplaintWriter` — `ON CONFLICT (main_complaint_id) DO NOTHING` ile aynı mesaj tekrar işlense bile duplicate oluşmuyor.
- **Domain modeli**: `Complaint` aggregate, status geçmişi, atama, yorum, soft delete, audit alanları (`ComplaintTests`, `GeoCoordinateTests` ile test edilmiş).
- **Sağlık kontrolü**: `GET /api/health` ve `/health` (DB health check ile).

## 4. Dokümante Edilmiş Ama Kodda Eksik Kısımlar

`docs/api-contract.md` sadece 3 endpoint'i dokümante ediyor (Create Complaint JSON/multipart, Add Attachments, Resolve Municipality) ve bunların **hepsi kodda mevcut** — bu üçü için doküman/kod arasında tutarsızlık yok.

Ancak kullanıcı talimatındaki kontrol listesindeki şu üç endpoint **ne dokümanda ne kodda** bulunuyor (yani bunlar henüz roadmap/vizyon seviyesinde, spesifikasyonu bile yazılmamış):

| Endpoint | Kodda var mı? | `docs/api-contract.md`'de var mı? |
|---|---|---|
| `GET /api/public/municipalities/resolve?lat=&lng=` | ✅ Var | ✅ Var |
| `POST /api/public/complaints` (JSON) | ✅ Var | ✅ Var |
| `POST /api/public/complaints` (multipart/form-data) | ✅ Var | ✅ Var |
| `POST /api/public/complaints/{trackingCode}/attachments` | ✅ Var | ✅ Var |
| `GET /api/public/complaints/track/{trackingCode}` | ❌ **Yok** | ❌ Yok |
| `POST /api/auth/login` | ❌ **Yok** | ❌ Yok |
| `GET /api/admin/complaints` | ❌ **Yok** | ❌ Yok |

`Controllers/` klasöründe toplam 3 controller var: `HealthController`, `PublicComplaintsController`, `PublicMunicipalitiesController`. Auth veya Admin controller'ı hiç yok.

Ayrıca: `CitizenPlatform.Application/Features/Complaints/SubmitComplaintCommand.cs` tanımlı ama hiçbir handler veya controller tarafından kullanılmıyor — muhtemelen `CreateComplaintCommand`'ın öncül/yetim (orphan) taslağı. Fonksiyonel bir zarar vermiyor, silinmesi düşünülebilir.

## 5. Backend Durumu

- **Api**: Sadece public complaint/municipality/health endpoint'leri. Auth, admin, CORS policy, rate limiting tanımlı değil. `GlobalExceptionMiddleware` standart `ApiResponse` formatında hata dönüyor, dev ortamında exception mesajını gösteriyor (prod'da genel mesaj). `TreatWarningsAsErrors=false` (README ile tutarlı).
- **Application**: Use case'ler (Create/AddAttachments), DTO'lar, FluentValidation validator'ları, outbox işleme servisi, port arayüzleri (repository/storage/geospatial abstraction'ları) düzgün ayrılmış.
- **Domain**: Framework bağımsız. `Complaint` aggregate factory metotlarıyla (`Create`, `ChangeStatus`, `AssignToDepartment`, `AddComment`, `AddAttachment`) korunuyor. `AuditableEntity` soft-delete + audit alanlarını yönetiyor.
- **Infrastructure**: EF Core + Npgsql + NetTopologySuite persistence, PostGIS boundary lookup, local file storage, EXIF okuyucu, tracking code üretici. `CurrentUserService` **hardcoded stub** (`UserId => null`, `IsAuthenticated => false`) — auth entegrasyonu henüz yok.
- **Integrations**: `PostgreSqlMunicipalityComplaintWriter` gerçek, idempotent bir yazıcı (placeholder değil).
- **Worker**: `OutboxProcessorService` (BackgroundService) gerçek polling/retry/backoff mantığına sahip, placeholder değil.

## 6. Database Durumu

- **Migration**: 3 EF Core migration mevcut (`InitialCreate`, `AddComplaintCreationFields`, `AddComplaintAttachmentFileMetadata`), `CitizenPlatformDbContextModelSnapshot.cs` güncel görünüyor.
- **Seed**: `database/main-db/003_seed_demo_municipality.sql` demo belediye/kategori/birim verisi ekliyor. `database/seed/` klasörü **boş** (sadece README) — kullanılmıyor.
- **PostGIS**: `database/main-db/001_enable_postgis.sql` extension'ı açıyor; `municipality_boundaries.boundary_geometry` (MultiPolygon) ve `complaints.location_geometry` (Point) GIST index'li olarak tasarlanmış, dokümanla kod tutarlı.
- **Main DB**: Şema EF migrationlarından geliyor, manuel scriptler (`001`–`003`) migration sonrası çalıştırılmak üzere tasarlanmış.
- **Municipality sample DB**: `database/municipality-sample-db/001_create_complaint_sync_tables.sql` — `municipal_complaints` ve `municipal_complaint_status_logs` tabloları, `main_complaint_id` üzerinde unique index (idempotency'nin temeli).
- **⚠️ Docker Compose init mount hatası (build'i bozmuyor ama fonksiyonel bir eksiklik)**: `docker-compose.yml` içinde `main-db` servisi init script dizini olarak `./database/seed` klasörünü mount ediyor — ama bu klasör boş. Asıl PostGIS/index/seed scriptleri (`001_enable_postgis.sql`, `002_indexes.sql`, `003_seed_demo_municipality.sql`) `./database/main-db` altında ve **hiç mount edilmiyor**, yani `docker compose up` sonrası container ilk açılışta bu scriptleri otomatik çalıştırmayacak. Ayrıca `municipality-sample-db` servisinde **hiç init volume mount'u yok**, `001_create_complaint_sync_tables.sql` da otomatik çalışmayacak. Bu, ilk kurulumda "veritabanı boş/PostGIS kapalı" sürprizine yol açar. Bu bir docker-compose/dosya-yolu düzeltmesi olduğu için (ve `dotnet build`/`dotnet test`/`npm run build:web` gibi build'i bozmadığı için) bu turda dokunulmadı; öncelikli eksikler listesinde işaretlendi.

## 7. Outbox / Worker Durumu

**Gerçek çalışıyor, placeholder değil.** Doğrulama:

- `CreateComplaintCommandHandler.HandleAsync` → `_unitOfWork.ExecuteInTransactionAsync(...)` içinde hem `_complaintRepository.AddAsync` hem `_integrationOutboxRepository.AddAsync` çağrılıyor → **aynı transaction, dual-write yok**, mimari kurallara (bkz. görev tanımındaki madde 1-2) uygun.
- `OutboxProcessingService.ProcessDueMessagesAsync` → due mesajları okuyor, `Processing` durumuna alıyor, `IntegrationAttempt` kaydı açıyor, ilgili `MunicipalityDatabaseConnection`'ı çözüyor, `IMunicipalityComplaintWriter`'ı provider'a göre seçiyor.
- Hata durumunda: `attempt.Fail(...)`, `AttemptCount >= MaxRetryCount` ise `Failed`, değilse exponential backoff ile `next_retry_at` (`CalculateNextRetryAt`) atanarak `Pending`'e geri dönüyor.
- `OutboxProcessorService` (Worker `BackgroundService`) periyodik polling yapıyor (`PollIntervalSeconds`, varsayılan 10sn), exception'ları yutup logluyor, servis çökmüyor.
- Belediye tarafı yazım idempotent: `ON CONFLICT (main_complaint_id) DO NOTHING`.
- Unit testlerle kapsanmış (`OutboxProcessingServiceTests`).

## 8. Admin Web Durumu

**Mock/statik.** `apps/admin-web/src/App.tsx` — hardcoded metrik kartları (`Açık bildirim: 128`, `Bugün gelen: 34`, `SLA riski: 7`) ve statik tablo satırları. Hiçbir API çağrısı, routing, state management veya auth akışı yok. `vite build` başarıyla derleniyor çünkü sadece statik JSX render ediyor.

## 9. Citizen Web Durumu

**Mock/statik.** `apps/citizen-web/src/App.tsx` — statik form (başlık/açıklama input'ları, "Konum seçimi" placeholder div'i, "Bildirim oluştur" butonu `type="button"` ve **onClick handler'ı yok**). Harita entegrasyonu (Leaflet/MapLibre) yok. API'ye hiç bağlı değil.

## 10. Citizen Mobile Durumu

**Mock/statik.** `App.tsx` → `HomeScreen.tsx` — statik `TextInput`/`TouchableOpacity`, gönder butonunun `onPress` handler'ı yok. `src/services/location/LocationProvider.ts` sadece bir **arayüz** (`LocationProvider` interface + `Coordinates` type) — gerçek Expo `expo-location` implementasyonu yok, `package.json`'da `expo-location` veya `expo-image-picker` bağımlılığı da yok. `typecheck` script'i tanımlı değil (bkz. bölüm 2).

## 11. Güvenlik Riskleri

- **Secret**: `.env.example` sadece placeholder/`change-me-local` değerleri içeriyor, `.gitignore` `.env` ve `.env.*`'i (`.env.example` hariç) doğru şekilde dışlıyor. Gerçek secret repoya sızmamış. ✅
- **Public data leak**: Şu an sadece "create" ve "resolve" endpoint'leri var; iç not (`isInternal`) veya vatandaşa görünmeyen geçmiş (`isVisibleToCitizen=false`) döndüren bir public tracking endpoint'i **henüz yok** — yani bu risk bugün için gerçekleşmiyor, ama `GET /api/public/complaints/track/{trackingCode}` eklendiğinde `docs/security.md`'deki KVKK/maskeleme kurallarına uyulması kritik olacak (henüz kod yok, doğrulanacak bir şey de yok).
- **File upload**: Content-type + dosya imzası kontrolü, boyut limiti (10MB/dosya, 50MB request body), rastgele dosya adı, path traversal koruması, SHA256 hash — hepsi mevcut ve dokümanla eşleşiyor. `IFileSafetyScanner` şu an `NoOpFileSafetyScanner` (antivirüs taraması yok) — `docs/security.md` bunu zaten "production öncesi eklenmeli" olarak işaretlemiş, tutarlı.
- **Auth**: Sistemde **hiç kimlik doğrulama yok**. `CurrentUserService` hardcoded `IsAuthenticated => false` stub. `AddAuthentication`/JWT/`[Authorize]` backend'de hiçbir yerde kullanılmıyor. Bu aşamada risk değil (henüz korunması gereken admin endpoint'i yok) ama admin API'leri eklenmeden **önce** auth'un devreye girmesi şart.
- **Rate limit / CORS**: `ServiceCollectionExtensions.cs` içinde rate limiting veya CORS policy kaydı yok. Public complaint oluşturma endpoint'i şu haliyle throttling olmadan herkese açık — spam/DoS riski (görev kapsamındaki "DoS saldırıları" kısıtı gereği bunu ben oluşturmuyorum, sadece mevcut kod tabanındaki eksikliği raporluyorum).

## 12. Multi-Tenant Riskleri

Şu an **admin/employee tarafı hiç kodda olmadığı için** multi-tenant izolasyon ihlali riski **aktif olarak test edilebilir durumda değil** — ama altyapı doğru kurulmuş:

- `Complaint`, `MunicipalityBoundary`, `MunicipalityDatabaseConnection` gibi tüm tenant-scoped entity'ler `municipalityId` alanı taşıyor.
- Belediye tespiti tamamen konum tabanlı (`ST_Contains`), EXIF'ten değil — mimari kurala (madde 3) uyuyor.
- Belediye DB bağlantı bilgisi (`MunicipalityDatabaseConnection`) her belediye için ayrı çözümleniyor (`MunicipalityDatabaseConnectionResolver`).
- **Eksik**: `MunicipalityEmployee`/`MunicipalityAdmin`/`SystemAdmin` rollerine göre veri filtreleme yapan hiçbir kod yok çünkü bu roller için henüz endpoint/controller/authorization policy yazılmamış. Admin API'leri eklendiğinde her sorgunun `municipalityId` ile scope edildiğinin (ve `SystemAdmin` dışında hiçbir rolün bunu bypass edemediğinin) test edilmesi gerekecek.

## 13. Öncelikli Eksikler

1. **Auth**: `POST /api/auth/login`, JWT/refresh token akışı, `[Authorize]` policy'leri, gerçek `ICurrentUserService` implementasyonu. Domain'de `User`/`Role`/`UserRole`/`RefreshToken` entity'leri zaten hazır, sadece API/Infrastructure tarafı yok.
2. **Admin API'leri**: `GET /api/admin/complaints` ve ilgili liste/detay/atama/durum güncelleme/yorum endpoint'leri, `municipalityId` bazlı tenant filtreleme dahil.
3. **Public tracking endpoint**: `GET /api/public/complaints/track/{trackingCode}` — `docs/security.md`'deki iç not/maskeleme kurallarına uygun şekilde.
4. **Docker Compose init script mount hatası** (bölüm 6): `main-db` ve `municipality-sample-db` servislerinin init volume'larının doğru klasörlere (`./database/main-db`, `./database/municipality-sample-db`) işaret etmesi gerekiyor.
5. **Frontend–API entegrasyonu**: Üç uygulamanın da (admin-web, citizen-web, citizen-mobile) gerçek API çağrılarına, routing'e ve state yönetimine bağlanması. Şu an hepsi statik mock.
6. **Citizen mobile**: `expo-location` ve `expo-image-picker` entegrasyonu, `LocationProvider` interface'inin gerçek implementasyonu.
7. **CORS + rate limiting**: Public endpoint'ler için en azından temel throttling.
8. **`SubmitComplaintCommand.cs`**: Kullanılmayan/yetim dosyanın temizlenmesi (düşük öncelik, kozmetik).

## 14. Devam Planı

Önerilen sıra (bir sonraki fazlar için, bu turda uygulanmadı):

1. Auth altyapısını kur (JWT + login endpoint + `ICurrentUserService` gerçek implementasyonu) — admin API'lerinin ön koşulu.
2. `GET /api/admin/complaints` ve temel admin akışını (liste, detay, atama, durum, iç not) tenant-scoped olarak ekle.
3. `GET /api/public/complaints/track/{trackingCode}` endpoint'ini KVKK/maskeleme kurallarına uygun şekilde ekle.
4. `docker-compose.yml` init mount yollarını düzelt, `docker compose config` + `docker compose up` ile gerçek ortamda doğrula (bu makinede Docker kurulursa).
5. Citizen web'e harita (Leaflet/MapLibre) ve gerçek API entegrasyonu ekle.
6. Citizen mobile'a `expo-location`/`expo-image-picker` ve gerçek API entegrasyonu ekle.
7. Admin web'i gerçek API'ye bağla, auth akışını ekle.
8. CORS policy + rate limiting ekle.
