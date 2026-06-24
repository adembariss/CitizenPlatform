-- Demo data for local development. The boundary below is a deliberately simple
-- sample polygon around Istanbul and must not be used as a real municipality boundary.

DO $$
DECLARE
    demo_municipality_id uuid := '11111111-1111-1111-1111-111111111111';
    road_category_id uuid := '22222222-2222-2222-2222-222222222201';
    cleaning_category_id uuid := '22222222-2222-2222-2222-222222222202';
    lighting_category_id uuid := '22222222-2222-2222-2222-222222222203';
    parks_category_id uuid := '22222222-2222-2222-2222-222222222204';
    traffic_category_id uuid := '22222222-2222-2222-2222-222222222205';
    animals_category_id uuid := '22222222-2222-2222-2222-222222222206';
    other_category_id uuid := '22222222-2222-2222-2222-222222222207';
    public_works_department_id uuid := '33333333-3333-3333-3333-333333333301';
    cleaning_department_id uuid := '33333333-3333-3333-3333-333333333302';
    parks_department_id uuid := '33333333-3333-3333-3333-333333333303';
    enforcement_department_id uuid := '33333333-3333-3333-3333-333333333304';
    veterinary_department_id uuid := '33333333-3333-3333-3333-333333333305';
BEGIN
    INSERT INTO public.municipalities (id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
    VALUES (demo_municipality_id, 'Demo Belediyesi', 'DEMO', true, now(), NULL, NULL, false)
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO public.municipality_boundaries
        (id, municipality_id, name, boundary_geometry, is_active, created_at, updated_at, deleted_at, is_deleted)
    VALUES
        (
            '11111111-1111-1111-1111-111111111112',
            demo_municipality_id,
            'Demo sınır - İstanbul civarı örnek polygon',
            ST_Multi(ST_GeomFromText('POLYGON((28.85 40.95, 29.20 40.95, 29.20 41.20, 28.85 41.20, 28.85 40.95))', 4326)),
            true,
            now(),
            NULL,
            NULL,
            false
        )
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO public.complaint_categories (id, municipality_id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
    VALUES
        (road_category_id, demo_municipality_id, 'Yol ve Kaldırım', 'YOL_KALDIRIM', true, now(), NULL, NULL, false),
        (cleaning_category_id, demo_municipality_id, 'Çöp ve Temizlik', 'COP_TEMIZLIK', true, now(), NULL, NULL, false),
        (lighting_category_id, demo_municipality_id, 'Aydınlatma', 'AYDINLATMA', true, now(), NULL, NULL, false),
        (parks_category_id, demo_municipality_id, 'Park ve Bahçe', 'PARK_BAHCE', true, now(), NULL, NULL, false),
        (traffic_category_id, demo_municipality_id, 'Trafik', 'TRAFIK', true, now(), NULL, NULL, false),
        (animals_category_id, demo_municipality_id, 'Sokak Hayvanları', 'SOKAK_HAYVANLARI', true, now(), NULL, NULL, false),
        (other_category_id, demo_municipality_id, 'Diğer', 'DIGER', true, now(), NULL, NULL, false)
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO public.departments (id, municipality_id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
    VALUES
        (public_works_department_id, demo_municipality_id, 'Fen İşleri', 'FEN_ISLERI', true, now(), NULL, NULL, false),
        (cleaning_department_id, demo_municipality_id, 'Temizlik İşleri', 'TEMIZLIK_ISLERI', true, now(), NULL, NULL, false),
        (parks_department_id, demo_municipality_id, 'Park ve Bahçeler', 'PARK_BAHCELER', true, now(), NULL, NULL, false),
        (enforcement_department_id, demo_municipality_id, 'Zabıta', 'ZABITA', true, now(), NULL, NULL, false),
        (veterinary_department_id, demo_municipality_id, 'Veteriner İşleri', 'VETERINER_ISLERI', true, now(), NULL, NULL, false)
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO public.category_department_rules
        (id, municipality_id, category_id, department_id, default_priority, is_active, created_at, updated_at, deleted_at, is_deleted)
    VALUES
        ('44444444-4444-4444-4444-444444444401', demo_municipality_id, road_category_id, public_works_department_id, 'Normal', true, now(), NULL, NULL, false),
        ('44444444-4444-4444-4444-444444444402', demo_municipality_id, cleaning_category_id, cleaning_department_id, 'Normal', true, now(), NULL, NULL, false),
        ('44444444-4444-4444-4444-444444444403', demo_municipality_id, lighting_category_id, public_works_department_id, 'Normal', true, now(), NULL, NULL, false),
        ('44444444-4444-4444-4444-444444444404', demo_municipality_id, parks_category_id, parks_department_id, 'Normal', true, now(), NULL, NULL, false),
        ('44444444-4444-4444-4444-444444444405', demo_municipality_id, traffic_category_id, enforcement_department_id, 'Normal', true, now(), NULL, NULL, false),
        ('44444444-4444-4444-4444-444444444406', demo_municipality_id, animals_category_id, veterinary_department_id, 'Normal', true, now(), NULL, NULL, false),
        ('44444444-4444-4444-4444-444444444407', demo_municipality_id, other_category_id, enforcement_department_id, 'Low', true, now(), NULL, NULL, false)
    ON CONFLICT (id) DO NOTHING;
END $$;
