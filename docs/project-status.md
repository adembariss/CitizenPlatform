# CitizenPlatform Project Status

_Son güncelleme: 2026-07-12 (Aşama 9 — kalan işlerin tamamı: public tracking/kategori API'leri, admin-web ve citizen-web ekranları, MinIO, refresh token, sertleştirme, citizen-mobile'ın ilk gerçek çalıştırılması)_
_Önceki doğrulama: commit `7e264dd` (Aşama 8)_

## 1. Genel Durum

**Aşama 8 sonunda listelenen 8 öncelikli eksiğin tamamı bu aşamada kapatıldı ve her biri canlı ortamda doğrulandı.** Proje artık uçtan uca özellik-tamamlanmış durumda:

1. **Public tracking + public kategori endpoint'leri** eklendi (madde 1-2) — vatandaş takip koduyla şikayetini görebiliyor; iç notlar, `isVisibleToCitizen=false` geçmiş kayıtları, vatandaş PII'ı ve admin user id'leri **asla dönmüyor** (canlıda gizli not/iç yorum oluşturulup sızmadığı doğrulandı; unit testlerle de korunuyor).
2. **citizen-mobile ilk kez gerçekten çalıştırıldı** (madde 3) — Metro ile bundle edildi, Expo web üzerinden tarayıcıda koşturuldu: konum → belediye çözümleme → gerçek kategori listesi → gönderim → takip kodu, hepsi canlı API'ye karşı çalıştı (detay bölüm 4).
3. **admin-web tamamlandı** (madde 4) — routing, şikayet detay/durum/atama/yorum ekranları, kategori/birim yönetimi; tarayıcıda gerçek API'ye karşı uçtan uca doğrulandı.
4. **citizen-web tamamlandı** (madde 5) — Leaflet harita ile konum seçimi, fotoğraf yükleme (multipart), gerçek kategori listesi, takip ekranı; tarayıcıda doğrulandı.
5. **MinIO entegrasyonu yazıldı** (madde 6) — `ObjectStorage__Provider=Minio` ile dosyalar gerçekten MinIO'ya gidiyor (canlı doğrulandı: obje bucket'ta, DB satırı `Minio` provider ile).
6. **Refresh token akışı eklendi** (madde 7) — login'de üretim, tek kullanımlık rotasyon, logout ile iptal; token'lar DB'de sadece SHA-256 hash. Canlıda: aynı token ikinci kullanımda 401, logout sonrası 401.
7. **Production sertleştirmesi** (madde 8) — Development dışında default/kısa JWT secret ile açılış reddediliyor; rate limit artık IP başına partitioned; `SubmitComplaintCommand.cs` silindi; `.env.example`'daki hiç bind olmayan `OBJECT_STORAGE_*` isimleri `ObjectStorage__*` olarak düzeltildi.
8. **Bonus — gerçek bug bulundu ve düzeltildi**: multipart form'daki koordinatlar sunucu culture'ı ile parse ediliyordu; tr-TR host'ta `41.05` → `4105` olup validation'a takılıyordu. `Program.cs`'te invariant culture sabitlendi, canlıda doğrulandı. (JSON yolu hiç etkilenmiyordu; bug'ı iki ayrı çalışma bağımsız olarak buldu.)

## 2. Build/Test Durumu (bu aşamanın sonunda, bu makinede koşuldu)

