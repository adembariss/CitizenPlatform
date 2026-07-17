import { useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import markerIconUrl from 'leaflet/dist/images/marker-icon.png';
import markerIcon2xUrl from 'leaflet/dist/images/marker-icon-2x.png';
import markerShadowUrl from 'leaflet/dist/images/marker-shadow.png';

const DEFAULT_CENTER: L.LatLngTuple = [41.05, 29.0];
const DEFAULT_ZOOM = 13;

const markerIcon = L.icon({
  iconUrl: markerIconUrl,
  iconRetinaUrl: markerIcon2xUrl,
  shadowUrl: markerShadowUrl,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41]
});

export type LatLng = {
  latitude: number;
  longitude: number;
};

type LocationMapProps = {
  position: LatLng | null;
  onPick: (position: LatLng) => void;
};

export function LocationMap({ position, onPick }: LocationMapProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<L.Map | null>(null);
  const markerRef = useRef<L.Marker | null>(null);
  const onPickRef = useRef(onPick);
  const [isExpanded, setIsExpanded] = useState(false);
  onPickRef.current = onPick;

  useEffect(() => {
    if (!containerRef.current || mapRef.current) {
      return;
    }

    const map = L.map(containerRef.current, { scrollWheelZoom: true, zoomControl: false }).setView(DEFAULT_CENTER, DEFAULT_ZOOM);
    mapRef.current = map;

    L.control.zoom({ position: 'topright' }).addTo(map);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    map.on('click', (event: L.LeafletMouseEvent) => {
      onPickRef.current({ latitude: event.latlng.lat, longitude: event.latlng.lng });
    });

    return () => {
      map.remove();
      mapRef.current = null;
      markerRef.current = null;
    };
  }, []);

  useEffect(() => {
    const map = mapRef.current;
    if (!map) {
      return;
    }

    if (!position) {
      markerRef.current?.remove();
      markerRef.current = null;
      return;
    }

    const latLng: L.LatLngTuple = [position.latitude, position.longitude];

    if (!markerRef.current) {
      const marker = L.marker(latLng, { icon: markerIcon, draggable: true }).addTo(map);
      marker.on('dragend', () => {
        const dragged = marker.getLatLng();
        onPickRef.current({ latitude: dragged.lat, longitude: dragged.lng });
      });
      markerRef.current = marker;
    } else {
      markerRef.current.setLatLng(latLng);
    }

    map.panTo(latLng);
  }, [position]);

  useEffect(() => {
    const frame = window.requestAnimationFrame(() => mapRef.current?.invalidateSize());
    if (isExpanded) {
      document.body.classList.add('map-expanded-open');
    } else {
      document.body.classList.remove('map-expanded-open');
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setIsExpanded(false);
    };
    window.addEventListener('keydown', handleKeyDown);

    return () => {
      window.cancelAnimationFrame(frame);
      window.removeEventListener('keydown', handleKeyDown);
      document.body.classList.remove('map-expanded-open');
    };
  }, [isExpanded]);

  return (
    <div className={`location-map-shell${isExpanded ? ' is-expanded' : ''}`}>
      <div ref={containerRef} className="location-map" aria-label="Konum seçme haritası" />
      <span className="location-map-instruction">Sorunun bulunduğu noktaya tıklayın</span>
      <button
        type="button"
        className="location-expand-button"
        onClick={() => setIsExpanded((expanded) => !expanded)}
        aria-expanded={isExpanded}
      >
        <ExpandIcon collapsed={!isExpanded} /> {isExpanded ? 'Haritayı küçült' : 'Haritayı büyüt'}
      </button>
    </div>
  );
}

function ExpandIcon({ collapsed }: { collapsed: boolean }) {
  return collapsed
    ? <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 3H3v5M16 3h5v5M21 16v5h-5M3 16v5h5" /></svg>
    : <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M9 3v6H3M15 3v6h6M15 21v-6h6M9 21v-6H3" /></svg>;
}
