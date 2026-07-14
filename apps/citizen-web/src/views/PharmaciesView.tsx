import { useEffect, useState } from 'react';
import {
  District,
  Pharmacy,
  getCurrentPosition,
  getDistricts,
  getNearbyPharmacies,
  getPharmacies,
  getProvinces
} from '../lib/api';
import { PharmacyMap } from '../components/PharmacyMap';

export function PharmaciesView() {
  const [onDutyOnly, setOnDutyOnly] = useState(true);
  const [provinces, setProvinces] = useState<string[]>([]);
  const [province, setProvince] = useState('');
  const [districts, setDistricts] = useState<District[]>([]);
  const [district, setDistrict] = useState('');
  const [pharmacies, setPharmacies] = useState<Pharmacy[]>([]);
  const [userLocation, setUserLocation] = useState<{ lat: number; lng: number } | null>(null);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [nearbyMode, setNearbyMode] = useState(false);

  useEffect(() => {
    getProvinces()
      .then((result) => {
        if (result.success && result.data) setProvinces(result.data);
      })
      .catch(() => undefined);
  }, []);

  // Filter mode: reload whenever province/district/onDuty change (unless in nearby mode).
  useEffect(() => {
    if (nearbyMode) return;
    let cancelled = false;
    setLoading(true);
    setMessage(null);
    getPharmacies({ province: province || undefined, district: district || undefined, onDutyOnly })
      .then((result) => {
        if (cancelled) return;
        setPharmacies(result.success && result.data ? result.data : []);
      })
      .catch(() => !cancelled && setPharmacies([]))
      .finally(() => !cancelled && setLoading(false));
    return () => {
      cancelled = true;
    };
  }, [province, district, onDutyOnly, nearbyMode]);

  function handleProvinceChange(value: string) {
    setNearbyMode(false);
    setUserLocation(null);
    setProvince(value);
    setDistrict('');
    setDistricts([]);
    if (value) {
      getDistricts(value)
        .then((result) => {
          if (result.success && result.data) setDistricts(result.data);
        })
        .catch(() => undefined);
    }
  }

  async function handleUseLocation() {
    setLoading(true);
    setMessage(null);
    try {
      const pos = await getCurrentPosition();
      const loc = { lat: pos.coords.latitude, lng: pos.coords.longitude };
      setUserLocation(loc);
      setNearbyMode(true);
      setProvince('');
      setDistrict('');
      const result = await getNearbyPharmacies(loc.lat, loc.lng, onDutyOnly);
      setPharmacies(result.success && result.data ? result.data : []);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Konum alınamadı.');
    } finally {
      setLoading(false);
    }
  }

  async function handleOnDutyToggle(next: boolean) {
    setOnDutyOnly(next);
    if (nearbyMode && userLocation) {
      setLoading(true);
      const result = await getNearbyPharmacies(userLocation.lat, userLocation.lng, next);
      setPharmacies(result.success && result.data ? result.data : []);
      setLoading(false);
    }
  }

  return (
    <>
      <section className="intro">
        <p>Eczaneler</p>
        <h1>Nöbetçi eczaneler ve en yakınlar.</h1>
      </section>

      <div className="report-form">
        <div className="pharmacy-controls">
          <button type="button" className="secondary-button" onClick={handleUseLocation} disabled={loading}>
            {loading ? 'Yükleniyor...' : 'Konumumu kullan (en yakın)'}
          </button>
          <label className="inline-select">
            İl
            <select value={province} onChange={(event) => handleProvinceChange(event.target.value)}>
              <option value="">Tümü</option>
              {provinces.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
            </select>
          </label>
          <label className="inline-select">
            İlçe
            <select
              value={district}
              onChange={(event) => {
                setNearbyMode(false);
                setUserLocation(null);
                setDistrict(event.target.value);
              }}
              disabled={!province || districts.length === 0}
            >
              <option value="">Tümü</option>
              {districts.map((d) => (
                <option key={d.id} value={d.name.replace(/ Belediyesi$/, '')}>
                  {d.name.replace(/ Belediyesi$/, '')}
                </option>
              ))}
            </select>
          </label>
          <label className="checkbox-label">
            <input type="checkbox" checked={onDutyOnly} onChange={(event) => handleOnDutyToggle(event.target.checked)} />
            Sadece nöbetçi
          </label>
        </div>

        {message && <p className="form-error">{message}</p>}

        <div className="map-legend">
          <span className="legend-item">
            <span className="legend-dot" style={{ background: '#2f9e44' }} /> Nöbetçi
          </span>
          <span className="legend-item">
            <span className="legend-dot" style={{ background: '#8f9cb8' }} /> Diğer eczaneler
          </span>
          {userLocation && (
            <span className="legend-item">
              <span className="legend-dot" style={{ background: '#1f5fc0' }} /> Konumun
            </span>
          )}
        </div>

        <PharmacyMap pharmacies={pharmacies} userLocation={userLocation} />

        <p className="field-hint">{pharmacies.length} eczane gösteriliyor{nearbyMode ? ' (en yakından uzağa)' : ''}.</p>

        <ul className="pharmacy-list">
          {pharmacies.slice(0, 30).map((pharmacy) => (
            <li key={pharmacy.id} className="pharmacy-item">
              <div className="pharmacy-item-top">
                <span className="pharmacy-name">{pharmacy.name}</span>
                {pharmacy.isOnDuty && <span className="status-badge status-resolved">Nöbetçi</span>}
              </div>
              <div className="pharmacy-item-meta">
                <span>
                  {pharmacy.province} · {pharmacy.district}
                </span>
                {pharmacy.distanceKm != null && <span>{pharmacy.distanceKm} km</span>}
                {pharmacy.phoneNumber && <span>{pharmacy.phoneNumber}</span>}
              </div>
            </li>
          ))}
        </ul>
      </div>
    </>
  );
}
