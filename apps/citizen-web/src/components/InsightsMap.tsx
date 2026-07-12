import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { ComplaintMapPoint } from '../lib/api';
import { statusColor, statusLabel } from '../lib/status';

const DEFAULT_CENTER: L.LatLngTuple = [41.05, 29.0];
const DEFAULT_ZOOM = 12;

// Read-only map that plots public complaint points as colour-coded circles.
export function InsightsMap({ points }: { points: ComplaintMapPoint[] }) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<L.Map | null>(null);
  const layerRef = useRef<L.LayerGroup | null>(null);

  useEffect(() => {
    if (!containerRef.current || mapRef.current) {
      return;
    }

    const map = L.map(containerRef.current, { scrollWheelZoom: false }).setView(DEFAULT_CENTER, DEFAULT_ZOOM);
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
    if (!map || !layer) {
      return;
    }

    layer.clearLayers();

    for (const point of points) {
      L.circleMarker([point.latitude, point.longitude], {
        radius: 8,
        color: '#ffffff',
        weight: 2,
        fillColor: statusColor(point.status),
        fillOpacity: 0.9
      })
        .bindPopup(`<strong>${escapeHtml(point.categoryName)}</strong><br/>${escapeHtml(statusLabel(point.status))}`)
        .addTo(layer);
    }

    if (points.length > 0) {
      const bounds = L.latLngBounds(points.map((point) => [point.latitude, point.longitude] as L.LatLngTuple));
      map.fitBounds(bounds.pad(0.3), { maxZoom: 15 });
    }
  }, [points]);

  return <div ref={containerRef} className="insights-map" aria-label="Şehirdeki bildirimler haritası" />;
}

function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, (char) => {
    switch (char) {
      case '&':
        return '&amp;';
      case '<':
        return '&lt;';
      case '>':
        return '&gt;';
      case '"':
        return '&quot;';
      default:
        return '&#39;';
    }
  });
}
