# CitizenPlatform Project Status

_Son güncelleme: 2026-07-12 (Aşama 7 — Auth + Belediye Yönetim Paneli Backend API'leri)_
_Önceki doğrulama: commit `52f9f41` (main, "Add outbox based municipality sync worker")_

## 1. Genel Durum

Backend artık **auth + belediye admin API'leri dahil olmak üzere** anlamlı ölçüde olgun: JWT tabanlı kimlik doğrulama, rol/tenant bazlı yetkilendirme, şikayet yönetimi (liste/detay/durum/atama/yorum/geçmiş), dashboard özet API'si ve kategori/birim yönetimi eklendi — hepsi Clean Architecture katmanlarına uygun, outbox pattern korunarak (dual-write yok) ve test edilmiş durumda.

Buna karşılık üç frontend uygulaması (admin-web, citizen-web, citizen-mobile) hâlâ statik/mock; public tracking endpoint'i, docker-compose init-mount düzeltmesi ve refresh token akışı hâlâ yapılmadı.

Kısaca: **Backend'in "vatandaş şikayet gönderir" + "belediye çalışanı yönetir" akışlarının ikisi de artık gerçek ve test edilmiş durumda. Kalan büyük boşluk: frontend entegrasyonu, public tracking, docker-compose düzeltmesi.**

## 2. Build/Test Durumu

| Komut | Sonuç |
|---|---|
| `dotnet restore backend/CitizenPlatform.sln` | ✅ Başarılı |
| `dotnet build backend/CitizenPlatform.sln` | ✅ Başarılı — 0 uyarı, 0 hata |
| `dotnet test backend/CitizenPlatform.sln` | ✅ Başarılı — **70/70 test geçti** (64 unit + 6 integration; önceki aşamada 24 test vardı) |
| `npm run build:web` | ✅ Başarılı — hem `admin-web` hem `citizen-web` derleniyor (bu aşamada frontend'e dokunulmadı) |
| `docker compose config` | ⚠️ Bu makinede Docker kurulu değil, önceki aşamadaki gibi çalıştırılamadı; bu tur da düzeltilmedi (bkz. bölüm 13). |

Yeni JWT paketleri (`Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`) `Directory.Packages.props`'a eklendi ve restore ile doğrulandı. Yeni EF Core migration (`AddAuthAndAdminComplaintFields`) `dotnet ef migrations add` ile üretildi; `users.password_hash`, `complaints.closed_at`, `complaint_status_histories.is_visible_to_citizen` kolonlarını ekliyor.

## 3. Çalışan Kısımlar

Önceki aşamadaki akışlara ek olarak, bu aşamada eklenip test edilenler:

- **Login**: `POST /api/auth/login` — email/şifre doğrulama (PBKDF2-HMACSHA256 hash), başarısızlıkta genel "Invalid email or password." mesajı (hangi alan yanlış sızdırılmıyor), başarıda JWT access token + kullanıcı bilgisi.
- **Mevcut kullanıcı**: `GET /api/auth/me` — token'daki `sub` claim'inden kullanıcıyı, rollerini ve belediyesini döner.
- **JWT authentication + authorization policy'leri**: `RequireSystemAdmin`, `RequireMunicipalityAdmin`, `RequireMunicipalityEmployee`, `RequireAdminAccess` — gerçek `IAuthorizationService` ile test edilmiş (bkz. `AdminAuthorizationPolicyTests`), Citizen rolü hiçbirini geçemiyor.
- **Tenant scope**: `TenantScope` merkezi yardımcı sınıfı — SystemAdmin her belediyeyi görebiliyor/filtreleyebiliyor, MunicipalityAdmin/Employee her zaman kendi belediyesine kilitleniyor (request'teki farklı `municipalityId` yok sayılıyor).
- **Admin complaint yönetimi**: liste (filtre + sayfalama + arama, maskelenmiş vatandaş adı), detay (maskelenmemiş, tenant-scoped), durum güncelleme (`Complaint.ChangeStatus`, aynı statüye geçiş 400 döner, Resolved/Closed'da `closedAt` set edilir), birime atama (`Complaint.AssignToDepartment`, başka belediyenin birimine atama engelleniyor), admin yorumu (`Complaint.AddComment`, internal/public ayrımı korunuyor), geçmiş (status + yorum + atama, iç notlar dahil — sadece admin görür).
- **Dashboard özet API'si**: toplam/açık/bugünkü/çözülen/kapanan sayılar, ortalama çözüm süresi, status/kategori/departman bazlı kırılım — tenant-scoped, N+1 sorgu yok.
- **Kategori/Birim yönetimi**: liste (tüm admin rolleri), oluşturma/güncelleme (sadece MunicipalityAdmin/SystemAdmin), silme yerine `isActive` toggle.
- **Outbox genişletildi**: `ComplaintStatusChanged` ve `ComplaintAssigned` mesajları artık worker tarafında gerçekten belediye sample DB'sine yazılıyor (idempotent, `municipal_complaints`/`municipal_complaint_status_logs` günceller); karşılık gelen kayıt henüz senkronize olmadıysa retry ediliyor. `AdminCommentAdded` mesajı bilinçli olarak no-op tamamlanıyor (sample DB'de yorum tablosu yok — bkz. `docs/complaint-flow.md`).
- **CORS + rate limiting**: `CORS__ALLOWED_ORIGINS` ile konfigüre edilebilir CORS policy; login (`5/dakika`) ve public complaint/resolve endpointleri (`30/dakika`) için fixed-window rate limiting.
- **Development demo kullanıcı seed'i**: sadece `Development` ortamında, Demo Belediyesi mevcutsa `systemadmin@demo.local` / `admin@demo.local` / `employee@demo.local` (şifre: `Demo123!`) oluşturuluyor; municipality seed'i yoksa uyarı loglayıp atlıyor (API'yi çökertmiyor).

Önceki aşamadan değişmeden çalışmaya devam eden: konumdan belediye tespiti, şikayet oluşturma (JSON/multipart), dosya yükleme/güvenliği, `ComplaintCreated` outbox akışı, sağlık kontrolü.

## 4. Dokümante Edilmiş Ama Kodda Eksik Kısımlar

`docs/api-contract.md` artık auth/admin API'lerini `docs/auth.md` ve `docs/admin-api.md`'ye işaret ediyor (tekrar/duplikasyon yok). Kod ile doküman arasında bilinen tek fark:

| Endpoint | Kodda var mı? | Dokümante mi? |
|---|---|---|
| `POST /api/auth/login` | ✅ Var | ✅ `docs/auth.md` |
| `GET /api/auth/me` | ✅ Var | ✅ `docs/auth.md` |
| `GET/PUT /api/admin/complaints/...` | ✅ Var | ✅ `docs/admin-api.md` |
| `GET /api/admin/dashboard/summary` | ✅ Var | ✅ `docs/admin-api.md` |
| `GET/POST/PUT /api/admin/categories`, `/departments` | ✅ Var | ✅ `docs/admin-api.md` |
| `GET /api/public/complaints/track/{trackingCode}` | ❌ **Hâlâ yok** | ❌ Hâlâ yok |

`SubmitComplaintCommand.cs` hâlâ kullanılmayan/yetim dosya olarak duruyor (önceki aşamadan, bu turda dokunulmadı).

## 5. Backend Durumu

- **Api**: `AuthController`, `AdminComplaintsController`, `AdminDashboardController`, `AdminCategoriesController`, `AdminDepartmentsController` eklendi. JWT bearer authentication, authorization policy'leri (`Api/Authorization/AuthorizationPolicySetup.cs` — hem gerçek uygulama hem testler aynı tanımı kullanıyor), CORS, rate limiting `ServiceCollectionExtensions`/`Program.cs` içinde wiring edildi. Controller'lar ince kalıyor — business logic Application handler'larında.
- **Application**: `Features/Auth`, `Features/AdminComplaints`, `Features/AdminDashboard`, `Features/AdminCategories`, `Features/AdminDepartments` eklendi. `TenantScope` (Common) merkezi scoping sağlıyor. `AdminScopedResult<T>` yeni bir Result varyantı — 404 (tenant dışı/yok) ile 400 (validasyon) arasında controller'ın doğru HTTP kodunu seçebilmesi için.
- **Domain**: `User.PasswordHash` + `SetPasswordHash`, `ComplaintStatusHistory.IsVisibleToCitizen`, `Complaint.ClosedAt` (Resolved/Closed'da otomatik set), `ComplaintCategory`/`Department` için `Rename`/`Activate`/`Deactivate` eklendi. Hepsi domain metotları üzerinden değiştiriliyor, dışarıdan doğrudan alan ataması yok.
- **Infrastructure**: `PasswordHasher` (PBKDF2), `JwtTokenService`, gerçek `CurrentUserService` (artık `IHttpContextAccessor` + JWT claim'lerinden okuyor — **stub kaldırıldı**), `UserRepository`, `MunicipalityRepository`, `AdminComplaintQueryRepository` (join'li, N+1'siz), `AdminDashboardRepository`, `DevelopmentDataSeeder`.
- **Integrations**: `PostgreSqlMunicipalityComplaintWriter` iki yeni metotla genişledi (`WriteComplaintStatusChangedAsync`, `WriteComplaintAssignedAsync`), her ikisi de idempotent ve "henüz senkronize olmamış kayıt" durumunu retry'a bırakıyor.
- **Worker**: `OutboxProcessingService.DispatchAsync` artık mesaj tipine göre yönlendiriyor (`ComplaintCreated`/`ComplaintStatusChanged`/`ComplaintAssigned`/`AdminCommentAdded`), placeholder değil.

## 6. Database Durumu

- **Migration**: Yeni migration `AddAuthAndAdminComplaintFields` eklendi (toplam 4 migration). `users.password_hash` (nullable), `complaints.closed_at` (nullable), `complaint_status_histories.is_visible_to_citizen` (not null, default true).
- **Seed**: Değişmedi — `database/main-db/003_seed_demo_municipality.sql` demo belediye verisini sağlıyor; `DevelopmentDataSeeder` bunun üstüne demo kullanıcı/rol ekliyor (uygulama içi, SQL değil).
- **Municipality sample DB**: Şema değişmedi (`municipal_complaints`, `municipal_complaint_status_logs`) — yeni outbox writer metotları mevcut kolonları (`status`, `department_name`) güncelliyor, yeni migration gerekmedi.
- **⚠️ Docker Compose init mount hatası**: Önceki aşamada tespit edildi, **bu turda hâlâ düzeltilmedi** (görev talimatı gereği — "Docker compose düzeltmesini sonra yapacağız"). Detay: bölüm 13.
- Bu makinede Docker olmadığı için yeni migration gerçek bir Postgres'e **uygulanamadı**; migration dosyası `dotnet ef migrations add` ile üretildi ve `dotnet build` ile derleme doğrulaması yapıldı, ama `dotnet ef database update` çalıştırılmadı.

## 7. Outbox / Worker Durumu

**Genişledi, hâlâ gerçek çalışıyor.** `ComplaintCreated`'a ek olarak:

- `ComplaintStatusChanged` → belediye DB'sindeki `municipal_complaints.status` günceller + `municipal_complaint_status_logs`'a idempotent kayıt ekler (`ON CONFLICT (main_complaint_id, status) DO NOTHING`). Karşılık gelen `municipal_complaints` kaydı yoksa (`ComplaintCreated` henüz işlenmemişse) `Result.Failure` döner → outbox `Pending`'e geri döner, retry edilir. Veri kaybı yok.
- `ComplaintAssigned` → `municipal_complaints.department_name` günceller, aynı "henüz yok, retry et" garantisi.
- `AdminCommentAdded` → sample DB şemasında yorum tablosu olmadığı için `OutboxProcessingService.DispatchAsync` bu tipi doğrudan `Result.Success()` ile tamamlıyor (dış sisteme yazma yok, bilinçli tasarım kararı).

`OutboxProcessingServiceTests` mevcut testleri (yeni interface metotlarına uyum sağlandı) hâlâ geçiyor; yeni mesaj tiplerinin worker dispatch mantığı `docs/complaint-flow.md` içinde belgelendi.

## 8. Admin Web Durumu

**Değişmedi — hâlâ mock/statik.** Bu aşamada frontend'e dokunulmadı (görev kapsamı backend'di). Şimdi gerçek bir backend API'si var (auth + complaint CRUD + dashboard), bir sonraki aşamada admin-web bu API'lere bağlanabilir.

## 9. Citizen Web Durumu

**Değişmedi — hâlâ mock/statik.**

## 10. Citizen Mobile Durumu

**Değişmedi — hâlâ mock/statik.**

## 11. Güvenlik Riskleri

- **Secret**: `.env.example`'a eklenen `JWT__SECRET` değeri de sadece placeholder (`change-me-local-development-secret-please-replace`), gerçek secret yazılmadı. `.gitignore` değişmedi, hâlâ doğru çalışıyor. ✅
- **Şifreleme**: Şifreler PBKDF2-HMACSHA256 (100.000 iterasyon) ile hashleniyor, düz metin hiçbir zaman saklanmıyor. ✅
- **Login enumeration**: Yanlış email ve yanlış şifre aynı genel mesajı (`Invalid email or password.`) döndürüyor — kullanıcı numaralandırma (user enumeration) riski azaltıldı. ✅
- **Auth artık var**: Önceki aşamada "hiç auth yok" riski raporlanmıştı; şimdi JWT + policy'ler devrede, admin endpoint'leri korunuyor. Ancak **refresh token yok** — access token süresi dolunca kullanıcı yeniden login olmalı (bu fazda kabul edilebilir, ileride eklenecek).
- **Rate limit + CORS eklendi**: Login (5/dk) ve public complaint/resolve (30/dk) endpointleri artık throttling altında; CORS origin listesi env'den geliyor. Bu basit fixed-window limitler; production'da daha sofistike bir çözüm (örn. Redis-backed distributed limiter) gerekebilir — şu an tek-instance deployment için yeterli.
- **Public tracking henüz yok**: `docs/security.md`'deki iç not/maskeleme kuralları bu yüzden hâlâ test edilemiyor (kod yok).
- **İç not sızıntısı riski — kontrol edildi**: Admin `history`/`detail` endpointleri iç notları (`isInternal=true`) döner ama bunlar `RequireAdminAccess` policy'si arkasında; public tarafa açık hiçbir endpoint bu veriyi döndürmüyor (public tracking eklenmediği için bu risk şu an gerçekleşmiyor).

## 12. Multi-Tenant Riskleri

**Artık aktif olarak test ediliyor** (önceki aşamada "kod yok, test edilemiyor" durumundaydı):

- `TenantScope.ResolveListFilter`: MunicipalityAdmin/Employee her zaman kendi `municipalityId`'sine kilitleniyor, request'teki farklı değer görmezden geliniyor — `TenantScopeTests`, `AdminComplaintListQueryHandlerTests` ile test edildi.
- `TenantScope.CanAccess`: Detay/status/assign/comment handler'ları complaint'in `municipalityId`'sini scope ile karşılaştırıp uyuşmazsa **404** (403 değil — kayıt varlığı sızdırılmıyor) döndürüyor — `AdminComplaintDetailQueryHandlerTests`, `UpdateComplaintStatusCommandHandlerTests`, `AssignComplaintCommandHandlerTests`, `AddAdminCommentCommandHandlerTests` ile test edildi.
- **Department cross-tenant koruması**: `AssignComplaintCommandHandler` complaint'in belediyesiyle department'ın belediyesini karşılaştırıyor, uyuşmazsa 400 — test edildi (`AssignComplaintCommandHandlerTests.HandleAsync_WhenDepartmentBelongsToDifferentMunicipality_ReturnsFailureAndDoesNotAssign`).
- **SystemAdmin bypass**: SystemAdmin tüm belediyeleri görebiliyor/yönetebiliyor — `TenantScopeTests`, `AdminComplaintListQueryHandlerTests.HandleAsync_WhenSystemAdminRequestsNoFilter_SearchesAllMunicipalities` ile test edildi.
- **Authorization policy hiyerarşisi**: Citizen rolü hiçbir admin policy'sini geçemiyor, token olmadan (anonymous principal) hiçbir policy geçilemiyor — `AdminAuthorizationPolicyTests` ile gerçek `IAuthorizationService` üzerinden test edildi (framework mock'lanmadı).

## 13. Öncelikli Eksikler

1. **Public tracking endpoint**: `GET /api/public/complaints/track/{trackingCode}` — `docs/security.md`'deki iç not/maskeleme kurallarına uygun şekilde. Artık `isVisibleToCitizen` alanı da domain'de mevcut, bu endpoint'i doğru filtrelemek için hazır.
2. **Docker Compose init script mount hatası**: `main-db` ve `municipality-sample-db` servislerinin init volume'larının doğru klasörlere (`./database/main-db`, `./database/municipality-sample-db`) işaret etmesi gerekiyor. Bilinçli olarak bu turda da ertelendi (görev talimatı: "Docker compose düzeltmesini sonra yapacağız").
3. **Frontend–API entegrasyonu**: admin-web artık gerçek bir auth + complaint + dashboard API'sine bağlanabilir; citizen-web/mobile gerçek API çağrılarına, routing'e ve state yönetimine bağlanmalı.
4. **Refresh token**: Şu an sadece access token var (60 dk varsayılan ömür). `RefreshToken` domain entity'si hazır, akış yazılmadı.
5. **Citizen mobile**: `expo-location`/`expo-image-picker` entegrasyonu.
6. **Migration'ın gerçek DB'ye uygulanması**: Bu makinede Docker/Postgres olmadığı için `AddAuthAndAdminComplaintFields` migration'ı sadece derleme seviyesinde doğrulandı, gerçek veritabanına hiç uygulanmadı.
7. **`SubmitComplaintCommand.cs`**: Hâlâ temizlenmedi (düşük öncelik).

## 14. Devam Planı

1. `GET /api/public/complaints/track/{trackingCode}` endpoint'ini KVKK/maskeleme kurallarına uygun şekilde ekle.
2. `docker-compose.yml` init mount yollarını düzelt, gerçek bir Postgres ortamında migration'ı uygula ve `docker compose config`/`up` ile doğrula.
3. Admin web'i gerçek auth + admin API'lerine bağla (login sayfası, complaint listesi/detayı, dashboard).
4. Citizen web'e harita (Leaflet/MapLibre) ve gerçek API entegrasyonu ekle (tracking endpoint'i eklendikten sonra).
5. Citizen mobile'a `expo-location`/`expo-image-picker` ve gerçek API entegrasyonu ekle.
6. Refresh token akışını ekle.
