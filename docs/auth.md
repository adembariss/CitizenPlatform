# Auth

CitizenPlatform admin tarafı JWT bearer token ile korunur. Vatandaş public endpointleri (`/api/public/*`) auth gerektirmez.

## Endpointler

### `POST /api/auth/login`

```http
POST /api/auth/login
Content-Type: application/json
```

```json
{
  "email": "admin@demo.local",
  "password": "Demo123!"
}
```

Başarılı cevap (`200 OK`):

```json
{
  "success": true,
  "data": {
    "accessToken": "...",
    "expiresAt": "2026-07-12T13:00:00Z",
    "user": {
      "id": "...",
      "fullName": "Demo Belediye Yoneticisi",
      "email": "admin@demo.local",
      "userType": "MunicipalityAdmin",
      "municipalityId": "11111111-1111-1111-1111-111111111111",
      "municipalityName": "Demo Belediyesi",
      "roles": ["MunicipalityAdmin"]
    }
  },
  "message": null,
  "errors": []
}
```

Hatalı email/şifre kombinasyonu her zaman aynı genel mesajla `401 Unauthorized` döner (`Invalid email or password.`) — hangi alanın yanlış olduğu (email bulunamadı mı, şifre mi yanlış) client'a sızdırılmaz. Login endpoint'i `auth-login` rate limit policy'si ile korunur (varsayılan: dakikada 5 istek/IP).

### `GET /api/auth/me`

```http
GET /api/auth/me
Authorization: Bearer <accessToken>
```

Token geçerli değilse veya yoksa `401 Unauthorized` döner. Geçerliyse `LoginResponseDto.user` ile aynı şekle sahip `CurrentUserDto` döner.

## Token içeriği

Access token'ın claim'leri:

- `sub` / `ClaimTypes.NameIdentifier`: kullanıcı id'si (Guid)
- `email`
- `ClaimTypes.Name`: display name
- `user_type`: `SystemAdmin` | `MunicipalityAdmin` | `MunicipalityEmployee` | `Citizen`
- `municipality_id`: sadece belediye bazlı kullanıcılarda mevcut (SystemAdmin'de yok)
- `ClaimTypes.Role`: kullanıcının aktif rollerinin her biri için bir claim (örn. `MunicipalityAdmin`)

Refresh token bu fazda yok; sadece access token var. `RefreshToken` domain entity'si ileride eklenecek akış için zaten hazır durumda, sadece kullanılmıyor.

## Konfigürasyon

Ortam değişkenleri (`.env` / `JWT__*`):

| Değişken | Açıklama | Development varsayılanı |
|---|---|---|
| `JWT__ISSUER` | Token issuer | `citizen-platform` |
| `JWT__AUDIENCE` | Token audience | `citizen-platform` |
| `JWT__SECRET` | HMAC-SHA256 imzalama anahtarı | `change-me-local-development-secret-please-replace` |
| `JWT__ACCESS_TOKEN_MINUTES` | Access token ömrü (dakika) | `60` |

**`JWT__SECRET` production'da mutlaka gerçek bir secret ile değiştirilmeli.** `.env.example` içindeki değer sadece local development placeholder'ıdır, repoya gerçek secret asla yazılmaz.

## Password hashing

Şifreler PBKDF2-HMACSHA256 (100.000 iterasyon, 16 byte salt, 32 byte hash) ile `PasswordHasher` (`Infrastructure/Identity/PasswordHasher.cs`) üzerinden hashlenir. Format: `{iterasyon}.{saltBase64}.{hashBase64}`. Düz metin şifre hiçbir zaman veritabanına yazılmaz.

## Authorization policy'leri

| Policy | İzin verilen roller |
|---|---|
| `RequireSystemAdmin` | `SystemAdmin` |
| `RequireMunicipalityAdmin` | `SystemAdmin`, `MunicipalityAdmin` |
| `RequireMunicipalityEmployee` | `SystemAdmin`, `MunicipalityAdmin`, `MunicipalityEmployee` |
| `RequireAdminAccess` | `SystemAdmin`, `MunicipalityAdmin`, `MunicipalityEmployee` |

Roller hiyerarşiktir: `SystemAdmin` her policy'yi geçer, `MunicipalityAdmin` kendi ve altındaki (`RequireAdminAccess`) policy'leri geçer. `Citizen` rolü hiçbir admin policy'sini geçemez. Politika tanımları `CitizenPlatform.Api/Authorization/AuthorizationPolicySetup.cs` içinde tek yerden yönetilir; hem gerçek uygulama başlangıcı hem de testler aynı tanımı kullanır (`backend/tests/CitizenPlatform.IntegrationTests/AdminAuthorizationPolicyTests.cs`).

## Development demo kullanıcılar

**Sadece `ASPNETCORE_ENVIRONMENT=Development` ortamında**, API açılışında `DevelopmentDataSeeder` (`Infrastructure/Seeding/DevelopmentDataSeeder.cs`) çalışır ve `database/main-db/003_seed_demo_municipality.sql` ile oluşturulan Demo Belediyesi (`code=DEMO`) mevcutsa şu demo kullanıcıları (yoksa) oluşturur:

| Email | Şifre | UserType | Belediye |
|---|---|---|---|
| `systemadmin@demo.local` | `Demo123!` | `SystemAdmin` | — |
| `admin@demo.local` | `Demo123!` | `MunicipalityAdmin` | Demo Belediyesi |
| `employee@demo.local` | `Demo123!` | `MunicipalityEmployee` | Demo Belediyesi |

**`Demo123!` sadece development seed şifresidir, hiçbir ortamda gerçek/production şifre olarak kullanılmamalıdır.** Demo belediye seed SQL'i çalışmamışsa (`database/main-db/003_seed_demo_municipality.sql`), seeder bunu tespit edip bir uyarı loglar ve kullanıcı oluşturmadan devam eder; API açılışını asla bloklamaz veya çökertmez. Production ortamında (`IsDevelopment() == false`) bu seed hiçbir zaman çalışmaz.
