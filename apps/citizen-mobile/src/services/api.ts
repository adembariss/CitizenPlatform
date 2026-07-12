import Constants from 'expo-constants';

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
  latitude: number;
  longitude: number;
  isAnonymous: boolean;
  source: 'CitizenMobile';
};

// Physical devices/emulators cannot reach the host machine via "localhost".
// Override with EXPO_PUBLIC_API_BASE_URL or app.json -> expo.extra.apiBaseUrl
// (Android emulator: use http://10.0.2.2:5080 instead of localhost).
const API_BASE_URL =
  process.env.EXPO_PUBLIC_API_BASE_URL ??
  (Constants.expoConfig?.extra?.apiBaseUrl as string | undefined) ??
  'http://localhost:5080';

async function readJson<T>(response: Response): Promise<ApiResponse<T>> {
  return (await response.json()) as ApiResponse<T>;
}

export async function resolveMunicipality(latitude: number, longitude: number): Promise<ApiResponse<MunicipalityResolveResult>> {
  const response = await fetch(`${API_BASE_URL}/api/public/municipalities/resolve?lat=${latitude}&lng=${longitude}`);
  return readJson<MunicipalityResolveResult>(response);
}

export async function getMunicipalityCategories(municipalityId: string): Promise<ApiResponse<PublicCategory[]>> {
  const response = await fetch(`${API_BASE_URL}/api/public/municipalities/${municipalityId}/categories`);
  return readJson<PublicCategory[]>(response);
}

export async function createComplaint(request: CreateComplaintRequest): Promise<ApiResponse<CreateComplaintResponse>> {
  const response = await fetch(`${API_BASE_URL}/api/public/complaints`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  });

  return readJson<CreateComplaintResponse>(response);
}
