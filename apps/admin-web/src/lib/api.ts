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
  }

  return response;
}

export async function login(email: string, password: string): Promise<ApiResponse<LoginResponse>> {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });

  return (await response.json()) as ApiResponse<LoginResponse>;
}

export async function getAdminComplaints(page = 1, pageSize = 20): Promise<AdminComplaintListResponse> {
  const response = await authorizedFetch(`/api/admin/complaints?page=${page}&pageSize=${pageSize}`);
  return (await response.json()) as AdminComplaintListResponse;
}

export async function getDashboardSummary(): Promise<ApiResponse<DashboardSummary>> {
  const response = await authorizedFetch('/api/admin/dashboard/summary');
  return (await response.json()) as ApiResponse<DashboardSummary>;
}
