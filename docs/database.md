# Veritabanı: Konum, Bağlantı ve Şema

## 1. Nerede çalışıyor?

Veritabanı **Docker container** içinde çalışan **PostgreSQL 16 + PostGIS** (mekânsal
uzantı) örneğidir. `docker-compose.yml` iki ayrı Postgres + bir MinIO başlatır:

| Amaç | Container | Host:Port | Veritabanı | Kullanıcı / Parola |
|---|---|---|---|---|
| **Ana platform DB** | `citizenplatform-main-db` | `localhost:5432` | `citizen_platform` | `citizen_platform` / `change-me-local` |
| **Örnek belediye (dış) DB** | `citizenplatform-municipality-sample-db` | `localhost:5433` | `municipality_sample` | `municipality_sample` / `change-me-local` |
| **Obje deposu (fotoğraflar)** | `citizenplatform-minio` | `localhost:9000` (API) · `:9001` (konsol) | — | `citizenplatform` / `change-me-local` |

- **Ana DB** her şeyi tutar (belediyeler, şikayetler, kullanıcılar, outbox…).
- **Örnek belediye DB**, outbox senkronunun yazdığı harici belediye sistemini temsil eder.
- **MinIO**, `ObjectStorage:Provider=Minio` iken şikayet fotoğraflarını saklar (varsayılan: yerel disk).
- Veriler Docker **volume**'lerinde kalıcıdır (`docker compose down` verini silmez; `-v` siler).

## 2. Nasıl bağlanırım / tabloları nasıl görürüm?

**Terminalden (psql):**
```powershell
docker exec -it citizenplatform-main-db psql -U citizen_platform -d citizen_platform
```
Sonra: `\dt` (tablolar) · `\d complaints` (bir tablonun kolonları) · `SELECT * FROM municipalities LIMIT 5;` · `\q` (çıkış).

**Görsel araç (önerilen):** **DBeaver** veya **pgAdmin** → yeni PostgreSQL bağlantısı:
Host `localhost`, Port `5432`, Database `citizen_platform`, User `citizen_platform`, Parola `change-me-local`.

## 3. Tablo kataloğu (public şeması)

**Ortak (Auditable) kolonlar:** neredeyse her tabloda `id` (uuid, PK), `created_at`,
`updated_at`, `deleted_at`, `is_deleted` (soft-delete) bulunur. Aşağıda bu kolonlar
tekrar edilmez; yalnızca alana özgü kolonlar listelenir.

### Belediye / dizin
- **`municipalities`** — belediyeler. `name`, `code` (benzersiz), `is_active`, **`province`** (il, adres seçici gruplaması), **`center_latitude`/`center_longitude`** (merkez, adresten seçimde konum).
- **`municipality_boundaries`** — belediye sınır poligonları. `municipality_id`→municipalities, `name`, **`boundary_geometry`** (PostGIS `geometry(MultiPolygon,4326)`, GiST index), `is_active`. Konum→belediye çözümü burada `ST_Contains` ile yapılır.
- **`complaint_categories`** — şikayet kategorileri. `municipality_id` (NULL=global), `name`, `code`, `is_active`.
- **`departments`** — belediye birimleri. `municipality_id`, `name`, `code`, `is_active`.
- **`category_department_rules`** — kategori→birim yönlendirme kuralı. `municipality_id`, `category_id`, `department_id`, `default_priority`.
- **`municipality_database_connections`** — belediyenin dış DB bağlantısı (outbox hedefi). `provider`, `connection_name`, `encrypted_connection_string`.

### Şikayet (çekirdek)
- **`complaints`** — şikayetler. `municipality_id`, `category_id`, `citizen_id` (NULL=anonim), `tracking_code` (benzersiz), `title`, `description`, `latitude`/`longitude` + `location_geometry` (PostGIS point), `status`, `priority`, `source`, `current_department_id`, `assigned_user_id`, `address_text`, `closed_at`, dış senkron alanları (`external_municipality_*`, `synced_at`).
- **`complaint_attachments`** — fotoğraflar. `complaint_id`, `file_name`, `original_file_name`, `content_type`, `size_in_bytes`, `storage_provider`, `object_key`, `sha256_hash`, EXIF konum/tarih.
- **`complaint_status_histories`** — durum geçmişi. `previous_status`, `new_status`, `changed_by_user_id`, `note`, **`is_visible_to_citizen`** (public takipte bu filtrelenir).
- **`complaint_comments`** — yorumlar. `author_user_id`, `body`, **`is_internal`** (public takipte gizlenir).
- **`complaint_assignments`** — birim atama geçmişi. `department_id`, `assigned_by_user_id`, `assigned_user_id`, `note`.

### Kimlik / yetki
- **`users`** — giriş yapan kullanıcılar. `email` (benzersiz-aktif), `display_name`, **`user_type`** (`SystemAdmin`/`MunicipalityAdmin`/`MunicipalityEmployee`/`Citizen`), `password_hash`, `is_active`.
- **`roles`** — roller. `name`, `key`, `is_system_role`.
- **`user_roles`** — kullanıcı-rol ataması. `user_id`, `role_id`, **`municipality_id`** (personelin belediyesi), `revoked_at`.
- **`refresh_tokens`** — yenileme token'ları (yalnızca SHA-256 `token_hash`), `expires_at`, `revoked_at`, `replaced_by_token_id`.
- **`citizens`** — vatandaş profili. `user_id` (NULL=anonim başvuru), `full_name`, `phone_number`, `email`.

### Entegrasyon / diğer
- **`integration_outbox`** — outbox kuyruğu (belediye dış DB'sine senkron). `municipality_id`, `aggregate_id`, `message_type`, `payload` (jsonb), `status`, `attempt_count`, `next_retry_at`.
- **`integration_attempts`** — outbox deneme kayıtları.
- **`audit_logs`**, **`notifications`** — denetim ve bildirim kayıtları.
- **`__EFMigrationsHistory`** — EF Core migration geçmişi. `spatial_ref_sys`, `geometry_columns`, `geography_columns` — PostGIS sistem tabloları (dokunma).

## 4. İlişkiler (özet)

```
municipalities 1─┬─* municipality_boundaries      (konum→belediye: ST_Contains)
                 ├─* complaint_categories
                 ├─* departments
                 ├─* category_department_rules
                 └─* municipality_database_connections

complaints *─1 municipalities
complaints *─1 complaint_categories
complaints *─0..1 citizens                        (anonimse NULL)
complaints 1─┬─* complaint_attachments
             ├─* complaint_status_histories
             ├─* complaint_comments
             └─* complaint_assignments

users 1─* user_roles *─1 roles                    (user_roles.municipality_id = personelin belediyesi)
users 1─0..1 citizens                             (üye vatandaş)
users 1─* refresh_tokens

complaints 1─* integration_outbox                 (worker → belediye dış DB)
```

## 5. Konum→belediye çözümü nasıl çalışır?

`municipality_boundaries.boundary_geometry` üzerinde GiST indexli bir sorgu:
```sql
SELECT m.* FROM municipality_boundaries b
JOIN municipalities m ON m.id = b.municipality_id
WHERE b.is_active AND ST_Contains(b.boundary_geometry, ST_SetSRID(ST_MakePoint(:lng,:lat),4326));
```
İstanbul ilçeleri için sınırlar **geoBoundaries ADM2** (gerçek OSM türevi poligonlar);
diğer bazı belediyeler için yaklaşık kutular (bkz. `database/main-db/00X_*.sql` seed'leri).
Adres seçici (İl→İlçe) ise koordinat çözümüne hiç gerek kalmadan belediyeyi doğrudan seçer.
