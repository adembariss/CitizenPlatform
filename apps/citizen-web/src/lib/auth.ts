import { ApiResponse } from './api';

export type CitizenUser = {
  id: string;
  fullName: string;
  email: string;
  userType: string;
};

export type CitizenAuthResponse = {
  accessToken: string;
  expiresAt: string;
  user: CitizenUser;
  refreshToken: string;
  refreshTokenExpiresAt: string;
};

export type CitizenProfile = {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string | null;
};

export type MyComplaint = {
  trackingCode: string;
  title: string;
  municipalityName: string;
  categoryName: string;
  departmentName: string | null;
  status: string;
  createdAt: string;
  updatedAt: string | null;
};

export type RegisterPayload = {
  email: string;
  phoneNumber: string;
  password: string;
  fullName?: string;
};

export type CitizenComplaintPayload = {
  categoryId: string;
  title?: string;
  description: string;
  latitude: number;
  longitude: number;
  addressText?: string;
  municipalityId?: string;
};

const ACCESS_KEY = 'citizen_access_token';
const USER_KEY = 'citizen_user';

export function getStoredToken(): string | null {
  return localStorage.getItem(ACCESS_KEY);
}

export function getStoredUser(): CitizenUser | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as CitizenUser;
  } catch {
    return null;
  }
}

export function storeSession(response: CitizenAuthResponse): void {
  localStorage.setItem(ACCESS_KEY, response.accessToken);
  localStorage.setItem(USER_KEY, JSON.stringify(response.user));
}

export function clearSession(): void {
  localStorage.removeItem(ACCESS_KEY);
  localStorage.removeItem(USER_KEY);
}

async function readJson<T>(response: Response): Promise<ApiResponse<T>> {
  // Some responses (e.g. a 401 from the [Authorize] filter) have an empty body,
  // so response.json() would throw. Read defensively and map to an ApiResponse.
  const text = await response.text();
  if (!text) {
    return {
      success: response.ok,
      data: null,
      message: response.status === 401 ? 'Oturum süreniz doldu. Lütfen tekrar giriş yapın.' : response.ok ? null : 'Bir hata oluştu.',
      errors: []
    };
  }

  try {
    return JSON.parse(text) as ApiResponse<T>;
  } catch {
    return { success: false, data: null, message: 'Sunucu yanıtı okunamadı.', errors: [] };
  }
}

export async function registerCitizen(payload: RegisterPayload): Promise<ApiResponse<CitizenAuthResponse>> {
  const response = await fetch('/api/citizen/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  return readJson<CitizenAuthResponse>(response);
}

export async function loginCitizen(email: string, password: string): Promise<ApiResponse<CitizenAuthResponse>> {
  const response = await fetch('/api/citizen/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });
  return readJson<CitizenAuthResponse>(response);
}

export async function getMyComplaints(token: string): Promise<ApiResponse<MyComplaint[]>> {
  const response = await fetch('/api/citizen/complaints', {
    headers: { Authorization: `Bearer ${token}` }
  });
  return readJson<MyComplaint[]>(response);
}

export async function createComplaintAuthed(
  token: string,
  payload: CitizenComplaintPayload
): Promise<ApiResponse<{ trackingCode: string }>> {
  const response = await fetch('/api/citizen/complaints', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(payload)
  });
  return readJson<{ trackingCode: string }>(response);
}
