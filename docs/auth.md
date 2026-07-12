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
    "refreshToken": "96-hex-karakterlik-tek-kullanimlik-token",
    "refreshTokenExpiresAt": "2026-07-26T12:00:00Z",
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

### `POST /api/auth/refresh`

```json
{ "refreshToken": "..." }
```

Refresh token'ı doğrular ve **tek kullanımlık rotasyon** uygular: eski token anında iptal edilir
(`RevokedAt` + `ReplacedByTokenId`), yeni bir access token + yeni bir refresh token döner
(`LoginResponseDto` ile aynı şekil). Bilinmeyen, süresi dolmuş, iptal edilmiş veya daha önce
kullanılmış token her zaman `401` (`Invalid or expired refresh token.`) döner — canlı ortamda
doğrulandı: aynı token ikinci kez kullanılınca 401. Kullanıcı pasifleştirilmişse rotasyonla
üretilen yeni token da anında iptal edilir.

Token'lar veritabanında **sadece SHA-256 hash olarak** saklanır (`refresh_tokens.TokenHash`);
düz token yalnızca response'ta bir kez görünür.

### `POST /api/auth/logout`

```json
{ "refreshToken": "..." }
```

Verilen refresh token'ı iptal eder; her zaman `200` döner (token bilinmiyorsa sessizce yok sayılır).
Access token'lar stateless olduğu için süreleri dolana kadar geçerli kalır — bu yüzden access token
ömrü kısa tutulmalıdır.

## Token içeriği

Access token'ın claim'leri:

- `sub` / `ClaimTypes.NameIdentifier`: kullanıcı id'si (Guid)
- `email`
- `ClaimTypes.Name`: display name
- `user_type`: `SystemAdmin` | `MunicipalityAdmin` | `MunicipalityEmployee` | `Citizen`
- `municipality_id`: sadece belediye bazlı kullanıcılarda mevcut (SystemAdmin'de yok)
- `ClaimTypes.Role`: kullanıcının aktif rollerinin her biri için bir claim (örn. `MunicipalityAdmin`)

Refresh token akışı yukarıda anlatıldığı gibi aktif: login'de üretilir, `/api/auth/refresh` ile
tek kullanımlık rotasyonla yenilenir, `/api/auth/logout` ile iptal edilir.

## Konfigürasyon

Ortam değişkenleri (`.env` / `JWT__*`):

| Değişken | Açıklama | Development varsayılanı |
|---|---|---|
| `JWT__ISSUER` | Token issuer | `citizen-platform` |
| `JWT__AUDIENCE` | Token audience | `citizen-platform` |
| `JWT__SECRET` | HMAC-SHA256 imzalama anahtarı | `change-me-local-development-secret-please-replace` |
| `JWT__ACCESS_TOKEN_MINUTES` | Access token ömrü (dakika) | `60` |
| `JWT__REFRESH_TOKEN_DAYS` | Refresh token ömrü (gün) | `14` |

**`JWT__SECRET` production'da mutlaka gerçek bir secret ile değiştirilmeli.** `.env.example` içindeki değer sadece local development placeholder'ıdır, repoya gerçek secret asla yazılmaz. Ayrıca API, `Development` dışındaki ortamlarda default secret veya 32 karakterden kısa bir secret ile **açılmayı reddeder** (`JwtOptions.EnsureProductionSecret`, `Program.cs`'te fail-fast).

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
