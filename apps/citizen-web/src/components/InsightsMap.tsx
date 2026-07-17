import { useCallback, useEffect, useRef, useState } from 'react';
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
  const [isExpanded, setIsExpanded] = useState(false);
  const [zoomLevel, setZoomLevel] = useState(DEFAULT_ZOOM);

  const fitPoints = useCallback(() => {
    const map = mapRef.current;
    if (!map) {
      return;
    }

    if (points.length === 0) {
      map.setView(DEFAULT_CENTER, DEFAULT_ZOOM);
      return;
    }

    const bounds = L.latLngBounds(points.map((point) => [point.latitude, point.longitude] as L.LatLngTuple));
    map.fitBounds(bounds.pad(0.3), { maxZoom: 15 });
  }, [points]);

  useEffect(() => {
    if (!containerRef.current || mapRef.current) {
      return;
    }

    const map = L.map(containerRef.current, {
      scrollWheelZoom: true,
      zoomControl: false,
      zoomSnap: 0.5,
      wheelDebounceTime: 30,
      wheelPxPerZoomLevel: 90
    }).setView(DEFAULT_CENTER, DEFAULT_ZOOM);
    mapRef.current = map;
    layerRef.current = L.layerGroup().addTo(map);

    L.control.zoom({ position: 'topright' }).addTo(map);
    map.on('zoomend', () => setZoomLevel(map.getZoom()));

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
      fitPoints();
    }
  }, [fitPoints, points]);

  useEffect(() => {
    const map = mapRef.current;
    if (!map) {
      return;
    }

    const frame = window.requestAnimationFrame(() => {
      map.invalidateSize();
      fitPoints();
    });

    if (isExpanded) {
      document.body.classList.add('map-expanded-open');
    } else {
      document.body.classList.remove('map-expanded-open');
    }

    return () => {
      window.cancelAnimationFrame(frame);
      document.body.classList.remove('map-expanded-open');
    };
  }, [fitPoints, isExpanded]);

  useEffect(() => {
    if (!isExpanded) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsExpanded(false);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isExpanded]);

  return (
    <div className={`insights-map-shell${isExpanded ? ' is-expanded' : ''}`}>
      <div className="insights-map-toolbar">
        <div className="map-toolbar-summary">
          <span className="map-live-badge"><span aria-hidden="true" /> {points.length} bildirim noktası</span>
          <span className="map-zoom-label">Yakınlık {zoomLevel.toFixed(1)} · Kaydırarak yakınlaştır</span>
        </div>
        <div className="map-toolbar-actions">
          <button type="button" onClick={fitPoints} aria-label="Tüm bildirim noktalarını göster">
            <FitIcon /> <span>Tümünü göster</span>
          </button>
          <button
            type="button"
            className="map-expand-button"
            onClick={() => setIsExpanded((expanded) => !expanded)}
            aria-expanded={isExpanded}
          >
            {isExpanded ? <CollapseIcon /> : <ExpandIcon />}
            <span>{isExpanded ? 'Küçült' : 'Haritayı büyüt'}</span>
          </button>
        </div>
      </div>
      <div className="insights-map-stage">
        <div ref={containerRef} className="insights-map" aria-label="Şehirdeki bildirimler haritası" />
        <div className="map-scroll-hint" aria-hidden="true">Fare tekerleği ile yakınlaştır</div>
      </div>
    </div>
  );
}

function FitIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 9V4h5M15 4h5v5M20 15v5h-5M9 20H4v-5" /></svg>;
}

function ExpandIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 3H3v5M16 3h5v5M21 16v5h-5M3 16v5h5" /></svg>;
}

function CollapseIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M9 3v6H3M15 3v6h6M15 21v-6h6M9 21v-6H3" /></svg>;
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
