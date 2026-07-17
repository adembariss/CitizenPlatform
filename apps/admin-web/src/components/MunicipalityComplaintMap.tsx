import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { AdminComplaintListItem, MunicipalityMapContext } from '../lib/api';
import { statusLabel } from '../lib/labels';

const DEFAULT_CENTER: L.LatLngTuple = [41.015, 29.0];
const DEFAULT_ZOOM = 11;
type BoundaryGeometry =
  | { type: 'Polygon'; coordinates: number[][][] }
  | { type: 'MultiPolygon'; coordinates: number[][][][] };

export function MunicipalityComplaintMap({
  complaints,
  context
}: {
  complaints: AdminComplaintListItem[];
  context: MunicipalityMapContext | null;
}) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<L.Map | null>(null);
  const complaintLayerRef = useRef<L.LayerGroup | null>(null);
  const boundaryLayerRef = useRef<L.LayerGroup | null>(null);
  const hasBoundary = Boolean(context?.boundaryGeoJson);

  useEffect(() => {
    if (!containerRef.current || mapRef.current) return;

    const map = L.map(containerRef.current, {
      scrollWheelZoom: true,
      zoomControl: false,
      zoomSnap: 0.5,
      maxBoundsViscosity: 1
    }).setView(DEFAULT_CENTER, DEFAULT_ZOOM);
    mapRef.current = map;
    boundaryLayerRef.current = L.layerGroup().addTo(map);
    complaintLayerRef.current = L.layerGroup().addTo(map);

    L.control.zoom({ position: 'topright' }).addTo(map);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    return () => {
      map.remove();
      mapRef.current = null;
      complaintLayerRef.current = null;
      boundaryLayerRef.current = null;
    };
  }, []);

  useEffect(() => {
    const map = mapRef.current;
    const layer = boundaryLayerRef.current;
    if (!map || !layer) return;

    layer.clearLayers();
    if (!context?.boundaryGeoJson) {
      map.setMinZoom(5);
      map.setMaxBounds([[-85, -180], [85, 180]]);
      if (context?.centerLatitude != null && context.centerLongitude != null && complaints.length === 0) {
        map.setView([context.centerLatitude, context.centerLongitude], 12);
      }
      return;
    }

    try {
      const geometry = JSON.parse(context.boundaryGeoJson) as BoundaryGeometry;
      const holes = getExteriorRings(geometry);
      const boundary = L.geoJSON(geometry, {
        style: {
          color: '#1557a5',
          weight: 3,
          opacity: 1,
          fillColor: '#2d83d5',
          fillOpacity: 0.08,
          dashArray: '7 5'
        }
      });
      const bounds = boundary.getBounds();
      const maskBounds = bounds.pad(8);
      const maskRing: L.LatLngTuple[] = [
        [maskBounds.getSouth(), maskBounds.getWest()],
        [maskBounds.getNorth(), maskBounds.getWest()],
        [maskBounds.getNorth(), maskBounds.getEast()],
        [maskBounds.getSouth(), maskBounds.getEast()]
      ];
      L.polygon([maskRing, ...holes], {
        stroke: false,
        fillColor: '#e8eef5',
        fillOpacity: 0.97,
        fillRule: 'evenodd',
        interactive: false
      }).addTo(layer);
      boundary.addTo(layer);
      if (bounds.isValid()) {
        const paddedBounds = bounds.pad(0.08);
        map.setMaxBounds(paddedBounds);
        map.fitBounds(bounds, { padding: [22, 22], animate: false });
        map.setMinZoom(map.getZoom());
      }
    } catch {
      if (context.centerLatitude != null && context.centerLongitude != null) {
        map.setView([context.centerLatitude, context.centerLongitude], 12);
      }
    }

    window.requestAnimationFrame(() => map.invalidateSize());
  }, [complaints.length, context]);

  useEffect(() => {
    const map = mapRef.current;
    const layer = complaintLayerRef.current;
    if (!map || !layer) return;

    layer.clearLayers();
    for (const complaint of complaints) {
      L.circleMarker([complaint.latitude, complaint.longitude], {
        radius: 9,
        color: '#ffffff',
        weight: 2.5,
        fillColor: statusColor(complaint.status),
        fillOpacity: 0.94
      })
        .bindPopup(
          `<div class="admin-map-popup"><strong>${escapeHtml(complaint.title)}</strong>` +
          `<span>${escapeHtml(complaint.trackingCode)}</span>` +
          `<small>${escapeHtml(complaint.categoryName)} · ${escapeHtml(statusLabel(complaint.status))}</small></div>`
        )
        .addTo(layer);
    }

    if (!hasBoundary) {
      if (complaints.length === 1) {
        map.setView([complaints[0].latitude, complaints[0].longitude], 15);
      } else if (complaints.length > 1) {
        const bounds = L.latLngBounds(complaints.map((item) => [item.latitude, item.longitude] as L.LatLngTuple));
        map.fitBounds(bounds.pad(0.25), { maxZoom: 15 });
      }
    }

    window.requestAnimationFrame(() => map.invalidateSize());
  }, [complaints, hasBoundary]);

  return (
    <div className={`municipality-map-stage${hasBoundary ? ' has-boundary' : ''}`}>
      <div ref={containerRef} className="municipality-map" aria-label="Belediyenin açık talep haritası" />
      {hasBoundary && (
        <div className="municipality-boundary-lock" aria-label="Harita belediye sınırına kilitli">
          <span aria-hidden="true">⌁</span>
          Yalnızca {context?.municipalityName} hizmet alanı
        </div>
      )}
      {complaints.length === 0 && (
        <div className="municipality-map-empty">
          <span aria-hidden="true">✓</span>
          <strong>Açık konumsal talep yok</strong>
          <small>Yeni bir başvuru geldiğinde haritada burada görünecek.</small>
        </div>
      )}
      <div className="municipality-map-legend" aria-label="Harita açıklaması">
        <span><i className="map-dot map-dot-new" /> Yeni</span>
        <span><i className="map-dot map-dot-active" /> İşlemde</span>
        <span><i className="map-dot map-dot-waiting" /> Bekliyor</span>
      </div>
    </div>
  );
}

function getExteriorRings(geometry: BoundaryGeometry): L.LatLngTuple[][] {
  const polygons = geometry.type === 'Polygon' ? [geometry.coordinates] : geometry.coordinates;
  return polygons
    .map((polygon) => polygon[0] ?? [])
    .filter((ring) => ring.length >= 3)
    .map((ring) => ring.map(([longitude, latitude]) => [latitude, longitude] as L.LatLngTuple));
}

function statusColor(status: string): string {
  const normalized = status.toLowerCase();
  if (normalized === 'inprogress' || normalized === 'assigned') return '#6750c5';
  if (normalized === 'underreview' || normalized === 'waitingforcitizen') return '#e28a12';
  return '#2775d7';
}

function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, (character) => {
    if (character === '&') return '&amp;';
    if (character === '<') return '&lt;';
    if (character === '>') return '&gt;';
    if (character === '"') return '&quot;';
    return '&#39;';
  });
}
