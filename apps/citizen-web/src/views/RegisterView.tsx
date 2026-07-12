import { FormEvent, useState } from 'react';
import { useAuth } from '../lib/AuthContext';

type RegisterViewProps = {
  onSuccess: () => void;
  onLogin: () => void;
};

export function RegisterView({ onSuccess, onLogin }: RegisterViewProps) {
  const { register } = useAuth();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [errors, setErrors] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setErrors([]);
    const result = await register({
      email: email.trim(),
      phoneNumber: phone.trim(),
      password,
      fullName: fullName.trim() || undefined
    });
    setBusy(false);
    if (result) {
      setErrors(result);
      return;
    }
    onSuccess();
  }

  return (
    <>
      <section className="intro">
        <p>Vatandaş üyeliği</p>
        <h1>Hesap oluştur, şikayetlerini takip et.</h1>
      </section>
      <div className="track-shell">
        <form className="report-form" onSubmit={handleSubmit}>
          <label>
            Ad Soyad (opsiyonel)
            <input value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Ayşe Yılmaz" autoComplete="name" />
          </label>
          <label>
            E-posta
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" required />
          </label>
          <label>
            Cep Telefonu <span className="required-hint">(zorunlu)</span>
            <input
              type="tel"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="0532 123 45 67"
              autoComplete="tel"
              required
            />
          </label>
          <label>
            Şifre
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="new-password"
              required
            />
          </label>
          <p className="field-hint">Şifre en az 8 karakter olmalı ve harf ile rakam içermelidir.</p>
          {errors.length > 0 && (
            <ul className="form-error-list">
              {errors.map((message) => (
                <li key={message} className="form-error">
                  {message}
                </li>
              ))}
            </ul>
          )}
          <button type="submit" disabled={busy}>
            {busy ? 'Hesap oluşturuluyor...' : 'Üye ol'}
          </button>
          <p className="auth-switch">
            Zaten hesabın var mı?{' '}
            <button type="button" className="link-button" onClick={onLogin}>
              Giriş yap
            </button>
          </p>
        </form>
      </div>
    </>
  );
}
