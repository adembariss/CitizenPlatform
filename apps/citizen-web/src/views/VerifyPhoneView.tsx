import { FormEvent, useState } from 'react';
import { useAuth } from '../lib/AuthContext';
import { resendCode, verifyPhone } from '../lib/auth';

type VerifyPhoneViewProps = {
  codePreview: string | null;
  onVerified: () => void;
  onSkip: () => void;
};

export function VerifyPhoneView({ codePreview, onVerified, onSkip }: VerifyPhoneViewProps) {
  const { token } = useAuth();
  const [code, setCode] = useState('');
  const [devCode, setDevCode] = useState<string | null>(codePreview);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!token) return;
    setBusy(true);
    setError(null);
    setInfo(null);
    const result = await verifyPhone(token, code.trim());
    setBusy(false);
    if (!result.success) {
      setError(result.message ?? 'Doğrulama başarısız.');
      return;
    }
    onVerified();
  }

  async function handleResend() {
    if (!token) return;
    setError(null);
    const result = await resendCode(token);
    if (result.success && result.data) {
      setDevCode(result.data.verificationCodePreview);
      setInfo('Yeni kod telefonuna gönderildi.');
    } else {
      setError(result.message ?? 'Kod gönderilemedi.');
    }
  }

  return (
    <>
      <section className="intro">
        <p>Telefon doğrulama</p>
        <h1>Telefonuna gelen kodu gir.</h1>
      </section>
      <div className="track-shell">
        <form className="report-form" onSubmit={handleSubmit}>
          <p className="field-hint">
            Telefonuna 6 haneli bir doğrulama kodu gönderdik. SMS servisi bağlanınca kod gerçek
            telefonuna gelecek; şimdilik demo için aşağıda gösteriliyor.
          </p>
          {devCode && (
            <p className="account-hint" role="status">
              Demo doğrulama kodun: <strong>{devCode}</strong>
            </p>
          )}
          <label>
            Doğrulama kodu
            <input
              value={code}
              onChange={(event) => setCode(event.target.value.replace(/\D/g, '').slice(0, 6))}
              inputMode="numeric"
              placeholder="6 haneli kod"
              autoComplete="one-time-code"
              required
            />
          </label>
          {error && <p className="form-error">{error}</p>}
          {info && <p className="field-hint">{info}</p>}
          <button type="submit" disabled={busy || code.length !== 6}>
            {busy ? 'Doğrulanıyor...' : 'Doğrula'}
          </button>
          <div className="verify-actions">
            <button type="button" className="link-button" onClick={handleResend}>
              Kodu tekrar gönder
            </button>
            <button type="button" className="link-button" onClick={onSkip}>
              Daha sonra
            </button>
          </div>
        </form>
      </div>
    </>
  );
}
