-- Örnek eczane verisi (demo). Her belediyenin merkezine bir eczane; İstanbul ilçelerine
-- ek 2 eczane. Yaklaşık her 3 eczaneden biri "nöbetçi" olarak işaretlenir.
-- İdempotent: pharmacies tablosunda kayıt varsa hiçbir şey yapmaz.
DO $$
DECLARE
    m RECORD;
    i integer := 0;
    dist text;
BEGIN
    IF (SELECT COUNT(*) FROM public.pharmacies) > 0 THEN
        RETURN;
    END IF;

    FOR m IN
        SELECT name, center_latitude AS lat, center_longitude AS lng, province
        FROM public.municipalities
        WHERE center_latitude IS NOT NULL AND province IS NOT NULL
        ORDER BY name
    LOOP
        i := i + 1;
        dist := regexp_replace(m.name, ' Belediyesi$', '');
        INSERT INTO public.pharmacies
            (id, name, province, district, address_text, phone_number, latitude, longitude, is_on_duty, created_at, updated_at, deleted_at, is_deleted)
        VALUES
            (gen_random_uuid(), dist || ' Merkez Eczanesi', m.province, dist, dist || ' merkez', NULL,
             m.lat + 0.003, m.lng + 0.003, (i % 3 = 0), now(), NULL, NULL, false);
    END LOOP;

    -- İstanbul ilçelerine ek eczaneler (biri nöbetçi)
    FOR m IN
        SELECT name, center_latitude AS lat, center_longitude AS lng
        FROM public.municipalities
        WHERE province = 'İstanbul' AND center_latitude IS NOT NULL
    LOOP
        dist := regexp_replace(m.name, ' Belediyesi$', '');
        INSERT INTO public.pharmacies
            (id, name, province, district, address_text, phone_number, latitude, longitude, is_on_duty, created_at, updated_at, deleted_at, is_deleted)
        VALUES
            (gen_random_uuid(), dist || ' Sağlık Eczanesi', 'İstanbul', dist, dist || ' cad.', NULL,
             m.lat - 0.004, m.lng - 0.003, true, now(), NULL, NULL, false),
            (gen_random_uuid(), dist || ' Yaşam Eczanesi', 'İstanbul', dist, dist, NULL,
             m.lat + 0.002, m.lng - 0.005, false, now(), NULL, NULL, false);
    END LOOP;
END $$;
