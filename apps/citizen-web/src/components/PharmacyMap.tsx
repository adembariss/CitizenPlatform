import { useCallback, useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { Pharmacy } from '../lib/api';

const DEFAULT_CENTER: L.LatLngTuple = [39.0, 35.0];
const DEFAULT_ZOOM = 6;
const ON_DUTY_COLOR = '#16a36a';
const NORMAL_COLOR = '#7b8da9';

function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, (character) =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[character] as string)
  );
}

export function PharmacyMap({
  pharmacies,
  userLocation,
  focusId,
  onFocusChange
}: {
  pharmacies: Pharmacy[];
  userLocation?: { lat: number; lng: number } | null;
  focusId?: string | null;
  onFocusChange?: (id: string) => void;
}) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<L.Map | null>(null);
  const layerRef = useRef<L.LayerGroup | null>(null);
  const boundsRef = useRef<L.LatLngBounds | null>(null);
  const markersRef = useRef<Map<string, L.CircleMarker>>(new Map());
  const [isExpanded, setIsExpanded] = useState(false);

  const showAllPoints = useCallback(() => {
    const map = mapRef.current;
    if (!map) return;
    if (boundsRef.current?.isValid()) {
      map.fitBounds(boundsRef.current.pad(0.18), { maxZoom: 14, animate: true });
    } else {
      map.setView(DEFAULT_CENTER, DEFAULT_ZOOM);
    }
  }, []);

  useEffect(() => {
    if (!containerRef.current || mapRef.current) return;

    const map = L.map(containerRef.current, {
      scrollWheelZoom: true,
      zoomControl: false
    }).setView(DEFAULT_CENTER, DEFAULT_ZOOM);
    mapRef.current = map;
    layerRef.current = L.layerGroup().addTo(map);
    L.control.zoom({ position: 'topright' }).addTo(map);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    // Mobilde/responsive geçişlerde Leaflet konteyner boyutunu geç algılayıp haritayı
    // yanlış boyutta (gri/yarım) çizebilir. Konteyner boyutu değiştikçe düzeltiyoruz.
    const initialFix = window.setTimeout(() => map.invalidateSize(), 200);
    const resizeObserver = new ResizeObserver(() => map.invalidateSize());
    resizeObserver.observe(containerRef.current);

    return () => {
      window.clearTimeout(initialFix);
      resizeObserver.disconnect();
      map.remove();
      mapRef.current = null;
      layerRef.current = null;
    };
  }, []);

  useEffect(() => {
    const map = mapRef.current;
    const layer = layerRef.current;
    if (!map || !layer) return;

    layer.clearLayers();
    markersRef.current.clear();
    const points: L.LatLngTuple[] = [];

    for (const pharmacy of pharmacies) {
      const phone = pharmacy.phoneNumber
        ? `<a href="tel:${escapeHtml(pharmacy.phoneNumber)}">${escapeHtml(pharmacy.phoneNumber)}</a>`
        : '';
      const address = pharmacy.addressText ? `<p>${escapeHtml(pharmacy.addressText)}</p>` : '';
      const distance = pharmacy.distanceKm != null ? `<span>${pharmacy.distanceKm} km uzakta</span>` : '';
      const directionsUrl = `https://www.google.com/maps/dir/?api=1&destination=${pharmacy.latitude},${pharmacy.longitude}`;
      const marker = L.circleMarker([pharmacy.latitude, pharmacy.longitude], {
        radius: pharmacy.isOnDuty ? 9 : 7,
        color: '#ffffff',
        weight: 3,
        fillColor: pharmacy.isOnDuty ? ON_DUTY_COLOR : NORMAL_COLOR,
        fillOpacity: 1
      })
        .bindTooltip(escapeHtml(pharmacy.name), { direction: 'top', offset: [0, -8] })
        .bindPopup(
          `<div class="pharmacy-map-popup">` +
            `<div class="pharmacy-popup-heading"><strong>${escapeHtml(pharmacy.name)}</strong>${
              pharmacy.isOnDuty ? '<b>Nöbetçi</b>' : ''
            }</div>` +
            `<span>${escapeHtml(pharmacy.province)} · ${escapeHtml(pharmacy.district)}</span>` +
            address +
            `<div class="pharmacy-popup-meta">${distance}${phone}</div>` +
            `<a class="pharmacy-popup-route" href="${directionsUrl}" target="_blank" rel="noopener noreferrer">Yol tarifi al →</a>` +
          `</div>`,
          { minWidth: 220 }
        )
        .on('click', () => onFocusChange?.(pharmacy.id))
        .addTo(layer);

      markersRef.current.set(pharmacy.id, marker);
      points.push([pharmacy.latitude, pharmacy.longitude]);
    }

    if (userLocation) {
      L.circleMarker([userLocation.lat, userLocation.lng], {
        radius: 9,
        color: '#ffffff',
        weight: 3,
        fillColor: '#1f5fc0',
        fillOpacity: 1
      })
        .bindPopup('<strong>Şu anki konumun</strong>')
        .addTo(layer);
      points.push([userLocation.lat, userLocation.lng]);
    }

    boundsRef.current = points.length > 0 ? L.latLngBounds(points) : null;
    showAllPoints();
  }, [onFocusChange, pharmacies, showAllPoints, userLocation]);

  useEffect(() => {
    const map = mapRef.current;
    for (const [id, marker] of markersRef.current) {
      const pharmacy = pharmacies.find((item) => item.id === id);
      const selected = id === focusId;
      marker.setStyle({
        radius: selected ? 12 : pharmacy?.isOnDuty ? 9 : 7,
        weight: selected ? 4 : 3,
        color: selected ? '#123f86' : '#ffffff'
      });
    }

    if (!map || !focusId) return;
    const marker = markersRef.current.get(focusId);
    if (marker) {
      map.setView(marker.getLatLng(), Math.max(map.getZoom(), 15), { animate: true });
      marker.openPopup();
    }
  }, [focusId, pharmacies]);

  useEffect(() => {
    document.body.classList.toggle('map-expanded-open', isExpanded);
    const timer = window.setTimeout(() => mapRef.current?.invalidateSize(), 120);
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setIsExpanded(false);
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => {
      window.clearTimeout(timer);
      window.removeEventListener('keydown', handleKeyDown);
      document.body.classList.remove('map-expanded-open');
    };
  }, [isExpanded]);

  return (
    <div className={`pharmacy-map-shell${isExpanded ? ' is-expanded' : ''}`}>
      <div className="pharmacy-map-toolbar" aria-label="Harita araçları">
        <button type="button" onClick={showAllPoints}>Tüm noktalar</button>
        <button type="button" onClick={() => setIsExpanded((current) => !current)} aria-expanded={isExpanded}>
          {isExpanded ? 'Haritayı küçült' : 'Haritayı büyüt'}
        </button>
      </div>
      <div ref={containerRef} className="pharmacy-map" aria-label="Eczane haritası" />
      {pharmacies.length === 0 && (
        <div className="pharmacy-map-empty">
          <strong>Bu filtrede eczane bulunamadı</strong>
          <span>İl, ilçe veya nöbetçi filtresini değiştirin.</span>
        </div>
      )}
      <div className="pharmacy-map-tip">Kaydırarak yakınlaştır · Noktaya dokunarak ayrıntıyı aç</div>
    </div>
  );
}