| Komut | Sonuç |
|---|---|
| `dotnet build backend/CitizenPlatform.sln` | ✅ 0 uyarı, 0 hata |
| `dotnet test backend/CitizenPlatform.sln` | ✅ **81/81 geçti** (75 unit + 6 integration; önceki 70'e +11: tracking sızıntı önleme, public kategori filtreleme, refresh token rotasyonu) |
| `npm run build:web` (admin-web + citizen-web) | ✅ tsc + vite temiz |
| `npm --workspace @citizen-platform/citizen-mobile run typecheck` | ✅ hatasız |
| Expo web bundle (`npx expo start --web`) | ✅ Metro 262 modül bundle etti, uygulama tarayıcıda çalıştı |

### Bu aşamada canlıda doğrulanan akışlar

- `GET /api/public/municipalities/{id}/categories` → demo belediyenin 7 gerçek kategorisi (bilinmeyen/pasif belediye → 404)
- `GET /api/public/complaints/track/{code}` → durum + görünür geçmiş + public yanıtlar + attachmentCount; **gizli geçmiş notu ve iç yorum response'ta yok**; bilinmeyen kod → 404
- Refresh token: login → refresh (rotasyon) → eski token 401 → yeni token 200 → logout → 401
- MinIO: multipart fotoğraflı şikayet → obje `citizen-platform-local` bucket'ında, `complaint_attachments` satırı `Minio` provider ile
- admin-web (tarayıcı): login → dashboard → filtreli liste → detay → durum güncelle → yorum ekle → birime ata → kategori oluştur/pasifleştir/yeniden adlandır → birim listesi → logout/route koruması
- citizen-web (tarayıcı): haritadan konum seç → belediye çözümlendi → gerçek kategoriler → fotoğraflı multipart gönderim → takip kodu → takip ekranında sorgulama (bozuk kod → dostane 404)
- citizen-mobile (Expo web, tarayıcı): konum → "Demo Belediyesi" → 7 kategori chip'i → seçim → gönderim → `BLD-2026-318515` alındı ve tracking endpoint'inden çapraz doğrulandı
- Worker: bu oturumda biriken **14 pending outbox mesajının tamamı** `Completed` oldu; belediye örnek DB'sinde statüler/birimler doğru yansıdı (`ComplaintAssigned` ve `AdminCommentAdded` mesajları da bu turda gerçek ortamda işlendi — Aşama 8'de eksik kalan teyit tamamlandı)

## 3. Yeni/Değişen API Yüzeyi

- `GET /api/public/complaints/track/{trackingCode}` — sözleşme `docs/api-contract.md`'de; sızıntı kuralları `TrackComplaintQueryHandler`'da tek noktada
- `GET /api/public/municipalities/{municipalityId}/categories`
- `POST /api/auth/refresh`, `POST /api/auth/logout` — `docs/auth.md` güncellendi; `LoginResponseDto`'ya `refreshToken` + `refreshTokenExpiresAt` alanları eklendi (ek alan — mevcut client'ları bozmaz)
- Yeni rate limit policy: `public-read` (60/dk/IP); tüm policy'ler artık IP başına partitioned

## 4. citizen-mobile Doğrulama Notları

Bu ortamda Android SDK/emulator yok; doğrulama **Expo web** ile yapıldı (Metro bundler + react-native-web, gerçek API'ye karşı, geolocation mock'lanarak). Bu, kodun gerçek bir çalışma zamanında koştuğunu ilk kez kanıtladı ama **fiziksel cihaz/emulator testi hâlâ yapılmadı** — native-only davranışlar (gerçek expo-location izin akışı, Android network) hâlâ tek doğrulanmamış yüzey.

