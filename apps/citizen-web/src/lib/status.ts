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

const HISTORY_NOTE_LABELS: Record<string, string> = {
  'complaint created.': 'Başvuru oluşturuldu.',
  'complaint assigned to department.': 'Başvuru ilgili birime atandı.'
};

export function statusHistoryNote(note: string): string {
  return HISTORY_NOTE_LABELS[note.trim().toLocaleLowerCase('en-US')] ?? note;
}

// Marker colours for the public complaints map, tuned to the blue/cyan brand
// with green for resolved and red for rejected outcomes.
const STATUS_COLORS: Record<string, string> = {
  New: '#1c7ed6',
  UnderReview: '#4263eb',
  Assigned: '#7048e8',
  InProgress: '#f59f00',
  WaitingForCitizen: '#f76707',
  Resolved: '#2f9e44',
  Closed: '#2f9e44',
  Rejected: '#e03131',
  Duplicate: '#868e96',
  OutOfScope: '#868e96'
};

export function statusColor(status: string): string {
  return STATUS_COLORS[status] ?? '#1c7ed6';
}

const dateTimeFormat = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'long', timeStyle: 'short' });

export function formatDateTime(value: string): string {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? value : dateTimeFormat.format(parsed);
}
