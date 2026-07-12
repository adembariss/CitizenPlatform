import { FormEvent, useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  AdminComplaintDetail,
  ApiResponse,
  Department,
  addComplaintComment,
  assignComplaint,
  getAdminComplaintDetail,
  getDepartments,
  updateComplaintStatus
} from '../lib/api';
import {
  COMPLAINT_STATUSES,
  formatDateTime,
  priorityLabel,
  sourceLabel,
  statusClass,
  statusLabel
} from '../lib/labels';

type ActionFeedback = { kind: 'success' | 'error'; text: string } | null;

function feedbackFromResponse(response: ApiResponse<unknown>, successText: string): ActionFeedback {
  if (response.success) {
    return { kind: 'success', text: successText };
  }

  const details = response.errors.length > 0 ? ` ${response.errors.join(' ')}` : '';
  return { kind: 'error', text: `${response.message ?? 'İşlem başarısız oldu.'}${details}` };
}

export function ComplaintDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [detail, setDetail] = useState<AdminComplaintDetail | null>(null);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const reload = useCallback(async () => {
    if (!id) {
      return;
    }

    const response = await getAdminComplaintDetail(id);
    if (response.success && response.data) {
      setDetail(response.data);
      setLoadError(null);
    } else {
      setLoadError(response.message ?? 'Şikayet detayı yüklenemedi.');
    }
    setLoading(false);
  }, [id]);

  useEffect(() => {
    void reload();
  }, [reload]);

  useEffect(() => {
    let cancelled = false;

    getDepartments()
      .then((response) => {
        if (!cancelled && response.success && response.data) {
          setDepartments(response.data.filter((department) => department.isActive));
        }
      })
      .catch(() => undefined);

    return () => {
      cancelled = true;
    };
  }, []);

  if (loading) {
    return (
      <>
        <header>
          <p>
            <Link className="inline-link" to="/complaints">
              ← Şikayetler
            </Link>
          </p>
          <h1>Yükleniyor...</h1>
        </header>
      </>
    );
  }

  if (!detail) {
    return (
      <>
        <header>
          <p>
            <Link className="inline-link" to="/complaints">
              ← Şikayetler
            </Link>
          </p>
          <h1>Şikayet bulunamadı</h1>
        </header>
        {loadError && <p className="form-error">{loadError}</p>}
      </>
    );
  }

  return (
    <>
      <header>
        <p>
          <Link className="inline-link" to="/complaints">
            ← Şikayetler
          </Link>{' '}
          / {detail.trackingCode}
        </p>
        <h1>{detail.title}</h1>
        <p className="detail-subtitle">
          <span className={statusClass(detail.status)}>{statusLabel(detail.status)}</span>
          <span>Öncelik: {priorityLabel(detail.priority)}</span>
          <span>Kaynak: {sourceLabel(detail.source)}</span>
        </p>
      </header>

      {loadError && <p className="form-error">{loadError}</p>}

      <section className="detail-panel" aria-label="Şikayet bilgileri">
        <h2>Bilgiler</h2>
        <div className="detail-grid">
          <DetailField label="Belediye" value={detail.municipalityName} />
          <DetailField label="Kategori" value={detail.categoryName} />
          <DetailField label="Birim" value={detail.departmentName ?? 'Atanmadı'} />
          <DetailField label="Vatandaş" value={detail.citizenFullName ?? '-'} />
          <DetailField label="Telefon" value={detail.citizenPhoneNumber ?? '-'} />
          <DetailField label="E-posta" value={detail.citizenEmail ?? '-'} />
          <DetailField label="Adres" value={detail.addressText ?? '-'} />
          <DetailField label="Konum" value={`${detail.latitude.toFixed(5)}, ${detail.longitude.toFixed(5)}`} />
          <DetailField label="Ek dosya sayısı" value={String(detail.attachments.length)} />
          <DetailField label="Oluşturulma" value={formatDateTime(detail.createdAt)} />
          <DetailField label="Güncellenme" value={formatDateTime(detail.updatedAt)} />
          <DetailField label="Kapanma" value={formatDateTime(detail.closedAt)} />
        </div>
        <div className="detail-description">
          <span className="detail-label">Açıklama</span>
          <p>{detail.description}</p>
        </div>
      </section>

      <div className="action-grid">
        <StatusForm detail={detail} onDone={reload} />
        <AssignForm detail={detail} departments={departments} onDone={reload} />
        <CommentForm detail={detail} onDone={reload} />
      </div>

      <section className="detail-panel" aria-label="Durum geçmişi">
        <h2>Durum geçmişi ({detail.statusHistories.length})</h2>
        {detail.statusHistories.length === 0 && <p className="empty-note">Durum geçmişi yok.</p>}
        <ul className="timeline">
          {detail.statusHistories.map((entry) => (
            <li key={entry.id}>
              <div className="timeline-head">
                <span>
                  {entry.previousStatus ? `${statusLabel(entry.previousStatus)} → ` : ''}
                  <span className={statusClass(entry.newStatus)}>{statusLabel(entry.newStatus)}</span>
                </span>
                <span className="timeline-date">{formatDateTime(entry.createdAt)}</span>
              </div>
              {entry.note && <p className="timeline-note">{entry.note}</p>}
              <span className="timeline-meta">
                {entry.isVisibleToCitizen ? 'Vatandaşa görünür' : 'Sadece iç kayıt'}
              </span>
            </li>
          ))}
        </ul>
      </section>

      <section className="detail-panel" aria-label="Yorumlar">
        <h2>Yorumlar ({detail.comments.length})</h2>
        {detail.comments.length === 0 && <p className="empty-note">Henüz yorum yok.</p>}
        <ul className="timeline">
          {detail.comments.map((comment) => (
            <li key={comment.id}>
              <div className="timeline-head">
                <span>{comment.isInternal ? <span className="tag tag-internal">İç not</span> : <span className="tag">Genel</span>}</span>
                <span className="timeline-date">{formatDateTime(comment.createdAt)}</span>
              </div>
              <p className="timeline-note">{comment.body}</p>
            </li>
          ))}
        </ul>
      </section>

      <section className="detail-panel" aria-label="Atamalar">
        <h2>Atamalar ({detail.assignments.length})</h2>
        {detail.assignments.length === 0 && <p className="empty-note">Henüz atama yapılmadı.</p>}
        <ul className="timeline">
          {detail.assignments.map((assignment) => (
            <li key={assignment.id}>
              <div className="timeline-head">
                <span>{assignment.departmentName}</span>
                <span className="timeline-date">{formatDateTime(assignment.assignedAt)}</span>
              </div>
              {assignment.note && <p className="timeline-note">{assignment.note}</p>}
            </li>
          ))}
        </ul>
      </section>

      {detail.attachments.length > 0 && (
        <section className="detail-panel" aria-label="Ekler">
          <h2>Ekler ({detail.attachments.length})</h2>
          <ul className="timeline">
            {detail.attachments.map((attachment) => (
              <li key={attachment.id}>
                <div className="timeline-head">
                  <span>{attachment.originalFileName}</span>
                  <span className="timeline-date">{formatDateTime(attachment.createdAt)}</span>
                </div>
                <span className="timeline-meta">
                  {attachment.contentType} — {(attachment.sizeInBytes / 1024).toFixed(1)} KB
                </span>
              </li>
            ))}
          </ul>
        </section>
      )}
    </>
  );
}

