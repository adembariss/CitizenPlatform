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
};

async function readJson<T>(response: Response): Promise<ApiResponse<T>> {
  const body = (await response.json()) as ApiResponse<T>;
  return body;
}

export async function resolveMunicipality(latitude: number, longitude: number): Promise<ApiResponse<MunicipalityResolveResult>> {
  const response = await fetch(`/api/public/municipalities/resolve?lat=${latitude}&lng=${longitude}`);
  return readJson<MunicipalityResolveResult>(response);
}

export async function getMunicipalityCategories(municipalityId: string): Promise<ApiResponse<PublicCategory[]>> {
  const response = await fetch(`/api/public/municipalities/${municipalityId}/categories`);
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

export function getCurrentPosition(): Promise<GeolocationPosition> {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
      reject(new Error('Bu tarayıcı konum servisini desteklemiyor.'));
      return;
    }

    navigator.geolocation.getCurrentPosition(resolve, reject, { enableHighAccuracy: true, timeout: 10_000 });
  });
}
