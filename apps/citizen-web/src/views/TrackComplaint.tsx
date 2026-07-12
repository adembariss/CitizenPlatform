import { FormEvent, useEffect, useState } from 'react';
import { trackComplaint, TrackedComplaint } from '../lib/api';
import { formatDateTime, statusLabel } from '../lib/status';

type TrackState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'loaded'; complaint: TrackedComplaint }
  | { status: 'not-found' }
  | { status: 'error'; message: string };

type TrackComplaintProps = {
  initialCode?: string;
};

export function TrackComplaint({ initialCode }: TrackComplaintProps) {
  const [code, setCode] = useState(initialCode ?? '');
  const [track, setTrack] = useState<TrackState>({ status: 'idle' });

  async function query(trackingCode: string) {
    const trimmed = trackingCode.trim();
    if (!trimmed) {
      setTrack({ status: 'error', message: 'Lütfen takip kodunuzu girin.' });
      return;
    }

    setTrack({ status: 'loading' });

    try {
      const result = await trackComplaint(trimmed);

      if (result.success && result.data) {
        setTrack({ status: 'loaded', complaint: result.data });
        return;
      }

      setTrack({ status: 'not-found' });
    } catch {
      setTrack({ status: 'error', message: 'Sorgulama sırasında bir hata oluştu. Lütfen tekrar deneyin.' });
    }
  }

  useEffect(() => {
    if (initialCode) {
      void query(initialCode);
    }
    // Only auto-query the code handed over from a fresh submission.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialCode]);

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    void query(code);
  }

  return (
    <>
      <section className="intro">
        <p>Şikayet sorgulama ekranı</p>
        <h1>Takip kodunla başvurunun durumunu öğren.</h1>
      </section>
      <div className="report-form">
        <form className="track-form" onSubmit={handleSubmit}>
          <label>
            Takip kodu
            <input
              value={code}
              onChange={(event) => setCode(event.target.value)}
              placeholder="Örn. BLD-2026-A8F21C"
              autoComplete="off"
            />
          </label>
          <button type="submit" disabled={track.status === 'loading'}>
            {track.status === 'loading' ? 'Sorgulanıyor...' : 'Sorgula'}
          </button>
        </form>

        {track.status === 'not-found' && (
          <p className="form-error">Bu takip koduna ait bir başvuru bulunamadı. Kodu kontrol edip tekrar deneyin.</p>
        )}
        {track.status === 'error' && <p className="form-error">{track.message}</p>}

        {track.status === 'loaded' && <ComplaintDetails complaint={track.complaint} />}
      </div>
    </>
  );
}

function ComplaintDetails({ complaint }: { complaint: TrackedComplaint }) {
  return (
    <div className="track-result">
      <div className="track-header">
        <h2>{complaint.title || 'Başvuru'}</h2>
        <span className={`status-badge status-${complaint.status.toLowerCase()}`}>{statusLabel(complaint.status)}</span>
      </div>

      <dl className="track-meta">
        <div>
          <dt>Takip kodu</dt>
          <dd>{complaint.trackingCode}</dd>
        </div>
        <div>
          <dt>Belediye</dt>
          <dd>{complaint.municipalityName}</dd>
        </div>
        <div>
          <dt>Kategori</dt>
          <dd>{complaint.categoryName}</dd>
        </div>
        <div>
          <dt>Sorumlu birim</dt>
          <dd>{complaint.departmentName ?? 'Henüz atanmadı'}</dd>
        </div>
        <div>
          <dt>Oluşturulma</dt>
          <dd>{formatDateTime(complaint.createdAt)}</dd>
        </div>
        <div>
          <dt>Son güncelleme</dt>
          <dd>{complaint.updatedAt ? formatDateTime(complaint.updatedAt) : '-'}</dd>
        </div>
        {complaint.closedAt && (
          <div>
            <dt>Kapatılma</dt>
            <dd>{formatDateTime(complaint.closedAt)}</dd>
          </div>
        )}
        <div>
          <dt>Fotoğraf sayısı</dt>
          <dd>{complaint.attachmentCount}</dd>
        </div>
      </dl>

      <div className="track-section">
        <h3>Açıklama</h3>
        <p>{complaint.description}</p>
        {complaint.addressText && <p className="track-address">Adres: {complaint.addressText}</p>}
      </div>

      <div className="track-section">
        <h3>Durum geçmişi</h3>
        <ol className="timeline">
          {complaint.statusHistory.map((entry, index) => (
            <li key={`${entry.createdAt}-${index}`}>
              <span className="timeline-status">
                {entry.previousStatus ? `${statusLabel(entry.previousStatus)} → ` : ''}
                {statusLabel(entry.newStatus)}
              </span>
              <span className="timeline-date">{formatDateTime(entry.createdAt)}</span>
              {entry.note && <span className="timeline-note">{entry.note}</span>}
            </li>
          ))}
        </ol>
      </div>

      <div className="track-section">
        <h3>Belediye yanıtları</h3>
        {complaint.responses.length === 0 ? (
          <p className="track-empty">Henüz bir yanıt bulunmuyor.</p>
        ) : (
          <ul className="responses">
            {complaint.responses.map((response, index) => (
              <li key={`${response.createdAt}-${index}`}>
                <p>{response.body}</p>
                <span className="timeline-date">{formatDateTime(response.createdAt)}</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
