import { createContext, useContext, useMemo, useState, ReactNode } from 'react';
import {
  CitizenUser,
  RegisterPayload,
  clearSession,
  getStoredToken,
  getStoredUser,
  loginCitizen,
  registerCitizen,
  storeSession
} from './auth';

export type RegisterOutcome =
  | { errors: string[] }
  | { ok: true; phoneVerified: boolean; codePreview: string | null };

type AuthContextValue = {
  user: CitizenUser | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<string | null>;
  register: (payload: RegisterPayload) => Promise<RegisterOutcome>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CitizenUser | null>(() => getStoredUser());
  const [token, setToken] = useState<string | null>(() => getStoredToken());

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: Boolean(token && user),
      login: async (email, password) => {
        const result = await loginCitizen(email, password);
        if (!result.success || !result.data) {
          return result.message ?? 'Giriş yapılamadı.';
        }
        storeSession(result.data);
        setUser(result.data.user);
        setToken(result.data.accessToken);
        return null;
      },
      register: async (payload) => {
        const result = await registerCitizen(payload);
        if (!result.success || !result.data) {
          return { errors: result.errors.length > 0 ? result.errors : [result.message ?? 'Kayıt yapılamadı.'] };
        }
        storeSession(result.data);
        setUser(result.data.user);
        setToken(result.data.accessToken);
        return { ok: true, phoneVerified: result.data.phoneVerified, codePreview: result.data.verificationCodePreview };
      },
      logout: () => {
        clearSession();
        setUser(null);
        setToken(null);
      }
    }),
    [user, token]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return ctx;
}
