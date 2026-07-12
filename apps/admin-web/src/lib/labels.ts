export const COMPLAINT_STATUSES = [
  'New',
  'UnderReview',
  'Assigned',
  'InProgress',
  'WaitingForCitizen',
  'Resolved',
  'Closed',
  'Rejected',
  'Duplicate',
  'OutOfScope'
] as const;

const STATUS_LABELS: Record<string, string> = {
  New: 'Yeni',
  UnderReview: 'İncelemede',
  Assigned: 'Atandı',
  InProgress: 'İşlemde',
  WaitingForCitizen: 'Vatandaş bekleniyor',
  Resolved: 'Çözüldü',
  Closed: 'Kapatıldı',
  Rejected: 'Reddedildi',
  Duplicate: 'Mükerrer',
  OutOfScope: 'Kapsam dışı'
};

const PRIORITY_LABELS: Record<string, string> = {
  Low: 'Düşük',
  Normal: 'Normal',
  High: 'Yüksek',
  Critical: 'Kritik'
};

const SOURCE_LABELS: Record<string, string> = {
  CitizenWeb: 'Vatandaş web',
  CitizenMobile: 'Vatandaş mobil',
  AdminPanel: 'Yönetim paneli',
  Integration: 'Entegrasyon'
};

export function statusLabel(status: string | null | undefined): string {
  return status ? (STATUS_LABELS[status] ?? status) : '-';
}

export function priorityLabel(priority: string | null | undefined): string {
  return priority ? (PRIORITY_LABELS[priority] ?? priority) : '-';
}

export function sourceLabel(source: string | null | undefined): string {
  return source ? (SOURCE_LABELS[source] ?? source) : '-';
}

export function statusClass(status: string): string {
  return `status-badge status-${status.toLowerCase()}`;
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return '-';
  }

  return new Date(value).toLocaleString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
}
