import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { Pharmacy } from '../lib/api';

const DEFAULT_CENTER: L.LatLngTuple = [39.0, 35.0];
const DEFAULT_ZOOM = 6;
const ON_DUTY_COLOR = '#2f9e44';
const NORMAL_COLOR = '#8f9cb8';

function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, (c) =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c] as string)
  );
}

export function PharmacyMap({
  pharmacies,
  userLocation,
  focusId
}: {
  pharmacies: Pharmacy[];
  userLocation?: { lat: number; lng: number } | null;
  focusId?: string | null;
}) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<L.Map | null>(null);
  const layerRef = useRef<L.LayerGroup | null>(null);
  const markersRef = useRef<Map<string, L.CircleMarker>>(new Map());

  useEffect(() => {
    if (!containerRef.current || mapRef.current) {
      return;
    }
    const map = L.map(containerRef.current, { scrollWheelZoom: true }).setView(DEFAULT_CENTER, DEFAULT_ZOOM);
    mapRef.current = map;
    layerRef.current = L.layerGroup().addTo(map);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);
    return () => {
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
      const phone = pharmacy.phoneNumber ? `<br/>${escapeHtml(pharmacy.phoneNumber)}` : '';
      const dist = pharmacy.distanceKm != null ? `<br/>${pharmacy.distanceKm} km` : '';
      const marker = L.circleMarker([pharmacy.latitude, pharmacy.longitude], {
        radius: pharmacy.isOnDuty ? 9 : 6,
        color: '#ffffff',
        weight: 2,
        fillColor: pharmacy.isOnDuty ? ON_DUTY_COLOR : NORMAL_COLOR,
        fillOpacity: 0.9
      })
        .bindPopup(
          `<strong>${escapeHtml(pharmacy.name)}</strong><br/>${escapeHtml(pharmacy.district)}` +
            `${pharmacy.isOnDuty ? ' · <b style="color:#2f9e44">Nöbetçi</b>' : ''}${phone}${dist}`
        )
        .addTo(layer);
      markersRef.current.set(pharmacy.id, marker);
      points.push([pharmacy.latitude, pharmacy.longitude]);
    }

    if (userLocation) {
      L.circleMarker([userLocation.lat, userLocation.lng], {
        radius: 8,
        color: '#ffffff',
        weight: 3,
        fillColor: '#1f5fc0',
        fillOpacity: 1
      })
        .bindPopup('Konumun')
        .addTo(layer);
      points.push([userLocation.lat, userLocation.lng]);
    }

    if (points.length > 0) {
      map.fitBounds(L.latLngBounds(points).pad(0.2), { maxZoom: 14 });
    }
  }, [pharmacies, userLocation]);

  useEffect(() => {
    const map = mapRef.current;
    if (!map || !focusId) {
      return;
    }
    const marker = markersRef.current.get(focusId);
    if (marker) {
      map.setView(marker.getLatLng(), Math.max(map.getZoom(), 15), { animate: true });
      marker.openPopup();
    }
  }, [focusId]);

  return <div ref={containerRef} className="insights-map" aria-label="Eczane haritası" />;
}
