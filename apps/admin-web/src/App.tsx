import { FormEvent, useEffect, useState } from 'react';
import {
  AdminComplaintListItem,
  CurrentUser,
  DashboardSummary,
  clearSession,
  getAdminComplaints,
  getDashboardSummary,
  getStoredToken,
  getStoredUser,
  login,
  storeSession
} from './lib/api';

export function App() {
  const [user, setUser] = useState<CurrentUser | null>(() => (getStoredToken() ? getStoredUser() : null));

  if (!user) {
    return <LoginScreen onLoggedIn={setUser} />;
  }

  return <Dashboard user={user} onLogout={() => { clearSession(); setUser(null); }} />;
}

function LoginScreen({ onLoggedIn }: { onLoggedIn: (user: CurrentUser) => void }) {
  const [email, setEmail] = useState('admin@demo.local');
  const [password, setPassword] = useState('Demo123!');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    const result = await login(email, password);
    setSubmitting(false);

    if (!result.success || !result.data) {
      setError(result.message ?? 'Giriş yapılamadı.');
      return;
    }

    storeSession(result.data);
    onLoggedIn(result.data.user);
  }

  return (
    <main className="login-shell">
      <form className="login-card" onSubmit={handleSubmit}>
        <strong>CitizenPlatform</strong>
        <h1>Belediye yönetim paneli</h1>
        <label>
          E-posta
          <input type="email" required value={email} onChange={(event) => setEmail(event.target.value)} />
        </label>
        <label>
          Şifre
          <input type="password" required value={password} onChange={(event) => setPassword(event.target.value)} />
        </label>
        {error && <p className="form-error">{error}</p>}
        <button type="submit" disabled={submitting}>
          {submitting ? 'Giriş yapılıyor...' : 'Giriş yap'}
        </button>
        <p className="login-hint">Demo: admin@demo.local / Demo123!</p>
      </form>
    </main>
  );
}

function Dashboard({ user, onLogout }: { user: CurrentUser; onLogout: () => void }) {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [complaints, setComplaints] = useState<AdminComplaintListItem[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const [summaryResponse, complaintsResponse] = await Promise.all([getDashboardSummary(), getAdminComplaints()]);

        if (cancelled) {
          return;
        }

        if (summaryResponse.success && summaryResponse.data) {
          setSummary(summaryResponse.data);
        }

        setComplaints(complaintsResponse.items ?? []);
      } catch {
        if (!cancelled) {
          setLoadError('Veriler yüklenemedi. API çalışıyor mu kontrol edin.');
        }
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  const metrics = summary
    ? [
        { label: 'Toplam bildirim', value: summary.totalComplaints },
        { label: 'Açık bildirim', value: summary.openComplaints },
        { label: 'Bugün gelen', value: summary.todayComplaints },
        { label: 'Çözülen', value: summary.resolvedComplaints }
      ]
    : [];

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <strong>CitizenPlatform</strong>
        <nav>
          <a href="/">Panel</a>
          <a href="/">Bildirimler</a>
          <a href="/">Harita</a>
          <a href="/">Birimler</a>
        </nav>
        <div className="sidebar-user">
          <span>{user.fullName}</span>
          <span className="sidebar-role">{user.municipalityName ?? user.userType}</span>
          <button type="button" onClick={onLogout}>
            Çıkış yap
          </button>
        </div>
      </aside>
      <section className="content">
        <header>
          <p>Belediye yönetim paneli</p>
          <h1>Vatandaş bildirimleri</h1>
        </header>

        {loadError && <p className="form-error">{loadError}</p>}

        <div className="metric-grid">
          {metrics.map((metric) => (
            <article key={metric.label} className="metric-card">
              <span>{metric.label}</span>
              <strong>{metric.value}</strong>
            </article>
          ))}
        </div>

        <section className="table-panel" aria-label="Son bildirimler">
          <div className="table-row table-head">
            <span>Takip Kodu</span>
            <span>Konu</span>
            <span>Kategori</span>
            <span>Durum</span>
          </div>
          {complaints.length === 0 && <div className="table-row">Henüz bildirim yok.</div>}
          {complaints.map((complaint) => (
            <div className="table-row" key={complaint.id}>
              <span>{complaint.trackingCode}</span>
              <span>{complaint.title}</span>
              <span>{complaint.categoryName}</span>
              <span>{complaint.status}</span>
            </div>
          ))}
        </section>
      </section>
    </main>
  );
}
