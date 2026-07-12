export type ApiResponse<T> = {
  success: boolean;
  data: T | null;
  message: string | null;
  errors: string[];
};

export type CurrentUser = {
  id: string;
  fullName: string;
  email: string;
  userType: string;
  municipalityId: string | null;
  municipalityName: string | null;
  roles: string[];
};

export type LoginResponse = {
  accessToken: string;
  expiresAt: string;
  user: CurrentUser;
};

export type AdminComplaintListItem = {
  id: string;
  trackingCode: string;
  municipalityName: string;
  categoryName: string;
  departmentName: string | null;
  title: string;
  descriptionSummary: string;
  status: string;
  priority: string;
  citizenFullName: string | null;
  addressText: string | null;
  latitude: number;
  longitude: number;
  createdAt: string;
  updatedAt: string | null;
};

export type AdminComplaintListResponse = {
  items: AdminComplaintListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type AdminComplaintFilters = {
  status?: string;
  categoryId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
};

export type AdminComplaintAttachment = {
  id: string;
  fileName: string;
  originalFileName: string;
  contentType: string;
  sizeInBytes: number;
  sha256Hash: string;
  createdAt: string;
};

export type AdminComplaintStatusHistory = {
  id: string;
  previousStatus: string | null;
  newStatus: string;
  changedByUserId: string | null;
  note: string | null;
  isVisibleToCitizen: boolean;
  createdAt: string;
};

export type AdminComplaintComment = {
  id: string;
  authorUserId: string;
  body: string;
  isInternal: boolean;
  createdAt: string;
};

export type AdminComplaintAssignment = {
  id: string;
  departmentId: string;
  departmentName: string;
  assignedByUserId: string;
  assignedUserId: string | null;
  note: string | null;
  assignedAt: string;
};

export type AdminComplaintDetail = {
  id: string;
  trackingCode: string;
  municipalityId: string;
  municipalityName: string;
  categoryId: string;
  categoryName: string;
  departmentId: string | null;
  departmentName: string | null;
  title: string;
  description: string;
  citizenFullName: string | null;
  citizenPhoneNumber: string | null;
  citizenEmail: string | null;
  addressText: string | null;
  latitude: number;
  longitude: number;
  status: string;
  priority: string;
  source: string;
  createdAt: string;
  updatedAt: string | null;
  closedAt: string | null;
  attachments: AdminComplaintAttachment[];
  statusHistories: AdminComplaintStatusHistory[];
  comments: AdminComplaintComment[];
  assignments: AdminComplaintAssignment[];
};

export type Category = {
  id: string;
  municipalityId: string | null;
  name: string;
  code: string;
  isActive: boolean;
};

export type Department = {
  id: string;
  municipalityId: string;
  name: string;
  code: string;
  isActive: boolean;
};

export type DashboardSummary = {
  totalComplaints: number;
  openComplaints: number;
  todayComplaints: number;
  resolvedComplaints: number;
  closedComplaints: number;
  averageResolutionHours: number;
  byStatus: { status: string; count: number }[];
  byCategory: { categoryId: string; categoryName: string; count: number }[];
  byDepartment: { departmentId: string; departmentName: string; count: number }[];
};

const TOKEN_STORAGE_KEY = 'citizenplatform.admin.accessToken';
const USER_STORAGE_KEY = 'citizenplatform.admin.user';

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function getStoredUser(): CurrentUser | null {
  const raw = localStorage.getItem(USER_STORAGE_KEY);
  return raw ? (JSON.parse(raw) as CurrentUser) : null;
}

export function storeSession(response: LoginResponse): void {
  localStorage.setItem(TOKEN_STORAGE_KEY, response.accessToken);
  localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(response.user));
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_STORAGE_KEY);
  localStorage.removeItem(USER_STORAGE_KEY);
}

async function authorizedFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const token = getStoredToken();
  const headers = new Headers(init.headers);
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(path, { ...init, headers });
  if (response.status === 401) {
    clearSession();
    if (window.location.pathname !== '/login') {
      window.location.assign('/login');
    }
  }

  return response;
}

function jsonInit(method: string, body: unknown): RequestInit {
  return {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
  };
}

