import { FormEvent, useState } from 'react';
import { useAuth } from '../lib/AuthContext';

type LoginViewProps = {
  onSuccess: () => void;
  onRegister: () => void;
};

export function LoginView({ onSuccess, onRegister }: LoginViewProps) {
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    const message = await login(email.trim(), password);
    setBusy(false);
    if (message) {
      setError(message);
      return;
    }
    onSuccess();
  }

  return (
    <>
      <section className="intro">
        <p>Vatandaş girişi</p>
        <h1>Hesabınla giriş yap.</h1>
      </section>
      <div className="track-shell">
        <form className="report-form" onSubmit={handleSubmit}>
          <label>
            E-posta
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" required />
          </label>
          <label>
            Şifre
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" required />
          </label>
          {error && <p className="form-error">{error}</p>}
          <button type="submit" disabled={busy}>
            {busy ? 'Giriş yapılıyor...' : 'Giriş yap'}
          </button>
          <p className="auth-switch">
            Hesabın yok mu?{' '}
            <button type="button" className="link-button" onClick={onRegister}>
              Üye ol
            </button>
          </p>
        </form>
      </div>
    </>
  );
}
