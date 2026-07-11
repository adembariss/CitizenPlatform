# Complaint Flow

Bir şikayetin doğuşundan belediye yönetim panelinde işlenmesine kadar geçen uçtan uca akış.

## 1. Oluşturma (vatandaş tarafı)

1. Vatandaş `POST /api/public/complaints` (JSON veya multipart) ile şikayet gönderir.
2. `GET /api/public/municipalities/resolve` ile önceden veya `CreateComplaintCommandHandler` içinde otomatik olarak konumdan belediye (`IGeoMunicipalityResolver` → PostGIS `ST_Contains`) çözülür.
3. Kategori, o belediyede aktif mi kontrol edilir (`IComplaintCategoryRepository.GetActiveForMunicipalityAsync`).
4. `Complaint.Create(...)` ile aggregate oluşturulur, `RecordInitialStatus()` ile `New` durumunda ilk status history kaydı yazılır.
5. Kategori-birim kuralı varsa (`CategoryDepartmentRule`) complaint otomatik `RouteToDepartment` ile ilgili birime yönlendirilir.
6. Fotoğraf(lar) varsa `ComplaintAttachmentUploadService` ile güvenlik kontrolleri (content-type + dosya imzası + boyut limiti) yapılıp saklanır.
7. **Tek transaction içinde**: complaint + status history + attachment + `IntegrationOutboxMessage(MessageType="ComplaintCreated")` ana veritabanına yazılır. Direct dual-write yapılmaz.
8. Vatandaşa `trackingCode` döner.

## 2. Admin işlemleri (belediye çalışanı tarafı)

Auth (`docs/auth.md`) ile giriş yapan `MunicipalityAdmin`/`MunicipalityEmployee`/`SystemAdmin`, `docs/admin-api.md`'de tanımlı endpointler üzerinden:

- **Status güncelleme** (`PUT /api/admin/complaints/{id}/status`): `Complaint.ChangeStatus(...)` çağrılır, yeni bir `ComplaintStatusHistory` kaydı (isVisibleToCitizen flag'i ile) ve `IntegrationOutboxMessage(MessageType="ComplaintStatusChanged")` **aynı transaction** içinde yazılır.
- **Birime atama** (`PUT /api/admin/complaints/{id}/assign`): `Complaint.AssignToDepartment(...)` çağrılır (bu otomatik olarak status'u `Assigned`'a çeker), `ComplaintAssignment` kaydı ve `IntegrationOutboxMessage(MessageType="ComplaintAssigned")` aynı transaction içinde yazılır.
- **Yorum ekleme** (`POST /api/admin/complaints/{id}/comments`): `Complaint.AddComment(...)` çağrılır, `ComplaintComment` kaydı ve `IntegrationOutboxMessage(MessageType="AdminCommentAdded")` aynı transaction içinde yazılır.

Her üç işlem de aynı `IUnitOfWork.ExecuteInTransactionAsync` deseniyle korunur — ana DB yazımı ile outbox mesajı asla birbirinden kopmaz.

## 3. Outbox → Belediye DB senkronizasyonu (Worker)

`OutboxProcessorService` (Worker `BackgroundService`) periyodik olarak `Pending` outbox mesajlarını okur ve `OutboxProcessingService.DispatchAsync` mesaj tipine göre yönlendirir:

| MessageType | Worker davranışı |
|---|---|
| `ComplaintCreated` | `IMunicipalityComplaintWriter.WriteComplaintCreatedAsync` — `municipal_complaints` tablosuna idempotent insert (`ON CONFLICT (main_complaint_id) DO NOTHING`) + `municipal_complaint_status_logs` kaydı. |
| `ComplaintStatusChanged` | `WriteComplaintStatusChangedAsync` — `municipal_complaints.status` günceller, yeni bir `municipal_complaint_status_logs` kaydı ekler (`ON CONFLICT (main_complaint_id, status) DO NOTHING` ile idempotent). Karşılık gelen `municipal_complaints` kaydı henüz yoksa (ör. `ComplaintCreated` henüz senkronize olmadıysa) mesaj **başarısız sayılır ve retry edilir** — sıra dışı işlenme durumunda veri kaybı olmaz. |
| `ComplaintAssigned` | `WriteComplaintAssignedAsync` — `municipal_complaints.department_name` günceller. Karşılık gelen kayıt yoksa aynı şekilde retry edilir. |
| `AdminCommentAdded` | **Belediye örnek veritabanı şemasında yorum tablosu olmadığı için** worker bu mesaj tipini herhangi bir dış yazma yapmadan doğrudan `Completed` işaretler. Bu bilinçli bir tasarım kararıdır (iç notlar zaten belediye DB'sine senkronize edilmeyecek şekilde tasarlandı); ana DB'deki `ComplaintComment` kaydı ve audit amaçlı outbox mesajı yine de oluşturulur. |

Hata durumunda (`Result.Failure`): `IntegrationAttempt` başarısız kapatılır, `AttemptCount >= MaxRetryCount` değilse exponential backoff ile `NextRetryAt` atanıp mesaj `Pending`'e döner; aşılırsa `Failed` olur ve `FailureReason` saklanır. Worker hatası API cevabını hiçbir zaman etkilemez — vatandaşın veya admin'in işlemi ana DB'de kalıcıdır.

## 4. Kalan adımlar (henüz yok)

- `GET /api/public/complaints/track/{trackingCode}` — vatandaşın kendi şikayetini takip etmesi (iç notlar ve `isVisibleToCitizen=false` geçmiş sızdırılmayacak şekilde).
- Bildirim gönderimi (`Notification` entity zaten var, gönderim mekanizması yok).
