import { colors } from '../theme';

const STATUS_LABELS: Record<string, string> = {
  New: 'Yeni',
  UnderReview: 'İnceleniyor',
  Assigned: 'Birime Atandı',
  InProgress: 'İşlemde',
  WaitingForCitizen: 'Yanıt Bekleniyor',
  Resolved: 'Çözüldü',
  Closed: 'Kapatıldı',
  Rejected: 'Reddedildi',
  Duplicate: 'Mükerrer Kayıt',
  OutOfScope: 'Yetki Dışında'
};

const STATUS_COLORS: Record<string, string> = {
  New: colors.blueBright,
  UnderReview: '#4263eb',
  Assigned: '#7048e8',
  InProgress: colors.amber,
  WaitingForCitizen: colors.orange,
  Resolved: colors.green,
  Closed: colors.green,
  Rejected: colors.red,
  Duplicate: colors.muted,
  OutOfScope: colors.muted
};

export function statusLabel(status: string): string {
  return STATUS_LABELS[status] ?? status;
}

export function statusColor(status: string): string {
  return STATUS_COLORS[status] ?? colors.blueBright;
}

const MONTHS_TR = [
  'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
  'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'
];

// Hermes'te tr-TR Intl her zaman tam olmadığından tarihi elle biçimlendiriyoruz.
export function formatDateTime(value: string): string {
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getDate()} ${MONTHS_TR[d.getMonth()]} ${d.getFullYear()}, ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function formatDate(value: string): string {
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return `${d.getDate()} ${MONTHS_TR[d.getMonth()]} ${d.getFullYear()}`;
}
