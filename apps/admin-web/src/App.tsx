import { FormEvent, useState } from 'react';
import { BrowserRouter, Navigate, NavLink, Outlet, Route, Routes, useNavigate } from 'react-router-dom';
import { clearSession, getStoredToken, getStoredUser, login, storeSession } from './lib/api';
import { DashboardPage } from './pages/DashboardPage';
import { ComplaintsPage } from './pages/ComplaintsPage';
import { ComplaintDetailPage } from './pages/ComplaintDetailPage';
import { CategoriesPage } from './pages/CategoriesPage';
import { DepartmentsPage } from './pages/DepartmentsPage';

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedLayout />}>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/complaints" element={<ComplaintsPage />} />
          <Route path="/complaints/:id" element={<ComplaintDetailPage />} />
          <Route path="/categories" element={<CategoriesPage />} />
          <Route path="/departments" element={<DepartmentsPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

function ProtectedLayout() {
  const navigate = useNavigate();
  const user = getStoredToken() ? getStoredUser() : null;
  const [menuOpen, setMenuOpen] = useState(false);
  const initials = user?.fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('');

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  function handleLogout() {
    clearSession();
    navigate('/login', { replace: true });
  }

  return (
    <main className="app-shell">
      <aside className={`sidebar${menuOpen ? ' sidebar-open' : ''}`}>
        <div className="sidebar-heading">
          <div className="sidebar-brand">
            <span className="sidebar-brand-mark" aria-hidden="true">B</span>
            <span>
              <strong title={user.municipalityName ?? 'Belediyem'}>{user.municipalityName ?? 'Belediyem'}</strong>
              <small>Belediye çalışan paneli</small>
            </span>
          </div>
          <button
            type="button"
            className="sidebar-toggle"
            aria-expanded={menuOpen}
            aria-controls="admin-navigation"
            onClick={() => setMenuOpen((open) => !open)}
          >
            <span aria-hidden="true">{menuOpen ? '×' : '☰'}</span>
            <span className="visually-hidden">{menuOpen ? 'Menüyü kapat' : 'Menüyü aç'}</span>
          </button>
        </div>
        <div className="sidebar-menu" id="admin-navigation">
          <span className="sidebar-nav-label">YÖNETİM</span>
          <nav aria-label="Yönetim menüsü">
            <NavLink to="/" end onClick={() => setMenuOpen(false)}>
              <DashboardIcon /> <span>Genel bakış</span>
            </NavLink>
            <NavLink to="/complaints" onClick={() => setMenuOpen(false)}><ComplaintIcon /> <span>Bildirimler</span></NavLink>
            <NavLink to="/categories" onClick={() => setMenuOpen(false)}><CategoryIcon /> <span>Kategoriler</span></NavLink>
            <NavLink to="/departments" onClick={() => setMenuOpen(false)}><DepartmentIcon /> <span>Birimler</span></NavLink>
          </nav>
          <div className="sidebar-user">
            <span className="sidebar-avatar" aria-hidden="true">{initials || 'B'}</span>
            <span className="sidebar-user-copy">
              <strong>{user.fullName}</strong>
              <span className="sidebar-role">{user.municipalityName ?? user.userType}</span>
            </span>
            <button type="button" onClick={handleLogout}>
              <LogoutIcon /> <span>Çıkış</span>
            </button>
          </div>
        </div>
      </aside>
      <section className="content">
        <Outlet />
      </section>
    </main>
  );
}

function DashboardIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="3" width="7" height="7" rx="2" /><rect x="14" y="3" width="7" height="7" rx="2" /><rect x="3" y="14" width="7" height="7" rx="2" /><rect x="14" y="14" width="7" height="7" rx="2" /></svg>;
}

function ComplaintIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M6 3h12a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H9l-5 4v-4a2 2 0 0 1-2-2V7a4 4 0 0 1 4-4Z" /><path d="M8 8h8M8 12h5" /></svg>;
}

function CategoryIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h6v6h-6z" /></svg>;
}

function DepartmentIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 21V7l8-4 8 4v14M8 10h2M14 10h2M8 14h2M14 14h2M9 21v-3h6v3" /></svg>;
}

function LogoutIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M10 5H5v14h5M14 8l4 4-4 4M8 12h10" /></svg>;
}

function LoginPage() {
  const navigate = useNavigate();
  const [email, setEmail] = useState('admin@demo.local');
  const [password, setPassword] = useState('Demo123!');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  if (getStoredToken() && getStoredUser()) {
    return <Navigate to="/" replace />;
  }

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
    navigate('/', { replace: true });
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
