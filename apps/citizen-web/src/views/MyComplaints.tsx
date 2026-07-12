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

  return (
    <>
      <section className="intro">
        <p>Hesabım</p>
        <h1>Merhaba{user ? `, ${user.fullName}` : ''}.</h1>
      </section>
      <div className="track-shell">
        <div className="report-form">
          <div className="mine-header">
            <h2>Şikayetlerim</h2>
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
                    <div className="mine-item-top">
                      <span className="mine-item-title">{item.title || 'Başvuru'}</span>
                      <span className={`status-badge status-${item.status.toLowerCase()}`}>{statusLabel(item.status)}</span>
                    </div>
                    <div className="mine-item-meta">
                      <span>{item.municipalityName}</span>
                      <span>·</span>
                      <span>{item.categoryName}</span>
                      <span>·</span>
                      <span>{item.trackingCode}</span>
                    </div>
                    <div className="mine-item-date">{formatDateTime(item.createdAt)}</div>
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