Çalıştırma sırasında bulunup düzeltilen iki monorepo sorunu (bunlar native çalıştırmayı da etkilerdi):
- `expo/AppEntry.js` hoisted olduğu için `../../App`'i repo kökünde arıyordu → lokal `index.ts` entry + `"main": "index.ts"`
- Kökte React 18.3.1 (Vite app'leri) + mobile'da 18.2.0 → çift React instance crash'i → `metro.config.js` ile modül çözümü app'in node_modules'üne sabitlendi

Cihazda test için: `npx expo start` → Expo Go ile QR; `EXPO_PUBLIC_API_BASE_URL`'i makinenin LAN IP'sine ayarla (Android emulator: `http://10.0.2.2:5080`).

## 5. Bilinen Kalan İşler / Riskler (öncelik sırasıyla)

1. **citizen-mobile fiziksel cihaz/emulator testi** — Expo web'de çalıştığı kanıtlandı ama native runtime hâlâ denenmedi.
2. **Attachment indirme/görüntüleme endpoint'i yok** — admin-web detayda sadece metadata gösteriyor; vatandaş tracking'i sadece sayı veriyor. `IFileStorageService.OpenReadAsync` hazır, sadece controller yüzeyi gerek.
3. **Mobile'da fotoğraf yükleme ve takip ekranı yok** (web'dekiyle eşitlik için).
4. **Admin kullanıcı listesi endpoint'i yok** — atamada `assignedUserId` hep null gönderiliyor (birim ataması çalışıyor).
5. Login rate limit'i (5/dk/IP) NAT arkasındaki büyük ofisler için sıkı olabilir; production'da gözden geçirilebilir.
6. Doğrulama sırasında demo DB'ye test verisi yazıldı (test şikayetleri, "Gürültü ve Çevre" adlı pasif kategori). Demo ortamı için zararsız; temiz kurulum `docker compose down -v` + bölüm 6 ile yeniden yapılabilir.

## 6. Ortamı Ayağa Kaldırma

```powershell
# 1. Servisler (main-db, municipality-sample-db, minio)
docker compose up -d

# 2. (Sadece ilk kurulumda) migration + index/seed
dotnet ef database update --project backend/src/CitizenPlatform.Infrastructure/CitizenPlatform.Infrastructure.csproj --startup-project backend/src/CitizenPlatform.Api/CitizenPlatform.Api.csproj
docker exec -i citizenplatform-main-db psql -U citizen_platform -d citizen_platform < database/main-db/002_indexes.sql
docker exec -i citizenplatform-main-db psql -U citizen_platform -d citizen_platform < database/main-db/003_seed_demo_municipality.sql

# 3. Backend (API http://localhost:5080 — launchSettings yok, URL'i env ile ver)
$env:ASPNETCORE_URLS='http://localhost:5080'; $env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project backend/src/CitizenPlatform.Api/CitizenPlatform.Api.csproj
dotnet run --project backend/src/CitizenPlatform.Worker/CitizenPlatform.Worker.csproj

# MinIO'ya geçmek için API'yi şu env'lerle başlat:
#   ObjectStorage__Provider=Minio  ObjectStorage__Endpoint=http://localhost:9000
#   ObjectStorage__BucketName=citizen-platform-local
#   ObjectStorage__AccessKey=citizenplatform  ObjectStorage__SecretKey=change-me-local

# 4. Frontend'ler
npm run dev:admin          # http://localhost:5173 (admin@demo.local / Demo123!)
npm run dev:citizen-web    # http://localhost:5174
npm --workspace @citizen-platform/citizen-mobile run web   # Expo web (test için)
```

Docker container'ları bu oturum sonunda çalışır bırakıldı; API/Worker/dev server'lar durduruldu.

## 7. Mimari Kurallara Uygunluk

- **Clean Architecture**: yeni iş mantığının tamamı Application handler'larında (`TrackComplaintQueryHandler`, `PublicCategoryListQueryHandler`, `RefreshTokenCommandHandler`, `LogoutCommandHandler`); controller'lar ince; Domain'e framework bağımlılığı girmedi. Storage doğrulama boru hattı `FileStorageServiceBase`'e çekildi, Local/MinIO sadece persistence implemente ediyor.
- **Outbox**: hiçbir yeni yazma yolu belediye DB'sine doğrudan yazmıyor; worker bu turda gerçek ortamda tekrar doğrulandı.
- **Multi-tenant**: yeni public endpoint'ler tenant-scope gerektirmiyor (tracking kod capability'si + belediye-id'li public liste); admin yüzeyi değişmedi, mevcut TenantScope testleri geçiyor.
- **Secret'lar**: repoya secret girmedi; `.env.example` placeholder; Development dışında default JWT secret fail-fast.
