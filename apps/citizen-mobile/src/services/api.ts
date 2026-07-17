import Constants from 'expo-constants';

// Fiziksel cihaz/emülatör "localhost" ile ana makineye ulaşamaz.
// EXPO_PUBLIC_API_BASE_URL veya app.json -> expo.extra.apiBaseUrl ile ayarlayın.
// (Android emülatör: http://10.0.2.2:5080 — gerçek telefon: http://<PC-LAN-IP>:5080)
export const API_BASE_URL =
  process.env.EXPO_PUBLIC_API_BASE_URL ??
  (Constants.expoConfig?.extra?.apiBaseUrl as string | undefined) ??
  'http://localhost:5080';

export type ApiResponse<T> = {
  success: boolean;
  data: T | null;
  message: string | null;
  errors: string[];
};

export type MunicipalityResolveResult = {
  isSuccess: boolean;
  municipalityId: string | null;
  municipalityName: string | null;
  municipalityCode: string | null;
  failureReason: string | null;
};

export type PublicCategory = { id: string; name: string; code: string };

export type District = {
  id: string;
  name: string;
  code: string;
  latitude: number | null;
  longitude: number | null;
};

export type CreateComplaintResponse = {
  complaintId: string;
  trackingCode: string;
  municipalityName: string;
  status: string;
  createdAt: string;
};

export type CreateComplaintRequest = {
  categoryId: string;
  title?: string;
  description: string;
  citizenFullName?: string;
  citizenPhoneNumber?: string;
  citizenEmail?: string;
  latitude: number;
  longitude: number;
  addressText?: string;
  isAnonymous: boolean;
  source: 'CitizenMobile';
  municipalityId?: string;
  // Dolu ise şikayet bir dağıtım kurumuna (elektrik/su/doğalgaz) yönlenir.
  institutionId?: string;
};

export type ComplaintStatusHistoryEntry = {
  previousStatus: string | null;
  newStatus: string;
  note: string | null;
  createdAt: string;
};

export type ComplaintResponseEntry = { body: string; createdAt: string };

export type TrackedComplaint = {
  trackingCode: string;
  municipalityName: string;
  categoryName: string;
  departmentName: string | null;
  title: string;
  description: string;
  addressText: string | null;
  status: string;
  createdAt: string;
  updatedAt: string | null;
  closedAt: string | null;
  attachmentCount: number;
  statusHistory: ComplaintStatusHistoryEntry[];
  responses: ComplaintResponseEntry[];
  // true ise şikayet bir dağıtım kurumuna düştü; municipalityName kurum adını taşır.
  isInstitution: boolean;
};

export type Pharmacy = {
  id: string;
  name: string;
  province: string;
  district: string;
  addressText: string | null;
  phoneNumber: string | null;
  latitude: number;
  longitude: number;
  isOnDuty: boolean;
  distanceKm: number | null;
};

export type PublicStats = {
  totalComplaints: number;
  resolvedComplaints: number;
  activeMunicipalities: number;
  categories: number;
};

export type PhotoAsset = { uri: string; name: string; type: string };

async function readJson<T>(response: Response): Promise<ApiResponse<T>> {
  const text = await response.text();
  if (!text) {
    return {
      success: response.ok,
      data: null,
      message: response.ok ? null : response.status === 401 ? 'Oturum süreniz doldu. Lütfen tekrar giriş yapın.' : 'Bir hata oluştu.',
      errors: []
    };
  }
  try {
    return JSON.parse(text) as ApiResponse<T>;
  } catch {
    return { success: false, data: null, message: 'Sunucu yanıtı okunamadı.', errors: [] };
  }
}

async function safeFetch<T>(input: string, init?: RequestInit): Promise<ApiResponse<T>> {
  try {
    const response = await fetch(`${API_BASE_URL}${input}`, init);
    return await readJson<T>(response);
  } catch {
    return { success: false, data: null, message: 'Sunucuya ulaşılamadı. İnternet bağlantınızı kontrol edin.', errors: [] };
  }
}

export const resolveMunicipality = (lat: number, lng: number) =>
  safeFetch<MunicipalityResolveResult>(`/api/public/municipalities/resolve?lat=${lat}&lng=${lng}`);

export const getMunicipalityCategories = (municipalityId: string) =>
  safeFetch<PublicCategory[]>(`/api/public/municipalities/${municipalityId}/categories`);

export const getProvinces = () => safeFetch<string[]>('/api/public/provinces');

export const getDistricts = (province: string) =>
  safeFetch<District[]>(`/api/public/districts?province=${encodeURIComponent(province)}`);

export type Institution = {
  id: string;
  name: string;
  type: 'Electricity' | 'Water' | 'NaturalGas' | string;
  typeLabel: string;
};

