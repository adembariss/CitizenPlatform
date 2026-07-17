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

export type PublicCategory = {
  id: string;
  name: string;
  code: string;
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
  source: 'CitizenWeb';
  municipalityId?: string;
  // Dolu ise şikayet bir dağıtım kurumuna (elektrik/su/doğalgaz) yönlenir.
  institutionId?: string;
};

export type District = {
  id: string;
  name: string;
  code: string;
  latitude: number | null;
  longitude: number | null;
};

export type ComplaintStatusHistoryEntry = {
  previousStatus: string | null;
  newStatus: string;
  note: string | null;
  createdAt: string;
};

export type ComplaintResponseEntry = {
  body: string;
  createdAt: string;
};

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

async function readJson<T>(response: Response): Promise<ApiResponse<T>> {
  // Defensive: empty or non-JSON bodies (e.g. an empty 401) must not throw.
  const text = await response.text();
  if (!text) {
    return {
      success: response.ok,
      data: null,
      message: response.ok ? null : 'Bir hata oluştu. Lütfen tekrar deneyin.',
      errors: []
    };
  }

  try {
    return JSON.parse(text) as ApiResponse<T>;
  } catch {
    return { success: false, data: null, message: 'Sunucu yanıtı okunamadı.', errors: [] };
  }
}

export async function resolveMunicipality(latitude: number, longitude: number): Promise<ApiResponse<MunicipalityResolveResult>> {
  const response = await fetch(`/api/public/municipalities/resolve?lat=${latitude}&lng=${longitude}`);
  return readJson<MunicipalityResolveResult>(response);
}

export async function getMunicipalityCategories(municipalityId: string): Promise<ApiResponse<PublicCategory[]>> {
  const response = await fetch(`/api/public/municipalities/${municipalityId}/categories`);
  return readJson<PublicCategory[]>(response);
}

export async function getProvinces(): Promise<ApiResponse<string[]>> {
  const response = await fetch('/api/public/provinces');
  return readJson<string[]>(response);
}

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

export async function getPharmacies(params: {
  province?: string;
  district?: string;
  onDutyOnly?: boolean;
}): Promise<ApiResponse<Pharmacy[]>> {
  const query = new URLSearchParams();
  if (params.province) query.set('province', params.province);
  if (params.district) query.set('district', params.district);
  if (params.onDutyOnly) query.set('onDutyOnly', 'true');
  const response = await fetch(`/api/public/pharmacies?${query.toString()}`);
  return readJson<Pharmacy[]>(response);
}

export async function getNearbyPharmacies(
  latitude: number,
  longitude: number,
  onDutyOnly: boolean
): Promise<ApiResponse<Pharmacy[]>> {
  const query = new URLSearchParams({ lat: String(latitude), lng: String(longitude), limit: '20' });
  if (onDutyOnly) query.set('onDutyOnly', 'true');
  const response = await fetch(`/api/public/pharmacies/nearby?${query.toString()}`);
  return readJson<Pharmacy[]>(response);
}

export async function getDistricts(province: string): Promise<ApiResponse<District[]>> {
  const response = await fetch(`/api/public/districts?province=${encodeURIComponent(province)}`);
  return readJson<District[]>(response);
}

export type Institution = {
  id: string;
  name: string;
  type: 'Electricity' | 'Water' | 'NaturalGas' | string;
  typeLabel: string;
};

// Konumdaki dağıtım kurumları — elektrik/su/doğalgaz. Adres modunda il/ilçe, harita modunda
// çözülen belediyenin id'si ile sorgulanır (il sunucuda türetilir).
export async function getInstitutions(params: {
  province?: string;
  district?: string;
  municipalityId?: string;
}): Promise<ApiResponse<Institution[]>> {
  const query = new URLSearchParams();
  if (params.province) query.set('province', params.province);
  if (params.district) query.set('district', params.district);
  if (params.municipalityId) query.set('municipalityId', params.municipalityId);
  const response = await fetch(`/api/public/institutions?${query.toString()}`);
  return readJson<Institution[]>(response);
}

export async function getInstitutionCategories(institutionId: string): Promise<ApiResponse<PublicCategory[]>> {
  const response = await fetch(`/api/public/institutions/${institutionId}/categories`);
  return readJson<PublicCategory[]>(response);
}

