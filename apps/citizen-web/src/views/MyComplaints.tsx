import { useEffect, useState } from 'react';
import { useAuth } from '../lib/AuthContext';
import { MyComplaint, getMyComplaints } from '../lib/auth';
import { formatDateTime, statusLabel } from '../lib/status';

type MyComplaintsProps = {
  onTrack: (trackingCode: string) => void;
  onReport: () => void;
};

type State =
  | { status: 'loading' }
  | { status: 'loaded'; items: MyComplaint[] }
  | { status: 'error'; message: string };

export function MyComplaints({ onTrack, onReport }: MyComplaintsProps) {
  const { token, user, logout } = useAuth();
  const [state, setState] = useState<State>({ status: 'loading' });

  useEffect(() => {
    if (!token) {
      return;
    }

    let cancelled = false;
    setState({ status: 'loading' });

    getMyComplaints(token)
      .then((result) => {
        if (cancelled) return;
        if (result.success && result.data) {
          setState({ status: 'loaded', items: result.data });
        } else {
          setState({ status: 'error', message: result.message ?? 'Şikayetler yüklenemedi.' });
        }
      })
      .catch(() => {
        if (!cancelled) setState({ status: 'error', message: 'Şikayetler yüklenemedi.' });
      });

    return () => {
      cancelled = true;
    };
  }, [token]);

  const items = state.status === 'loaded' ? state.items : [];
  const completedStatuses = new Set(['Resolved', 'Closed']);
  const completedCount = items.filter((item) => completedStatuses.has(item.status)).length;
  const openCount = items.length - completedCount;

  return (
    <>
      <section className="intro mine-intro">
        <div>
          <p>VATANDAŞ HESABI</p>
          <h1>Merhaba{user ? `, ${user.fullName}` : ''}.</h1>
          <span>Tüm başvurularını, güncel durumlarını ve belediye süreçlerini tek ekrandan takip et.</span>
        </div>
        <button type="button" className="mine-new-button" onClick={onReport}>Yeni bildirim oluştur <span aria-hidden="true">→</span></button>
      </section>
      <div className="track-shell mine-shell">
        <div className="mine-summary-grid" aria-label="Başvuru özeti">
          <article><span>Toplam başvuru</span><strong>{items.length}</strong><small>Tüm kayıtların</small></article>
          <article><span>Açık başvuru</span><strong>{openCount}</strong><small>İşlem devam ediyor</small></article>
          <article><span>Sonuçlanan</span><strong>{completedCount}</strong><small>Çözülen talepler</small></article>
        </div>
        <div className="report-form mine-panel">
          <div className="mine-header">
            <div>
              <h2>Başvurularım</h2>
              <p>{items.length > 0 ? `${items.length} kayıt görüntüleniyor` : 'Başvuru geçmişin'}</p>
            </div>
            <button type="button" className="secondary-button" onClick={logout}>
              Çıkış yap
            </button>
          </div>

          {state.status === 'loading' && <p className="track-empty">Yükleniyor...</p>}
          {state.status === 'error' && <p className="form-error">{state.message}</p>}

          {state.status === 'loaded' && state.items.length === 0 && (
            <div className="mine-empty">
              <p className="track-empty">Henüz bir şikayetin yok.</p>
              <button type="button" onClick={onReport}>
                İlk şikayetini oluştur
              </button>
            </div>
          )}

          {state.status === 'loaded' && state.items.length > 0 && (
            <ul className="mine-list">
              {state.items.map((item) => (
                <li key={item.trackingCode}>
                  <button type="button" className="mine-item" onClick={() => onTrack(item.trackingCode)}>
                    <span className={`mine-status-mark status-${item.status.toLowerCase()}`} aria-hidden="true" />
                    <div className="mine-item-content">
                      <div className="mine-item-top">
                        <span className="mine-item-title">{item.title || 'Başvuru'}</span>
                        <span className={`status-badge status-${item.status.toLowerCase()}`}>{statusLabel(item.status)}</span>
                      </div>
                      <div className="mine-item-meta">
                        <span>{item.municipalityName}</span>
                        <span>{item.categoryName}</span>
                        <span>{item.trackingCode}</span>
                      </div>
                      <div className="mine-item-date">Oluşturulma: {formatDateTime(item.createdAt)}</div>
                    </div>
                    <span className="mine-item-arrow" aria-hidden="true">→</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </>
  );
}
