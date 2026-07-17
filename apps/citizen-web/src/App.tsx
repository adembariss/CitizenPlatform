import { useState } from 'react';
import { HomePage } from './views/HomePage';
import { ReportForm } from './views/ReportForm';
import { TrackComplaint } from './views/TrackComplaint';
import { LoginView } from './views/LoginView';
import { RegisterView } from './views/RegisterView';
import { VerifyPhoneView } from './views/VerifyPhoneView';
import { MyComplaints } from './views/MyComplaints';
import { PharmaciesView } from './views/PharmaciesView';
import { BrandMark } from './components/BrandMark';
import { useAuth } from './lib/AuthContext';

type View =
  | { name: 'home' }
  | { name: 'report' }
  | { name: 'track'; initialCode?: string }
  | { name: 'pharmacies' }
  | { name: 'login' }
  | { name: 'register' }
  | { name: 'verify'; codePreview: string | null }
  | { name: 'mine' };

export function App() {
  const { isAuthenticated, user, logout } = useAuth();
  const [view, setView] = useState<View>({ name: 'home' });
  const [menuOpen, setMenuOpen] = useState(false);

  const showView = (nextView: View) => {
    setView(nextView);
    setMenuOpen(false);
  };
  const handleLogout = () => {
    logout();
    showView({ name: 'home' });
  };
  const goHome = () => showView({ name: 'home' });
  const goReport = () => showView({ name: 'report' });
  const goTrack = (initialCode?: string) => showView({ name: 'track', initialCode });
  const goPharmacies = () => showView({ name: 'pharmacies' });
  const goLogin = () => showView({ name: 'login' });
  const goRegister = () => showView({ name: 'register' });
  const goVerify = (codePreview: string | null) => showView({ name: 'verify', codePreview });
  const goMine = () => showView({ name: 'mine' });

  return (
    <div className="portal">
      <header className="site-header">
        <button type="button" className="brand" onClick={goHome} aria-label="Ana sayfa">
          <BrandMark />
          <span className="brand-text">
            Belediyem
            <small>Vatandaş Bildirim Portalı</small>
          </span>
        </button>

        <button
          type="button"
          className="nav-toggle"
          aria-expanded={menuOpen}
          aria-controls="citizen-navigation"
          onClick={() => setMenuOpen((open) => !open)}
        >
          <span aria-hidden="true">{menuOpen ? '×' : '☰'}</span>
          <span className="visually-hidden">{menuOpen ? 'Menüyü kapat' : 'Menüyü aç'}</span>
        </button>

        <nav className={`site-nav${menuOpen ? ' site-nav-open' : ''}`} id="citizen-navigation" aria-label="Ana menü">
          <button type="button" className={navClass(view.name === 'home')} onClick={goHome}>
            Ana Sayfa
          </button>
          <button type="button" className={navClass(view.name === 'report')} onClick={goReport}>
            Şikayet Oluştur
          </button>
          <button type="button" className={navClass(view.name === 'track')} onClick={() => goTrack()}>
            Şikayet Sorgula
          </button>
          <button type="button" className={navClass(view.name === 'pharmacies')} onClick={goPharmacies}>
            Eczaneler
          </button>
          {isAuthenticated ? (
            <>
              <button type="button" className={navClass(view.name === 'mine')} onClick={goMine}>
                Şikayetlerim
              </button>
              <div className="nav-account">
                <span className="nav-account-badge" aria-hidden="true">
                  {(user?.fullName || user?.email || '?').trim().charAt(0).toUpperCase()}
                </span>
                <span className="nav-account-name" title={user?.email}>{user?.fullName || 'Hesabım'}</span>
                <button type="button" className="nav-logout" onClick={handleLogout}>
                  Çıkış
                </button>
              </div>
            </>
          ) : (
            <>
              <button type="button" className={navClass(view.name === 'login')} onClick={goLogin}>
                Giriş
              </button>
              <button type="button" className="btn btn-primary nav-cta" onClick={goRegister}>
                Üye Ol
              </button>
            </>
          )}
        </nav>
      </header>

      <main className="site-main">
        {view.name === 'home' && <HomePage onReport={goReport} onTrack={() => goTrack()} />}

        {view.name === 'report' && (
          <div className="page">
            <div className="citizen-shell report-shell">
              <ReportForm onTrack={goTrack} />
            </div>
          </div>
        )}

        {view.name === 'track' && (
          <div className="page">
            <div className="track-shell">
              <TrackComplaint initialCode={view.initialCode} />
            </div>
          </div>
        )}

        {view.name === 'pharmacies' && (
          <div className="page">
            <div className="pharmacy-shell">
              <PharmaciesView />
            </div>
          </div>
        )}

        {view.name === 'login' && (
          <div className="page">
            <LoginView onSuccess={goMine} onRegister={goRegister} />
          </div>
        )}

        {view.name === 'register' && (
          <div className="page">
            <RegisterView onSuccess={goMine} onLogin={goLogin} onVerify={goVerify} />
          </div>
        )}

        {view.name === 'verify' && (
          <div className="page">
            <VerifyPhoneView codePreview={view.codePreview} onVerified={goMine} onSkip={goMine} />
          </div>
        )}

        {view.name === 'mine' &&
          (isAuthenticated ? (
            <div className="page">
              <div className="mine-page">
                <MyComplaints onTrack={goTrack} onReport={goReport} />
              </div>
            </div>
          ) : (
            <div className="page">
              <LoginView onSuccess={goMine} onRegister={goRegister} />
            </div>
          ))}
      </main>

      <footer className="site-footer">
        <div className="footer-inner">
          <div className="footer-brand">
            <BrandMark />
            <div>
              <strong>Belediyem</strong>
              <p>Vatandaş odaklı şikayet ve bildirim yönetim platformu.</p>
            </div>
          </div>
          <div className="footer-cols">
            <div>
              <h4>Hızlı Erişim</h4>
              <button type="button" className="footer-link" onClick={goReport}>
                Şikayet Oluştur
              </button>
              <button type="button" className="footer-link" onClick={() => goTrack()}>
                Şikayet Sorgula
              </button>
              <button type="button" className="footer-link" onClick={goHome}>
                Ana Sayfa
              </button>
            </div>
            <div>
              <h4>Acil Durumlar</h4>
              <p className="footer-note">Yangın · İtfaiye 112</p>
              <p className="footer-note">Belediye Çağrı Merkezi 153</p>
              <p className="footer-note">Elektrik / Su Arıza Hattı</p>
            </div>
          </div>
        </div>
        <div className="footer-bar">
          <span>© {new Date().getFullYear()} Belediyem — Vatandaş Bildirim Portalı</span>
          <span>NetCAD teknolojisiyle geliştirilmiştir</span>
        </div>
      </footer>
    </div>
  );
}

function navClass(active: boolean): string {
  return active ? 'nav-link active' : 'nav-link';
}
