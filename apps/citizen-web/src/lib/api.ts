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
};

async function readJson<T>(response: Response): Promise<ApiResponse<T>> {
  const body = (await response.json()) as ApiResponse<T>;
  return body;
}

export async function resolveMunicipality(latitude: number, longitude: number): Promise<ApiResponse<MunicipalityResolveResult>> {
  const response = await fetch(`/api/public/municipalities/resolve?lat=${latitude}&lng=${longitude}`);
  return readJson<MunicipalityResolveResult>(response);
}

export async function createComplaint(request: CreateComplaintRequest): Promise<ApiResponse<CreateComplaintResponse>> {
  const response = await fetch('/api/public/complaints', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  });

  return readJson<CreateComplaintResponse>(response);
}

export function getCurrentPosition(): Promise<GeolocationPosition> {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
      reject(new Error('Bu tarayıcı konum servisini desteklemiyor.'));
      return;
    }

    navigator.geolocation.getCurrentPosition(resolve, reject, { enableHighAccuracy: true, timeout: 10_000 });
  });
}
