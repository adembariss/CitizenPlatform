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
  const { isAuthenticated } = useAuth();
  const [view, setView] = useState<View>({ name: 'home' });

  const goHome = () => setView({ name: 'home' });
  const goReport = () => setView({ name: 'report' });
  const goTrack = (initialCode?: string) => setView({ name: 'track', initialCode });
  const goPharmacies = () => setView({ name: 'pharmacies' });
  const goLogin = () => setView({ name: 'login' });
  const goRegister = () => setView({ name: 'register' });
  const goVerify = (codePreview: string | null) => setView({ name: 'verify', codePreview });
  const goMine = () => setView({ name: 'mine' });

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

        <nav className="site-nav" aria-label="Ana menü">
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
            <button type="button" className={navClass(view.name === 'mine')} onClick={goMine}>
              Şikayetlerim
            </button>
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
            <div className="citizen-shell">
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
            <div className="track-shell">
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
              <MyComplaints onTrack={goTrack} onReport={goReport} />
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