export async function createComplaint(request: CreateComplaintRequest): Promise<ApiResponse<CreateComplaintResponse>> {
  const response = await fetch('/api/public/complaints', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  });

  return readJson<CreateComplaintResponse>(response);
}

function buildComplaintForm(request: CreateComplaintRequest, files: File[]): FormData {
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

  for (const file of files) {
    form.append('files', file, file.name);
  }

  return form;
}

export async function createComplaintWithPhotos(
  request: CreateComplaintRequest,
  files: File[]
): Promise<ApiResponse<CreateComplaintResponse>> {
  const response = await fetch('/api/public/complaints', {
    method: 'POST',
    body: buildComplaintForm(request, files)
  });

  return readJson<CreateComplaintResponse>(response);
}

export async function trackComplaint(trackingCode: string): Promise<ApiResponse<TrackedComplaint>> {
  const response = await fetch(`/api/public/complaints/track/${encodeURIComponent(trackingCode.trim())}`);
  return readJson<TrackedComplaint>(response);
}

// Adds photos to an existing complaint (used by the logged-in flow, which creates
// the complaint via the authenticated JSON endpoint then attaches photos here).
export async function addComplaintAttachments(
  trackingCode: string,
  files: File[]
): Promise<ApiResponse<unknown>> {
  const form = new FormData();
  for (const file of files) {
    form.append('files', file, file.name);
  }

  const response = await fetch(`/api/public/complaints/${encodeURIComponent(trackingCode)}/attachments`, {
    method: 'POST',
    body: form
  });
  return readJson<unknown>(response);
}

export type PublicStats = {
  totalComplaints: number;
  resolvedComplaints: number;
  activeMunicipalities: number;
  categories: number;
};

export type ComplaintMapPoint = {
  latitude: number;
  longitude: number;
  categoryName: string;
  status: string;
  createdAt: string;
};

export async function getPublicStats(): Promise<ApiResponse<PublicStats>> {
  const response = await fetch('/api/public/stats');
  return readJson<PublicStats>(response);
}

export async function getComplaintMapPoints(limit = 300): Promise<ApiResponse<ComplaintMapPoint[]>> {
  const response = await fetch(`/api/public/complaints/map?limit=${limit}`);
  return readJson<ComplaintMapPoint[]>(response);
}

export function getCurrentPosition(): Promise<GeolocationPosition> {
  return new Promise((resolve, reject) => {
    if (typeof navigator === 'undefined' || !navigator.geolocation) {
      reject(new Error('Bu tarayıcı konum servisini desteklemiyor. Haritaya tıklayarak konum seçebilirsiniz.'));
      return;
    }

    // Browser geolocation is only exposed in a secure context (https) or on localhost.
    // Over a plain-http LAN address the API is silently unavailable, so give a clear hint.
    if (typeof window !== 'undefined' && window.isSecureContext === false) {
      reject(
        new Error(
          'Konum servisi yalnızca güvenli (https) veya localhost bağlantılarında çalışır. Haritaya tıklayarak konum seçebilirsiniz.'
        )
      );
      return;
    }

    navigator.geolocation.getCurrentPosition(
      resolve,
      (error) => reject(new Error(geolocationErrorMessage(error))),
      { enableHighAccuracy: true, timeout: 12_000, maximumAge: 0 }
    );
  });
}

function geolocationErrorMessage(error: GeolocationPositionError): string {
  switch (error.code) {
    case error.PERMISSION_DENIED:
      return 'Konum izni verilmedi. Tarayıcı adres çubuğundaki konum simgesinden izin verebilir ya da haritaya tıklayarak konum seçebilirsiniz.';
    case error.POSITION_UNAVAILABLE:
      return 'Konumunuz şu anda belirlenemedi. Haritaya tıklayarak konum seçebilirsiniz.';
    case error.TIMEOUT:
      return 'Konum alma zaman aşımına uğradı. Tekrar deneyin ya da haritaya tıklayarak konum seçin.';
    default:
      return 'Konum alınamadı. Haritaya tıklayarak konum seçebilirsiniz.';
  }
}