function DetailField({ label, value }: { label: string; value: string }) {
  return (
    <div className="detail-field">
      <span className="detail-label">{label}</span>
      <span>{value}</span>
    </div>
  );
}

function StatusForm({ detail, onDone }: { detail: AdminComplaintDetail; onDone: () => Promise<void> }) {
  const [newStatus, setNewStatus] = useState('');
  const [note, setNote] = useState('');
  const [isVisibleToCitizen, setIsVisibleToCitizen] = useState(true);
  const [feedback, setFeedback] = useState<ActionFeedback>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!newStatus) {
      setFeedback({ kind: 'error', text: 'Yeni durumu seçin.' });
      return;
    }

    setSubmitting(true);
    setFeedback(null);

    const response = await updateComplaintStatus(detail.id, {
      newStatus,
      note: note.trim() || null,
      isVisibleToCitizen
    });
    setSubmitting(false);
    setFeedback(feedbackFromResponse(response, 'Durum güncellendi.'));

    if (response.success) {
      setNewStatus('');
      setNote('');
      await onDone();
    }
  }

  return (
    <form className="action-card" onSubmit={handleSubmit}>
      <h3>Durum güncelle</h3>
      <label>
        Yeni durum
        <select value={newStatus} onChange={(event) => setNewStatus(event.target.value)}>
          <option value="">Seçin...</option>
          {COMPLAINT_STATUSES.filter((status) => status !== detail.status).map((status) => (
            <option key={status} value={status}>
              {statusLabel(status)}
            </option>
          ))}
        </select>
      </label>
      <label>
        Not
        <textarea rows={2} value={note} onChange={(event) => setNote(event.target.value)} />
      </label>
      <label className="checkbox-label">
        <input
          type="checkbox"
          checked={isVisibleToCitizen}
          onChange={(event) => setIsVisibleToCitizen(event.target.checked)}
        />
        Vatandaşa görünür
      </label>
      {feedback && <p className={feedback.kind === 'error' ? 'form-error' : 'form-success'}>{feedback.text}</p>}
      <button type="submit" disabled={submitting}>
        {submitting ? 'Kaydediliyor...' : 'Durumu güncelle'}
      </button>
    </form>
  );
}

