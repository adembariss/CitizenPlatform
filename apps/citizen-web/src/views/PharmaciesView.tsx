import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
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

function SearchIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="11" cy="11" r="7" />
      <path d="m16.5 16.5 4 4" />
    </svg>
  );
}

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
  const [focusedId, setFocusedId] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const mapWrapperRef = useRef<HTMLDivElement | null>(null);

  const visiblePharmacies = useMemo(() => {
    const query = searchTerm.trim().toLocaleLowerCase('tr-TR');
    if (!query) return pharmacies;
    return pharmacies.filter((pharmacy) =>
      [pharmacy.name, pharmacy.province, pharmacy.district, pharmacy.addressText]
        .filter(Boolean)
        .some((value) => value!.toLocaleLowerCase('tr-TR').includes(query))
    );
  }, [pharmacies, searchTerm]);

  const onDutyCount = useMemo(
    () => visiblePharmacies.filter((pharmacy) => pharmacy.isOnDuty).length,
    [visiblePharmacies]
  );

  const handleMapFocus = useCallback((id: string) => setFocusedId(id), []);

  function handleListFocus(id: string) {
    setFocusedId(id);
    mapWrapperRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  useEffect(() => {
    getProvinces()
      .then((result) => {
        if (result.success && result.data) setProvinces(result.data);
      })
      .catch(() => setMessage('İl listesi şu anda yüklenemedi.'));
  }, []);

  useEffect(() => {
    if (nearbyMode) return;
    let cancelled = false;
    setLoading(true);
    setMessage(null);
    setFocusedId(null);
    getPharmacies({ province: province || undefined, district: district || undefined, onDutyOnly })
      .then((result) => {
        if (cancelled) return;
        setPharmacies(result.success && result.data ? result.data : []);
        if (!result.success) setMessage(result.message || 'Eczaneler yüklenemedi.');
      })
      .catch(() => {
        if (!cancelled) {
          setPharmacies([]);
          setMessage('Eczaneler yüklenirken bir sorun oluştu. Lütfen tekrar deneyin.');
        }
      })
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
    setFocusedId(null);
    if (value) {
      getDistricts(value)
        .then((result) => {
          if (result.success && result.data) setDistricts(result.data);
        })
        .catch(() => setMessage('İlçe listesi şu anda yüklenemedi.'));
    }
  }

  async function handleUseLocation() {
    setLoading(true);
    setMessage(null);
    setFocusedId(null);
    try {
      const position = await getCurrentPosition();
      const location = { lat: position.coords.latitude, lng: position.coords.longitude };
      setUserLocation(location);
      setNearbyMode(true);
      setProvince('');
      setDistrict('');
      const result = await getNearbyPharmacies(location.lat, location.lng, onDutyOnly);
      setPharmacies(result.success && result.data ? result.data : []);
      if (!result.success) setMessage(result.message || 'Yakındaki eczaneler bulunamadı.');
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Konum alınamadı.');
    } finally {
      setLoading(false);
    }
  }

  function clearFilters() {
    setNearbyMode(false);
    setUserLocation(null);
    setProvince('');
    setDistrict('');
    setDistricts([]);
    setSearchTerm('');
    setFocusedId(null);
  }

  return (
    <div className="pharmacies-page">
      <section className="pharmacy-intro">
        <div>
          <p>SAĞLIK NOKTALARI · CANLI HARİTA</p>
          <h1>İhtiyacın olan eczaneyi hızlıca bul.</h1>
          <span>Nöbetçi eczaneleri görüntüle, konumuna en yakın sonucu bul ve tek dokunuşla yol tarifi al.</span>
        </div>
        <div className="pharmacy-hero-stats" aria-label="Eczane özeti">
          <span><strong>{loading ? '—' : visiblePharmacies.length}</strong> sonuç</span>
          <span><strong>{loading ? '—' : onDutyCount}</strong> nöbetçi</span>
        </div>
      </section>

      <section className="pharmacy-finder" aria-labelledby="pharmacy-finder-title">
        <div className="pharmacy-filter-heading">
          <div>
            <span className="section-kicker">AKILLI ARAMA</span>
            <h2 id="pharmacy-finder-title">Eczane bul</h2>
            <p>Konumunu kullanabilir veya il ve ilçeye göre arama yapabilirsin.</p>
          </div>
          <button type="button" className="pharmacy-location-button" onClick={handleUseLocation} disabled={loading}>
            <span aria-hidden="true">⌖</span>
            {loading ? 'Yükleniyor…' : 'Yakınımdakileri bul'}
          </button>
        </div>

        <div className="pharmacy-controls">
          <label className="pharmacy-search-field">
            <span className="visually-hidden">Eczane ara</span>
            <SearchIcon />
            <input
              type="search"
              value={searchTerm}
              onChange={(event) => {
                setSearchTerm(event.target.value);
                setFocusedId(null);
              }}
              placeholder="Eczane, ilçe veya adres ara"
            />
          </label>
          <label className="inline-select">
            <span>İl</span>
            <select value={province} onChange={(event) => handleProvinceChange(event.target.value)}>
              <option value="">Tüm iller</option>
              {provinces.map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
          </label>
          <label className="inline-select">
            <span>İlçe</span>
            <select
              value={district}
              onChange={(event) => {
                setNearbyMode(false);
                setUserLocation(null);
                setFocusedId(null);
                setDistrict(event.target.value);
              }}
              disabled={!province || districts.length === 0}
            >
              <option value="">Tüm ilçeler</option>
              {districts.map((item) => {
                const name = item.name.replace(/ Belediyesi$/, '');
                return <option key={item.id} value={name}>{name}</option>;
              })}
            </select>
          </label>
          <button
            type="button"
            className={`pharmacy-duty-toggle${onDutyOnly ? ' is-active' : ''}`}
            aria-pressed={onDutyOnly}
            onClick={() => setOnDutyOnly((current) => !current)}
          >
            <span className="pharmacy-duty-toggle-dot" />
            Sadece nöbetçi
          </button>
        </div>

        <div className="pharmacy-active-filters" aria-live="polite">
          <span>{nearbyMode ? 'Konumuna göre sıralanıyor' : province || 'Türkiye geneli'}</span>
          {district && <span>{district}</span>}
          {searchTerm && <span>“{searchTerm}”</span>}
          {(nearbyMode || province || district || searchTerm) && (
            <button type="button" onClick={clearFilters}>Filtreleri temizle</button>
          )}
        </div>

        {message && <p className="form-error pharmacy-message" role="status">{message}</p>}

        <div className="pharmacy-workspace">
          <div className="pharmacy-map-panel" ref={mapWrapperRef}>
            <div className="pharmacy-map-heading">
              <div>
                <strong>{visiblePharmacies.length} eczane haritada</strong>
                <span>{nearbyMode ? 'En yakından uzağa sıralandı' : 'Bir noktaya dokunarak ayrıntıyı görüntüle'}</span>
              </div>
              <div className="map-legend" aria-label="Harita açıklaması">
                <span className="legend-item"><span className="legend-dot is-duty" /> Nöbetçi</span>
                {!onDutyOnly && <span className="legend-item"><span className="legend-dot is-regular" /> Diğer</span>}
                {userLocation && <span className="legend-item"><span className="legend-dot is-user" /> Konumun</span>}
              </div>
            </div>
            <PharmacyMap
              pharmacies={visiblePharmacies}
              userLocation={userLocation}
              focusId={focusedId}
              onFocusChange={handleMapFocus}
            />
          </div>

          <aside className="pharmacy-results" aria-label="Eczane sonuçları">
            <div className="pharmacy-results-heading">
              <div><strong>Sonuçlar</strong><span>{visiblePharmacies.length} kayıt</span></div>
              {loading && <span className="pharmacy-loading">Güncelleniyor…</span>}
            </div>

            {visiblePharmacies.length === 0 && !loading ? (
              <div className="pharmacy-empty-state">
                <span aria-hidden="true">+</span>
                <strong>Sonuç bulunamadı</strong>
                <p>Arama kelimesini veya bölge filtrelerini değiştirerek tekrar deneyin.</p>
              </div>
            ) : (
              <ul className="pharmacy-list">
                {visiblePharmacies.slice(0, 60).map((pharmacy, index) => (
                  <li key={pharmacy.id} className={focusedId === pharmacy.id ? 'is-focused' : ''}>
                    <button type="button" className="pharmacy-item-button" onClick={() => handleListFocus(pharmacy.id)}>
                      <span className="pharmacy-item-index">{nearbyMode ? index + 1 : 'E'}</span>
                      <span className="pharmacy-item-content">
                        <span className="pharmacy-item-top">
                          <strong className="pharmacy-name">{pharmacy.name}</strong>
                          {pharmacy.isOnDuty && <span className="pharmacy-duty-badge">Nöbetçi</span>}
                        </span>
                        <span className="pharmacy-item-region">{pharmacy.province} · {pharmacy.district}</span>
                        {pharmacy.addressText && <span className="pharmacy-address">{pharmacy.addressText}</span>}
                        <span className="pharmacy-item-meta">
                          {pharmacy.distanceKm != null && <span>{pharmacy.distanceKm} km</span>}
                          {pharmacy.phoneNumber && <span>{pharmacy.phoneNumber}</span>}
                        </span>
                      </span>
                    </button>
                    <div className="pharmacy-item-actions">
                      {pharmacy.phoneNumber && <a href={`tel:${pharmacy.phoneNumber}`}>Ara</a>}
                      <a
                        href={`https://www.google.com/maps/dir/?api=1&destination=${pharmacy.latitude},${pharmacy.longitude}`}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        Yol tarifi
                      </a>
                    </div>
                  </li>
                ))}
              </ul>
            )}
            {visiblePharmacies.length > 60 && (
              <p className="pharmacy-result-limit">İlk 60 sonuç gösteriliyor. Daha hızlı bulmak için aramayı daraltın.</p>
            )}
          </aside>
        </div>
      </section>
    </div>
  );
}
