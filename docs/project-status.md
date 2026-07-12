# CitizenPlatform Project Status

_Son güncelleme: 2026-07-12 (Aşama 8 — Docker düzeltmesi + ilk canlı DB doğrulaması + Frontend entegrasyonu)_
_Önceki doğrulama: commit `331aaa6` (main, "Add auth and admin complaint management api")_

## 1. Genel Durum

**Bu aşamada proje ilk kez uçtan uca, gerçek bir Docker/Postgres ortamında çalıştırıldı ve doğrulandı** — önceki iki aşamada backend sadece derleme + fake-repository unit testleriyle doğrulanmıştı, hiçbir endpoint gerçek bir veritabanına karşı hiç çağrılmamıştı. Bu turda:

1. Docker Desktop kullanıcı tarafından kuruldu, `docker-compose.yml`'deki init-mount hatası gerçek nedeniyle (EF migration'dan önce index/seed scriptlerinin çalışmaya çalışması) bulunup düzeltildi.
2. Üç container (`main-db`, `municipality-sample-db`, `minio`) ayağa kaldırıldı, EF Core migration'ları canlı veritabanına uygulandı, demo belediye seed'i çalıştırıldı.
3. API ve Worker canlı DB'ye karşı çalıştırılıp login, şikayet oluşturma, admin yönetimi ve outbox senkronizasyonu **gerçek HTTP istekleriyle** uçtan uca doğrulandı.
4. **admin-web ve citizen-web artık gerçek API'ye bağlı** — mock veri kalmadı. Headless tarayıcı (Playwright) ile gerçek bir kullanıcı akışı (login → dashboard, konum paylaş → şikayet oluştur → takip kodu al) çalıştırılıp ekran görüntüleriyle doğrulandı.
5. **citizen-mobile** gerçek API çağrılarına ve `expo-location` ile gerçek konum servisine bağlandı (TypeScript typecheck geçiyor), ama fiziksel cihaz/emulator olmadığı için çalışma zamanında görsel olarak doğrulanamadı.

Kısaca: **Backend artık sadece "test edilmiş" değil, gerçek ortamda "çalıştığı kanıtlanmış" durumda. admin-web ve citizen-web gerçek API'ye bağlı ve tarayıcıda doğrulandı. citizen-mobile kod olarak bağlandı ama cihazda denenmedi. Kalan büyük boşluk: public tracking endpoint'i ve production'a uygun sertleştirme (refresh token, CORS/rate-limit ayarları, gerçek dosya depolama).**

## 2. Build/Test Durumu

| Komut | Sonuç |
|---|---|
| `dotnet build backend/CitizenPlatform.sln` | ✅ Başarılı — 0 uyarı, 0 hata |
| `dotnet test backend/CitizenPlatform.sln` | ✅ **70/70 test geçti** (64 unit + 6 integration, değişmedi) |
| `npm run build:web` | ✅ admin-web + citizen-web derleniyor |
| `npm --workspace @citizen-platform/citizen-mobile run typecheck` | ✅ Artık script var (bu turda eklendi) ve **hatasız geçiyor** |
| `docker compose config` | ✅ **Artık çalıştırılabiliyor** — Docker bu turda kuruldu, config geçerli |
| `docker compose up -d` | ✅ 3 container da `healthy` |
| `dotnet ef database update` | ✅ **İlk kez gerçek Postgres'e uygulandı** — 4 migration da başarıyla işlendi |
| `database/main-db/002_indexes.sql`, `003_seed_demo_municipality.sql` | ✅ Migration sonrası container içinde `psql` ile çalıştırıldı, Demo Belediyesi doğrulandı |

### Canlı ortamda doğrulanan uçtan uca akışlar (curl + Playwright ile)