function AssignForm({
  detail,
  departments,
  onDone
}: {
  detail: AdminComplaintDetail;
  departments: Department[];
  onDone: () => Promise<void>;
}) {
  const [departmentId, setDepartmentId] = useState('');
  const [note, setNote] = useState('');
  const [feedback, setFeedback] = useState<ActionFeedback>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!departmentId) {
      setFeedback({ kind: 'error', text: 'Bir birim seçin.' });
      return;
    }

    setSubmitting(true);
    setFeedback(null);

    const response = await assignComplaint(detail.id, {
      departmentId,
      assignedUserId: null,
      note: note.trim() || null
    });
    setSubmitting(false);
    setFeedback(feedbackFromResponse(response, 'Birim ataması yapıldı.'));

    if (response.success) {
      setDepartmentId('');
      setNote('');
      await onDone();
    }
  }

  return (
    <form className="action-card" onSubmit={handleSubmit}>
      <h3>Birime ata</h3>
      <label>
        Birim
        <select value={departmentId} onChange={(event) => setDepartmentId(event.target.value)}>
          <option value="">Seçin...</option>
          {departments.map((department) => (
            <option key={department.id} value={department.id}>
              {department.name}
            </option>
          ))}
        </select>
      </label>
      <label>
        Not
        <textarea rows={2} value={note} onChange={(event) => setNote(event.target.value)} />
      </label>
      {feedback && <p className={feedback.kind === 'error' ? 'form-error' : 'form-success'}>{feedback.text}</p>}
      <button type="submit" disabled={submitting}>
        {submitting ? 'Atanıyor...' : 'Birime ata'}
      </button>
    </form>
  );
}

function CommentForm({ detail, onDone }: { detail: AdminComplaintDetail; onDone: () => Promise<void> }) {
  const [commentText, setCommentText] = useState('');
  const [isInternal, setIsInternal] = useState(false);
  const [feedback, setFeedback] = useState<ActionFeedback>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!commentText.trim()) {
      setFeedback({ kind: 'error', text: 'Yorum metni boş olamaz.' });
      return;
    }

    setSubmitting(true);
    setFeedback(null);

    const response = await addComplaintComment(detail.id, {
      commentText: commentText.trim(),
      isInternal
    });
    setSubmitting(false);
    setFeedback(feedbackFromResponse(response, 'Yorum eklendi.'));

    if (response.success) {
      setCommentText('');
      setIsInternal(false);
      await onDone();
    }
  }

  return (
    <form className="action-card" onSubmit={handleSubmit}>
      <h3>Yorum ekle</h3>
      <label>
        Yorum
        <textarea rows={3} value={commentText} onChange={(event) => setCommentText(event.target.value)} />
      </label>
      <label className="checkbox-label">
        <input type="checkbox" checked={isInternal} onChange={(event) => setIsInternal(event.target.checked)} />
        İç not (vatandaşa gösterilmez)
      </label>
      {feedback && <p className={feedback.kind === 'error' ? 'form-error' : 'form-success'}>{feedback.text}</p>}
      <button type="submit" disabled={submitting}>
        {submitting ? 'Ekleniyor...' : 'Yorum ekle'}
      </button>
    </form>
  );
}
