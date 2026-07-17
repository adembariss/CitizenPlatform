import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
  loginCitizen,
  registerCitizen,
  resendCode as resendCodeApi,
  verifyPhone as verifyPhoneApi,
  type CitizenAuthResponse,
  type CitizenUser,
  type RegisterPayload
} from '../services/api';
import { clearSession, loadSession, saveSession } from '../services/storage';

export type AuthOutcome =
  | { ok: true; phoneVerified: boolean; codePreview: string | null }
  | { ok: false; errors: string[] };

type AuthContextValue = {
  ready: boolean;
  token: string | null;
  user: CitizenUser | null;
  phoneVerified: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<AuthOutcome>;
  register: (payload: RegisterPayload) => Promise<AuthOutcome>;
  verifyPhone: (code: string) => Promise<AuthOutcome>;
  resendCode: () => Promise<string | null>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

function messagesFrom(message: string | null, errors: string[]): string[] {
  if (errors.length > 0) return errors;
  return [message ?? 'Bir hata oluştu. Lütfen tekrar deneyin.'];
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [ready, setReady] = useState(false);
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<CitizenUser | null>(null);
  const [phoneVerified, setPhoneVerified] = useState(false);

  useEffect(() => {
    loadSession().then((session) => {
      if (session) {
        setToken(session.token);
        setUser(session.user);
        setPhoneVerified(true); // Kalıcı oturum yalnızca doğrulanmış hesaplar için tutulur.
      }
      setReady(true);
    });
  }, []);

  const applySession = useCallback(async (response: CitizenAuthResponse) => {
    setToken(response.accessToken);
    setUser(response.user);
    setPhoneVerified(response.phoneVerified);
    if (response.phoneVerified) {
      await saveSession(response.accessToken, response.user);
    }
  }, []);

  const login = useCallback(
    async (email: string, password: string): Promise<AuthOutcome> => {
      const result = await loginCitizen(email, password);
      if (!result.success || !result.data) {
        return { ok: false, errors: messagesFrom(result.message, result.errors) };
      }
      await applySession(result.data);
      return { ok: true, phoneVerified: result.data.phoneVerified, codePreview: result.data.verificationCodePreview };
    },
    [applySession]
  );

  const register = useCallback(
    async (payload: RegisterPayload): Promise<AuthOutcome> => {
      const result = await registerCitizen(payload);
      if (!result.success || !result.data) {
        return { ok: false, errors: messagesFrom(result.message, result.errors) };
      }
      await applySession(result.data);
      return { ok: true, phoneVerified: result.data.phoneVerified, codePreview: result.data.verificationCodePreview };
    },
    [applySession]
  );

  const verifyPhone = useCallback(
    async (code: string): Promise<AuthOutcome> => {
      if (!token) return { ok: false, errors: ['Oturum bulunamadı.'] };
      const result = await verifyPhoneApi(token, code);
      if (!result.success) {
        return { ok: false, errors: messagesFrom(result.message, result.errors) };
      }
      setPhoneVerified(true);
      if (user) await saveSession(token, user);
      return { ok: true, phoneVerified: true, codePreview: null };
    },
    [token, user]
  );

  const resendCode = useCallback(async (): Promise<string | null> => {
    if (!token) return null;
    const result = await resendCodeApi(token);
    return result.data?.verificationCodePreview ?? null;
  }, [token]);

  const logout = useCallback(async () => {
    await clearSession();
    setToken(null);
    setUser(null);
    setPhoneVerified(false);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      ready,
      token,
      user,
      phoneVerified,
      isAuthenticated: Boolean(token) && phoneVerified,
      login,
      register,
      verifyPhone,
      resendCode,
      logout
    }),
    [ready, token, user, phoneVerified, login, register, verifyPhone, resendCode, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