- `GET /health`, `GET /api/health` → Healthy (gerçek DB bağlantısı ile)
- `POST /api/auth/login` (`admin@demo.local` / `Demo123!`) → gerçek JWT, doğru roller/belediye
- `GET /api/auth/me`, yanlış şifre → 401, token'sız admin endpoint → 401
- `GET /api/public/municipalities/resolve` → PostGIS `ST_Contains` ile gerçek sınır sorgusu (içeride/dışarıda her iki durum da test edildi)
- `POST /api/public/complaints` → gerçek `Complaint` + outbox mesajı yazıldı
- `GET /api/admin/complaints`, `GET .../dashboard/summary` → gerçek join sorguları, doğru maskeleme (`"A*** L***"`)
- `PUT .../status`, `PUT .../assign`, `POST .../comments`, `GET .../history` → hepsi gerçek DB'de doğru şekilde işlendi
- **Worker** çalıştırıldı → `ComplaintCreated` ve `ComplaintStatusChanged` outbox mesajları `Completed` oldu, belediye `municipality-sample-db`'sinde `municipal_complaints.status` ve `department_name` doğru şekilde güncellendi, `municipal_complaint_status_logs`'a idempotent kayıt düştü.
- **admin-web**: Playwright ile login → gerçek dashboard metrikleri (toplam/açık/bugün) ve gerçek complaint listesi ekran görüntüsüyle doğrulandı.
- **citizen-web**: Playwright ile (mock geolocation) "Konumumu kullan" → "Demo Belediyesi" çözümlendi → form gönderildi → gerçek takip kodu (`BLD-2026-...`) alındı, admin-web listesinde anında göründü.

Bu, projenin **ilk kez tam uçtan uca (frontend → API → DB → outbox → belediye DB) çalıştığı ve kanıtlandığı** an.

## 3. Docker Compose Durumu

**Önceki aşamalarda tespit edilen hata gerçek kök nedeniyle düzeltildi** (önceki analiz kısmen yanlıştı — `municipality-sample-db` mount'u zaten doğruydu, sorun sadece `main-db`'deydi):

- **Kök neden**: `database/main-db/002_indexes.sql` ve `003_seed_demo_municipality.sql`, EF Core migration'larının oluşturduğu tablolara (`municipality_boundaries`, `complaints`, ...) bağımlı. Bunlar `docker-entrypoint-initdb.d` altında otomatik çalıştığında (container ilk açılışta, migration'lardan önce) `relation "public.municipality_boundaries" does not exist` hatasıyla **container'ın çökmesine** neden oluyordu.
- **Çözüm**: Yeni `database/docker-init/001_enable_postgis.sql` dosyası oluşturuldu (sadece `CREATE EXTENSION IF NOT EXISTS postgis;`) ve `docker-compose.yml`'deki `main-db` init mount'u buraya yönlendirildi. `database/main-db/001-003` scriptleri olduğu gibi kaldı, ama artık **migration sonrası manuel/scriptli çalıştırılması gereken** dosyalar olarak `database/main-db/README.md`'de net şekilde belgelendi.
- `municipality-sample-db` mount'u zaten doğruydu ve hep doğru çalıştı — önceki aşamadaki proje-status notu bu konuda hatalıydı, düzeltildi.
- Doğru başlatma sırası artık `database/main-db/README.md`'de adım adım yazıyor: `docker compose up -d` → `dotnet ef database update` → `002_indexes.sql` → `003_seed_demo_municipality.sql`.

## 4. Frontend Durumu

### Admin Web — **artık gerçek API'ye bağlı**

- `src/lib/api.ts`: fetch tabanlı client (login, complaint listesi, dashboard summary), JWT `localStorage`'da saklanıyor, 401'de otomatik oturum temizleniyor.
- Login ekranı eklendi (`admin@demo.local` / `Demo123!` demo ipucuyla), başarılı girişte gerçek dashboard'a geçiyor.
- Dashboard metrik kartları ve bildirim tablosu artık **gerçek `GET /api/admin/dashboard/summary` ve `GET /api/admin/complaints` verisiyle** doluyor, hardcoded veri kalmadı.
- Vite dev server proxy'si (`/api` → `http://localhost:5080`) eklendi, CORS'a takılmadan çalışıyor.
- **Eksik**: routing yok (tek sayfa, sidebar linkleri hâlâ işlevsiz), complaint detay/durum-güncelleme/atama ekranları yok (sadece liste var), kategori/birim yönetim ekranı yok.

### Citizen Web — **artık gerçek API'ye bağlı**

- `src/lib/api.ts`: `resolveMunicipality`, `createComplaint`, tarayıcı `navigator.geolocation` sarmalayıcısı.
- Form artık gerçek: kategori seçimi (demo kategorileri hardcoded — aşağıda not), "Konumumu kullan" butonu gerçek konum alıp `resolve` endpoint'ini çağırıyor, gönderim gerçek `POST /api/public/complaints` yapıyor ve gerçek takip kodunu gösteriyor.
- **Eksik**: harita/görsel konum seçimi yok (sadece tek tık "konumumu kullan"), fotoğraf yükleme UI'da yok (backend destekliyor ama form'a eklenmedi), kategori listesi gerçek bir public endpoint'ten gelmiyor (aşağıda not).

