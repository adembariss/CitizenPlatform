type ComplaintLocation = {
  addressText: string | null;
  latitude: number;
  longitude: number;
};

export function reporterLabel(fullName: string | null): string {
  return fullName?.trim() || 'Anonim';
}

export function complaintAddress(complaint: ComplaintLocation): string {
  const address = complaint.addressText?.trim();
  if (address) {
    return address;
  }

  if (Number.isFinite(complaint.latitude) && Number.isFinite(complaint.longitude)) {
    return `${complaint.latitude.toFixed(5)}, ${complaint.longitude.toFixed(5)}`;
  }

  return 'Adres belirtilmemiş';
}
