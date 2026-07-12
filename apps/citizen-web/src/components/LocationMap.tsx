import { useEffect, useRef } from 'react';
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
  onPickRef.current = onPick;

  useEffect(() => {
    if (!containerRef.current || mapRef.current) {
      return;
    }

    const map = L.map(containerRef.current).setView(DEFAULT_CENTER, DEFAULT_ZOOM);
    mapRef.current = map;

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

  return <div ref={containerRef} className="location-map" aria-label="Konum seçme haritası" />;
}