### Citizen Mobile — **kod olarak bağlandı, cihazda doğrulanamadı**

- `expo-location` + `expo-constants` bağımlılıkları eklendi, `ExpoLocationProvider` (gerçek `LocationProvider` implementasyonu) yazıldı — önceki aşamada sadece interface vardı.
- `src/services/api.ts`: aynı `resolveMunicipality`/`createComplaint` sözleşmesi, `EXPO_PUBLIC_API_BASE_URL`/`app.json extra.apiBaseUrl` ile yapılandırılabilir (Android emulator için `10.0.2.2` notu eklendi).
- `HomeScreen.tsx` artık gerçek state/handler'lara sahip: konum al → çöz → gönder → takip kodu göster.
- `package.json`'a `typecheck` script'i eklendi (**TS hatasız geçiyor**).
- **Doğrulanamadı**: Bu ortamda Android/iOS emulator veya fiziksel cihaz yok, Expo Metro bundler hiç çalıştırılmadı. Kod mantığı citizen-web ile birebir aynı desende yazıldı (o zaten tarayıcıda çalıştığı kanıtlandı) ama mobile'da gerçek bir çalışma zamanı denemesi yapılmadı — **bu net bir risk, Fable veya bir sonraki oturum fiziksel/emulator testi yapmalı**.

### Ortak sınırlama: Kategori listesi hardcoded

Hem citizen-web hem citizen-mobile, demo belediyenin 7 kategorisini (`database/main-db/003_seed_demo_municipality.sql`'deki sabit GUID'lerle) hardcoded olarak kullanıyor (`lib/categories.ts` / `services/categories.ts`). Gerçek bir "bu belediyenin kategorilerini listele" public endpoint'i yok. Bu, tek-belediye demo senaryosunda çalışır ama gerçek çok-belediyeli üretim için **yeni bir public endpoint gerekiyor** (öncelikli eksiklere eklendi).

## 5. Backend Durumu

Aşama 7'den değişmedi (bkz. git geçmişi) — bu turda sadece `docker-compose.yml`, `database/docker-init/`, `database/main-db/README.md` değişti. Kod tarafında yeni bir şey eklenmedi, sadece **var olan kod ilk kez gerçek bir ortamda doğrulandı**.

## 6. Database Durumu

- **Migration**: 4 migration da canlı `main-db`'ye uygulandı (`dotnet ef database update`), hiç hata yok.
- **Seed**: Demo Belediyesi + 7 kategori + 5 birim + kategori-birim kuralları + belediye DB bağlantı kaydı, hepsi doğrulandı (`SELECT` ile kontrol edildi).
- **Municipality sample DB**: Şema container açılışında otomatik oluştu (bu servis zaten doğru mount edilmişti), worker testleriyle gerçek yazma/güncelleme doğrulandı.
- **MinIO**: Container `healthy`, ama **hâlâ kullanılmıyor** — uygulama kodu sadece `LocalFileStorageService` içeriyor, `IFileStorageService`'in bir MinIO implementasyonu yok. `OBJECT_STORAGE_PROVIDER=Local` varsayılanı zaten bunu yansıtıyor ama docker-compose'daki MinIO servisi şu an "hazır ama bağlanmamış" durumda.

## 7. Outbox / Worker Durumu

Aşama 7'de yazılan mantık **bu turda ilk kez gerçek bir ortamda çalıştırılıp doğrulandı** (bkz. bölüm 2). `ComplaintCreated` ve `ComplaintStatusChanged` mesajları gerçekten işlendi ve belediye örnek veritabanına doğru şekilde yansıdı. `ComplaintAssigned` ve `AdminCommentAdded` kod olarak mevcut ama bu turda canlı ortamda ayrıca tetiklenmedi (assign/comment endpoint'leri curl ile çağrıldı ve outbox mesajı oluştu, ama worker'ın ikinci bir çalıştırmasında bunların da işlendiği ayrıca doğrulanmadı — yüksek olasılıkla çalışır çünkü aynı kod yolu, ama net olarak teyit edilmedi).

## 8-10. Frontend Durumu

Yukarı taşındı → bkz. bölüm 4.

## 11. Güvenlik Riskleri

Aşama 7'deki değerlendirme geçerliliğini koruyor, ek olarak:

