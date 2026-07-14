-- Toplu içe aktarılan (TR_*) ilçe belediyeleri için standart kategori/birim/kural.
-- Yalnızca henüz kategorisi olmayan TR_* belediyeler için çalışır (idempotent).
DO $$
DECLARE
    m RECORD;
    c_yol uuid; c_cop uuid; c_ayd uuid; c_park uuid; c_traf uuid; c_hay uuid; c_diger uuid;
    d_fen uuid; d_temizlik uuid; d_park uuid; d_zabita uuid; d_vet uuid;
BEGIN
    FOR m IN
        SELECT mm.id FROM public.municipalities mm
        WHERE mm.code LIKE 'TR\_%'
          AND NOT EXISTS (SELECT 1 FROM public.complaint_categories c WHERE c.municipality_id = mm.id)
    LOOP
        c_yol := gen_random_uuid(); c_cop := gen_random_uuid(); c_ayd := gen_random_uuid();
        c_park := gen_random_uuid(); c_traf := gen_random_uuid(); c_hay := gen_random_uuid(); c_diger := gen_random_uuid();

        INSERT INTO public.complaint_categories (id, municipality_id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
        VALUES
            (c_yol,   m.id, 'Yol ve Kaldırım',   'YOL_KALDIRIM',     true, now(), NULL, NULL, false),
            (c_cop,   m.id, 'Çöp ve Temizlik',   'COP_TEMIZLIK',     true, now(), NULL, NULL, false),
            (c_ayd,   m.id, 'Aydınlatma',        'AYDINLATMA',       true, now(), NULL, NULL, false),
            (c_park,  m.id, 'Park ve Bahçe',     'PARK_BAHCE',       true, now(), NULL, NULL, false),
            (c_traf,  m.id, 'Trafik',            'TRAFIK',           true, now(), NULL, NULL, false),
            (c_hay,   m.id, 'Sokak Hayvanları',  'SOKAK_HAYVANLARI', true, now(), NULL, NULL, false),
            (c_diger, m.id, 'Diğer',             'DIGER',            true, now(), NULL, NULL, false);

        d_fen := gen_random_uuid(); d_temizlik := gen_random_uuid(); d_park := gen_random_uuid();
        d_zabita := gen_random_uuid(); d_vet := gen_random_uuid();

        INSERT INTO public.departments (id, municipality_id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
        VALUES
            (d_fen,      m.id, 'Fen İşleri',        'FEN_ISLERI',       true, now(), NULL, NULL, false),
            (d_temizlik, m.id, 'Temizlik İşleri',   'TEMIZLIK_ISLERI',  true, now(), NULL, NULL, false),
            (d_park,     m.id, 'Park ve Bahçeler',  'PARK_BAHCELER',    true, now(), NULL, NULL, false),
            (d_zabita,   m.id, 'Zabıta',            'ZABITA',           true, now(), NULL, NULL, false),
            (d_vet,      m.id, 'Veteriner İşleri',  'VETERINER_ISLERI', true, now(), NULL, NULL, false);

        INSERT INTO public.category_department_rules
            (id, municipality_id, category_id, department_id, default_priority, is_active, created_at, updated_at, deleted_at, is_deleted)
        VALUES
            (gen_random_uuid(), m.id, c_yol,   d_fen,      'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), m.id, c_cop,   d_temizlik, 'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), m.id, c_ayd,   d_fen,      'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), m.id, c_park,  d_park,     'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), m.id, c_traf,  d_zabita,   'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), m.id, c_hay,   d_vet,      'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), m.id, c_diger, d_zabita,   'Low',    true, now(), NULL, NULL, false);
    END LOOP;
END $$;
