// Turkish labels for backend ComplaintStatus values (serialized as strings by the API).
const STATUS_LABELS: Record<string, string> = {
  New: 'Yeni',
  UnderReview: 'İnceleniyor',
  Assigned: 'Birime Atandı',
  InProgress: 'İşlemde',
  WaitingForCitizen: 'Vatandaş Yanıtı Bekleniyor',
  Resolved: 'Çözüldü',
  Closed: 'Kapatıldı',
  Rejected: 'Reddedildi',
  Duplicate: 'Mükerrer Kayıt',
  OutOfScope: 'Yetki Alanı Dışında'
};

export function statusLabel(status: string): string {
  return STATUS_LABELS[status] ?? status;
}

const dateTimeFormat = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'long', timeStyle: 'short' });

export function formatDateTime(value: string): string {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? value : dateTimeFormat.format(parsed);
}