- **Yerel `.env` dosyası oluşturuldu** (`.env.example`'dan kopyalanarak) — bu dosya `.gitignore` ile hariç tutuluyor, repoya girmedi. ✅
- **MinIO credential'ları** (`citizenplatform` / `change-me-local`) sadece local `.env`'de, placeholder niteliğinde. ✅
- Diğer tüm riskler (refresh token yok, public tracking yok, rate limit basit) değişmedi.

## 12. Multi-Tenant Riskleri

Değişmedi, aşama 7'deki testler hâlâ geçerli. Bu turda ayrıca gerçek DB üzerinde de dolaylı olarak doğrulandı (demo belediye dışında ikinci bir belediye olmadığı için cross-tenant senaryosu canlı ortamda ayrıca denenmedi, ama fake-repository testleri değişmeden geçmeye devam ediyor).

## 13. Öncelikli Eksikler

1. **Public tracking endpoint**: `GET /api/public/complaints/track/{trackingCode}` — hâlâ yok.
2. **Public kategori listesi endpoint'i**: citizen-web/mobile'daki hardcoded kategori listesinin yerini alacak gerçek bir "belediyenin kategorilerini getir" endpoint'i yok (bu turda ortaya çıkan yeni bir ihtiyaç).
3. **citizen-mobile cihaz/emulator testi**: Kod yazıldı ve typecheck geçti ama hiç çalıştırılmadı — bir sonraki oturumda Expo Go veya emulator ile denenmeli.
4. **admin-web routing + eksik ekranlar**: complaint detay/durum-güncelleme/atama/yorum ekranları, kategori/birim yönetim ekranı yok — sadece liste + dashboard var.
5. **citizen-web'de fotoğraf yükleme UI'ı** yok (backend destekliyor).
6. **MinIO entegrasyonu**: Container ayakta ama `IFileStorageService`'in Minio implementasyonu hiç yazılmadı.
7. **Refresh token**: Hâlâ yok.
8. **`SubmitComplaintCommand.cs`**: Hâlâ temizlenmedi.

## 14. Devam Planı

1. Public tracking + public kategori listesi endpoint'lerini ekle (ikisi de citizen tarafının gerçek kullanılabilirliği için kritik).
2. citizen-mobile'ı Expo Go veya emulator ile gerçekten çalıştırıp doğrula.
3. admin-web'e routing ekleyip complaint detay/durum/atama/yorum ekranlarını gerçek API'ye bağla.
4. citizen-web'e fotoğraf yükleme ve gerçek harita (Leaflet/MapLibre) ekle.
5. MinIO `IFileStorageService` implementasyonunu yaz, `OBJECT_STORAGE_PROVIDER=Minio` ile local'den geçişi mümkün kıl.
6. Refresh token akışını ekle.
7. Production sertleştirmesi: gerçek `JWT__SECRET`, CORS origin listesinin daraltılması, rate limit'in gözden geçirilmesi.

## 15. Bu ortamda çalıştırma notları (bir sonraki oturum için)

```powershell
# 1. Servisleri başlat (main-db, municipality-sample-db, minio)
docker compose up -d

# 2. (Sadece ilk kurulumda) migration + seed
dotnet ef database update --project backend/src/CitizenPlatform.Infrastructure/CitizenPlatform.Infrastructure.csproj --startup-project backend/src/CitizenPlatform.Api/CitizenPlatform.Api.csproj
docker exec -i citizenplatform-main-db psql -U citizen_platform -d citizen_platform < database/main-db/002_indexes.sql
docker exec -i citizenplatform-main-db psql -U citizen_platform -d citizen_platform < database/main-db/003_seed_demo_municipality.sql

# 3. Backend'i çalıştır
dotnet run --project backend/src/CitizenPlatform.Api/CitizenPlatform.Api.csproj        # http://localhost:5080
dotnet run --project backend/src/CitizenPlatform.Worker/CitizenPlatform.Worker.csproj  # outbox sync

# 4. Frontend'leri çalıştır
npm run dev:admin          # http://localhost:5173 (admin@demo.local / Demo123!)
npm run dev:citizen-web    # http://localhost:5174
```

Docker container'ları bu oturum sonunda **çalışır durumda bırakıldı** (main-db, municipality-sample-db, minio — hepsi `healthy`). `dotnet run` ile başlatılan API/Worker ve `npm run dev` ile başlatılan frontend dev server'ları ise doğrulama tamamlandıktan sonra durduruldu.