async function readEnvelope<T>(response: Response): Promise<ApiResponse<T>> {
  try {
    return (await response.json()) as ApiResponse<T>;
  } catch {
    return {
      success: false,
      data: null,
      message: `İstek başarısız oldu (HTTP ${response.status}).`,
      errors: []
    };
  }
}

export async function login(email: string, password: string): Promise<ApiResponse<LoginResponse>> {
  const response = await fetch('/api/auth/login', jsonInit('POST', { email, password }));
  return readEnvelope<LoginResponse>(response);
}

export async function getAdminComplaints(filters: AdminComplaintFilters = {}): Promise<AdminComplaintListResponse> {
  const params = new URLSearchParams();
  params.set('page', String(filters.page ?? 1));
  params.set('pageSize', String(filters.pageSize ?? 20));
  if (filters.status) {
    params.set('status', filters.status);
  }
  if (filters.categoryId) {
    params.set('categoryId', filters.categoryId);
  }
  if (filters.search?.trim()) {
    params.set('search', filters.search.trim());
  }

  const response = await authorizedFetch(`/api/admin/complaints?${params.toString()}`);
  if (!response.ok) {
    throw new Error(`Şikayet listesi alınamadı (HTTP ${response.status}).`);
  }

  return (await response.json()) as AdminComplaintListResponse;
}

export async function getAdminComplaintDetail(id: string): Promise<ApiResponse<AdminComplaintDetail>> {
  const response = await authorizedFetch(`/api/admin/complaints/${id}`);
  return readEnvelope<AdminComplaintDetail>(response);
}

export async function updateComplaintStatus(
  id: string,
  body: { newStatus: string; note: string | null; isVisibleToCitizen: boolean }
): Promise<ApiResponse<unknown>> {
  const response = await authorizedFetch(`/api/admin/complaints/${id}/status`, jsonInit('PUT', body));
  return readEnvelope<unknown>(response);
}

export async function assignComplaint(
  id: string,
  body: { departmentId: string; assignedUserId: string | null; note: string | null }
): Promise<ApiResponse<unknown>> {
  const response = await authorizedFetch(`/api/admin/complaints/${id}/assign`, jsonInit('PUT', body));
  return readEnvelope<unknown>(response);
}

export async function addComplaintComment(
  id: string,
  body: { commentText: string; isInternal: boolean }
): Promise<ApiResponse<AdminComplaintComment>> {
  const response = await authorizedFetch(`/api/admin/complaints/${id}/comments`, jsonInit('POST', body));
  return readEnvelope<AdminComplaintComment>(response);
}

export async function getCategories(): Promise<ApiResponse<Category[]>> {
  const response = await authorizedFetch('/api/admin/categories');
  return readEnvelope<Category[]>(response);
}

export async function createCategory(body: { name: string; code: string }): Promise<ApiResponse<Category>> {
  const response = await authorizedFetch('/api/admin/categories', jsonInit('POST', { ...body, municipalityId: null }));
  return readEnvelope<Category>(response);
}

export async function updateCategory(
  id: string,
  body: { name?: string | null; isActive?: boolean | null }
): Promise<ApiResponse<Category>> {
  const response = await authorizedFetch(`/api/admin/categories/${id}`, jsonInit('PUT', body));
  return readEnvelope<Category>(response);
}

export async function getDepartments(): Promise<ApiResponse<Department[]>> {
  const response = await authorizedFetch('/api/admin/departments');
  return readEnvelope<Department[]>(response);
}

export async function createDepartment(body: { name: string; code: string }): Promise<ApiResponse<Department>> {
  const response = await authorizedFetch('/api/admin/departments', jsonInit('POST', { ...body, municipalityId: null }));
  return readEnvelope<Department>(response);
}

export async function updateDepartment(
  id: string,
  body: { name?: string | null; isActive?: boolean | null }
): Promise<ApiResponse<Department>> {
  const response = await authorizedFetch(`/api/admin/departments/${id}`, jsonInit('PUT', body));
  return readEnvelope<Department>(response);
}

export async function getDashboardSummary(): Promise<ApiResponse<DashboardSummary>> {
  const response = await authorizedFetch('/api/admin/dashboard/summary');
  return readEnvelope<DashboardSummary>(response);
}
