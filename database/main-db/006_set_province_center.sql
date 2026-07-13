-- Mevcut belediyelere (003 Demo + 004 il/ilçeler) il (province) ve merkez koordinatı
-- doldurur. Merkez, mevcut sınır kutusunun ağırlık merkezinden (ST_Centroid) türetilir,
-- böylece koordinatları tekrar elle girmeye gerek kalmaz. İl adı varsayılan olarak
-- belediye adından ("... Belediyesi" eki atılarak) türetilir; ilçe belediyeleri için
-- bağlı olduğu il ayrıca ayarlanır. İdempotenttir (yalnızca NULL olanları doldurur).

-- 1) Merkez koordinat: sınır poligonunun centroid'i
UPDATE public.municipalities m
SET center_latitude = c.lat,
    center_longitude = c.lng
FROM (
    SELECT b.municipality_id,
           ST_Y(ST_Centroid(b.boundary_geometry)) AS lat,
           ST_X(ST_Centroid(b.boundary_geometry)) AS lng
    FROM public.municipality_boundaries b
    WHERE b.is_deleted = false AND b.is_active = true
) c
WHERE m.id = c.municipality_id
  AND m.center_latitude IS NULL;

-- 2) İl adı: belediye adından türet (varsayılan)
UPDATE public.municipalities
SET province = regexp_replace(name, ' Belediyesi$', '')
WHERE province IS NULL;

-- 3) İlçe belediyeleri: bağlı olduğu il
UPDATE public.municipalities SET province = 'Çanakkale' WHERE code = 'GELIBOLU';
UPDATE public.municipalities SET province = 'İzmir'     WHERE code = 'CESME';
UPDATE public.municipalities SET province = 'Muğla'     WHERE code = 'BODRUM';
UPDATE public.municipalities SET province = 'Antalya'   WHERE code = 'ALANYA';

-- 4) Demo belediye (İstanbul civarı örnek)
UPDATE public.municipalities SET province = 'İstanbul' WHERE code = 'DEMO';
