# Admin API

Belediye yönetim paneli için backend API'leri. Tüm endpointler `Authorization: Bearer <accessToken>` gerektirir (bkz. `docs/auth.md`) ve `RequireAdminAccess` policy'si ile korunur (write/create/update endpointlerinde `RequireMunicipalityAdmin` gerekir — bkz. tablo).

## Multi-tenant izolasyon

Her admin endpoint'i `TenantScope` (`Application/Common/TenantScope.cs`) üzerinden merkezi olarak scope'lanır:

- **SystemAdmin**: tüm belediyeleri görebilir. `municipalityId` query parametresi ile filtreleyebilir; vermezse tüm belediyeler döner.
- **MunicipalityAdmin / MunicipalityEmployee**: her zaman sadece token'daki `municipalityId`. Request'te farklı bir `municipalityId` gönderilse bile **yok sayılır** (ignore edilir, hata dönmez — `ResolveListFilter` request değerini SystemAdmin dışında hiç okumaz).
- Başka belediyeye ait bir complaint id'si detay/status/assign/comment endpointlerinden istenirse **404 Not Found** döner (403 yerine — kayıt varlığının sızdırılmaması için).

Bu davranış `backend/tests/CitizenPlatform.UnitTests/TenantScopeTests.cs`, `AdminComplaintListQueryHandlerTests.cs`, `AdminComplaintDetailQueryHandlerTests.cs`, `AssignComplaintCommandHandlerTests.cs` içinde test edilir; policy hiyerarşisi `backend/tests/CitizenPlatform.IntegrationTests/AdminAuthorizationPolicyTests.cs` içinde gerçek `IAuthorizationService` ile test edilir.

## Complaint yönetimi

| Endpoint | Policy |
|---|---|
| `GET /api/admin/complaints` | `RequireAdminAccess` |
| `GET /api/admin/complaints/{id}` | `RequireAdminAccess` |
| `GET /api/admin/complaints/{id}/history` | `RequireAdminAccess` |
| `PUT /api/admin/complaints/{id}/status` | `RequireAdminAccess` |
| `PUT /api/admin/complaints/{id}/assign` | `RequireAdminAccess` |
| `POST /api/admin/complaints/{id}/comments` | `RequireAdminAccess` |

### `GET /api/admin/complaints`

Query: `status`, `categoryId`, `departmentId`, `dateFrom`, `dateTo`, `search`, `page` (varsayılan 1), `pageSize` (varsayılan 20, azami 100), `municipalityId` (sadece SystemAdmin). `search` tracking code, title, description ve adres üzerinde çalışır. N+1 sorgu yapılmaz (`AdminComplaintQueryRepository.SearchAsync` tek bir join'li sorgu + sayfalama). `citizenFullName` maskelenmiş döner (örn. `"A*** L***"`).

```json
{
  "items": [{ "id": "...", "trackingCode": "BLD-2026-ABC123", "citizenFullName": "A*** B***", "...": "..." }],
  "page": 1,
  "pageSize": 20,
  "totalCount": 100
}
```

### `GET /api/admin/complaints/{id}`

Tam detay: complaint alanları + `citizenFullName`/`citizenPhoneNumber`/`citizenEmail` (maskelenmemiş — yetkili personel için), attachments, statusHistories, comments (iç notlar dahil), assignments. Tenant dışı istekte `404`.

### `GET /api/admin/complaints/{id}/history`

Sadece statusHistories + comments + assignments + created/updated bilgisi (public tracking bunu **kullanmaz**, iç notlar burada görünür).

### `PUT /api/admin/complaints/{id}/status`

```json
{ "newStatus": "UnderReview", "note": "Talep incelemeye alindi.", "isVisibleToCitizen": true }
```

- `Complaint.ChangeStatus(...)` domain metodu kullanılır.
- Aynı status'a tekrar geçiş **400 Bad Request** döner (`"Complaint is already in the requested status."`) — no-op yerine net bir hata tercih edildi.
- `Resolved`/`Closed` durumuna geçişte `closedAt` domain içinde otomatik set edilir; başka bir statüye geçilirse `closedAt` temizlenir.
- Ana DB'ye complaint + status history + outbox mesajı (`ComplaintStatusChanged`) **tek transaction** içinde yazılır (`IUnitOfWork.ExecuteInTransactionAsync`).

### `PUT /api/admin/complaints/{id}/assign`

```json
{ "departmentId": "...", "assignedUserId": null, "note": "Fen Isleri birimine yonlendirildi." }
```

- Department, complaint ile **aynı belediyeye** ait olmalı; değilse `400` (`"Department does not belong to this municipality."`) — başka belediyenin birimine atama yapılamaz.
- `Complaint.AssignToDepartment(...)` domain metodu kullanılır (otomatik olarak status `Assigned`'a geçer, zaten `Assigned` ise sadece `Touch()`).
- Outbox mesajı: `ComplaintAssigned`.

### `POST /api/admin/complaints/{id}/comments`

```json
{ "commentText": "Ekip yonlendirildi.", "isInternal": false }
```

- `Complaint.AddComment(...)` domain metodu kullanılır.
- `isInternal=true` ise public tracking'de **asla görünmeyecek** (public tracking endpoint'i henüz yok — bkz. `docs/project-status.md` kalan işler).
- Outbox mesajı: `AdminCommentAdded` — ancak belediye sample DB şemasında yorum tablosu olmadığı için worker bu mesajı harici bir yazma yapmadan `Completed` işaretler (bkz. `docs/complaint-flow.md`).

## Dashboard

| Endpoint | Policy |
|---|---|
| `GET /api/admin/dashboard/summary` | `RequireAdminAccess` |

Query: `municipalityId` (sadece SystemAdmin için anlamlı). Response: `totalComplaints`, `openComplaints`, `todayComplaints`, `resolvedComplaints`, `closedComplaints`, `averageResolutionHours`, `byStatus[]`, `byCategory[]`, `byDepartment[]`. Tenant scope aynı şekilde uygulanır.

## Kategori ve Birim yönetimi

| Endpoint | Policy |
|---|---|
| `GET /api/admin/categories` | `RequireAdminAccess` |
| `POST /api/admin/categories` | `RequireMunicipalityAdmin` |
| `PUT /api/admin/categories/{id}` | `RequireMunicipalityAdmin` |
| `GET /api/admin/departments` | `RequireAdminAccess` |
| `POST /api/admin/departments` | `RequireMunicipalityAdmin` |
| `PUT /api/admin/departments/{id}` | `RequireMunicipalityAdmin` |

- `MunicipalityEmployee` sadece listeleyebilir (GET), oluşturma/güncelleme yapamaz.
- `MunicipalityAdmin` sadece kendi belediyesinde yönetebilir; `SystemAdmin` her belediyede (kategori için global/`municipalityId=null` kategoriler de dahil, sadece `SystemAdmin` düzenleyebilir).
- Silme yerine `isActive=false` (soft toggle) kullanılır — `PUT` ile `isActive` alanı gönderilir.
- Kategori/birim değişiklikleri outbox event üretmez; bunlar ana platformun lokal taxonomy/konfigürasyon verisidir, belediye sample DB'sine senkronize edilmez.

## Yetki hataları

- Token yok veya geçersiz → `401 Unauthorized` (JWT bearer middleware).
- Token geçerli ama rol yetersiz (örn. `Citizen` admin endpoint'ine istek atarsa) → `403 Forbidden` (ASP.NET Core authorization middleware).
- Complaint/department/category bulunamadı veya tenant dışı → `404 Not Found`.