// Konumdaki dağıtım kurumları — çözülen belediyenin id'siyle sorgulanır (il sunucuda türetilir).
export function getInstitutions(params: { province?: string; district?: string; municipalityId?: string }) {
  const q = new URLSearchParams();
  if (params.province) q.set('province', params.province);
  if (params.district) q.set('district', params.district);
  if (params.municipalityId) q.set('municipalityId', params.municipalityId);
  return safeFetch<Institution[]>(`/api/public/institutions?${q.toString()}`);
}

export const getInstitutionCategories = (institutionId: string) =>
  safeFetch<PublicCategory[]>(`/api/public/institutions/${institutionId}/categories`);

export const getPublicStats = () => safeFetch<PublicStats>('/api/public/stats');

export function getPharmacies(params: { province?: string; district?: string; onDutyOnly?: boolean }) {
  const q = new URLSearchParams();
  if (params.province) q.set('province', params.province);
  if (params.district) q.set('district', params.district);
  if (params.onDutyOnly) q.set('onDutyOnly', 'true');
  return safeFetch<Pharmacy[]>(`/api/public/pharmacies?${q.toString()}`);
}

export function getNearbyPharmacies(lat: number, lng: number, onDutyOnly: boolean) {
  const q = new URLSearchParams({ lat: String(lat), lng: String(lng), limit: '20' });
  if (onDutyOnly) q.set('onDutyOnly', 'true');
  return safeFetch<Pharmacy[]>(`/api/public/pharmacies/nearby?${q.toString()}`);
}

export const createComplaint = (request: CreateComplaintRequest) =>
  safeFetch<CreateComplaintResponse>('/api/public/complaints', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  });

function buildComplaintForm(request: CreateComplaintRequest, photos: PhotoAsset[]): FormData {
  const form = new FormData();
  form.append('categoryId', request.categoryId);
  form.append('description', request.description);
  form.append('latitude', String(request.latitude));
  form.append('longitude', String(request.longitude));
  form.append('isAnonymous', String(request.isAnonymous));
  form.append('source', request.source);
  if (request.title) form.append('title', request.title);
  if (request.municipalityId) form.append('municipalityId', request.municipalityId);
  if (request.institutionId) form.append('institutionId', request.institutionId);
  if (request.citizenFullName) form.append('citizenFullName', request.citizenFullName);
  if (request.citizenPhoneNumber) form.append('citizenPhoneNumber', request.citizenPhoneNumber);
  if (request.citizenEmail) form.append('citizenEmail', request.citizenEmail);
  if (request.addressText) form.append('addressText', request.addressText);
  photos.forEach((photo) => {
    // React Native FormData dosya biçimi.
    form.append('files', { uri: photo.uri, name: photo.name, type: photo.type } as unknown as Blob);
  });
  return form;
}

export const createComplaintWithPhotos = (request: CreateComplaintRequest, photos: PhotoAsset[]) =>
  safeFetch<CreateComplaintResponse>('/api/public/complaints', {
    method: 'POST',
    body: buildComplaintForm(request, photos)
  });

export const trackComplaint = (trackingCode: string) =>
  safeFetch<TrackedComplaint>(`/api/public/complaints/track/${encodeURIComponent(trackingCode.trim())}`);

export function addComplaintAttachments(trackingCode: string, photos: PhotoAsset[]) {
  const form = new FormData();
  photos.forEach((photo) => {
    form.append('files', { uri: photo.uri, name: photo.name, type: photo.type } as unknown as Blob);
  });
  return safeFetch<unknown>(`/api/public/complaints/${encodeURIComponent(trackingCode)}/attachments`, {
    method: 'POST',
    body: form
  });
}

// ---- Kimlik doğrulamalı uçlar ----

export type CitizenUser = { id: string; fullName: string; email: string; userType: string };

export type CitizenAuthResponse = {
  accessToken: string;
  expiresAt: string;
  user: CitizenUser;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  phoneVerified: boolean;
  verificationCodePreview: string | null;
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

export type RegisterPayload = { email: string; phoneNumber: string; password: string; fullName?: string };

export type CitizenComplaintPayload = {
  categoryId: string;
  title?: string;
  description: string;
  latitude: number;
  longitude: number;
  addressText?: string;
  municipalityId?: string;
};

export const registerCitizen = (payload: RegisterPayload) =>
  safeFetch<CitizenAuthResponse>('/api/citizen/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });

export const loginCitizen = (email: string, password: string) =>
  safeFetch<CitizenAuthResponse>('/api/citizen/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });

export const verifyPhone = (token: string, code: string) =>
  safeFetch<unknown>('/api/citizen/auth/verify-phone', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify({ code })
  });

export const resendCode = (token: string) =>
  safeFetch<{ verificationCodePreview: string | null }>('/api/citizen/auth/resend-code', {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` }
  });

export const getMyComplaints = (token: string) =>
  safeFetch<MyComplaint[]>('/api/citizen/complaints', { headers: { Authorization: `Bearer ${token}` } });

export const createComplaintAuthed = (token: string, payload: CitizenComplaintPayload) =>
  safeFetch<{ trackingCode: string }>('/api/citizen/complaints', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(payload)
  });
