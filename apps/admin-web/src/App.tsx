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

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  function handleLogout() {
    clearSession();
    navigate('/login', { replace: true });
  }

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <strong>CitizenPlatform</strong>
        <nav>
          <NavLink to="/" end>
            Panel
          </NavLink>
          <NavLink to="/complaints">Şikayetler</NavLink>
          <NavLink to="/categories">Kategoriler</NavLink>
          <NavLink to="/departments">Birimler</NavLink>
        </nav>
        <div className="sidebar-user">
          <span>{user.fullName}</span>
          <span className="sidebar-role">{user.municipalityName ?? user.userType}</span>
          <button type="button" onClick={handleLogout}>
            Çıkış yap
          </button>
        </div>
      </aside>
      <section className="content">
        <Outlet />
      </section>
    </main>
  );
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
