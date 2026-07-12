-- Türkiye belediyeleri için demo seed'i.
--
-- ÖNEMLİ: Buradaki sınırlar, her belediyenin GERÇEK merkez koordinatı etrafına
-- çizilmiş YAKLAŞIK (±0.08° ≈ 18 km) kare kutulardır; kadastral/resmi sınır DEĞİLDİR.
-- Amaç, konuma göre doğru belediyenin bulunmasını (ST_Contains) uçtan uca göstermektir.
-- Kutular birbirleriyle çakışmayacak şekilde seçilmiştir. Gerçek üretim için OSM/resmi
-- admin-boundary poligonlarının içe aktarılması gerekir (bir sonraki adım).
--
-- İstanbul, mevcut "Demo Belediyesi" (003_seed) ile temsil edildiğinden burada tekrar
-- eklenmez. Her belediye için standart 7 kategori + 5 birim + kategori→birim kuralları
-- üretilir. Kullanıcılar (her belediyeye ayrı admin + memur) API açılışında
-- DevelopmentDataSeeder tarafından oluşturulur.

DO $$
DECLARE
    m RECORD;
    mid uuid;
    half CONSTANT double precision := 0.08;
    c_yol uuid; c_cop uuid; c_ayd uuid; c_park uuid; c_traf uuid; c_hay uuid; c_diger uuid;
    d_fen uuid; d_temizlik uuid; d_park uuid; d_zabita uuid; d_vet uuid;
BEGIN
    FOR m IN
        SELECT * FROM (VALUES
            ('Adana Belediyesi','ADANA',37.00,35.32),
            ('Adıyaman Belediyesi','ADIYAMAN',37.76,38.28),
            ('Afyonkarahisar Belediyesi','AFYON',38.76,30.54),
            ('Ağrı Belediyesi','AGRI',39.72,43.05),
            ('Amasya Belediyesi','AMASYA',40.65,35.83),
            ('Ankara Belediyesi','ANKARA',39.93,32.85),
            ('Antalya Belediyesi','ANTALYA',36.90,30.70),
            ('Artvin Belediyesi','ARTVIN',41.18,41.82),
            ('Aydın Belediyesi','AYDIN',37.85,27.84),
            ('Balıkesir Belediyesi','BALIKESIR',39.65,27.88),
            ('Bilecik Belediyesi','BILECIK',40.14,29.98),
            ('Bingöl Belediyesi','BINGOL',38.88,40.50),
            ('Bitlis Belediyesi','BITLIS',38.40,42.11),
            ('Bolu Belediyesi','BOLU',40.74,31.61),
            ('Burdur Belediyesi','BURDUR',37.72,30.29),
            ('Bursa Belediyesi','BURSA',40.18,29.07),
            ('Çanakkale Belediyesi','CANAKKALE',40.16,26.41),
            ('Çankırı Belediyesi','CANKIRI',40.60,33.62),
            ('Çorum Belediyesi','CORUM',40.55,34.95),
            ('Denizli Belediyesi','DENIZLI',37.78,29.09),
            ('Diyarbakır Belediyesi','DIYARBAKIR',37.91,40.24),
            ('Edirne Belediyesi','EDIRNE',41.68,26.56),
            ('Elazığ Belediyesi','ELAZIG',38.68,39.22),
            ('Erzincan Belediyesi','ERZINCAN',39.75,39.49),
            ('Erzurum Belediyesi','ERZURUM',39.90,41.27),
            ('Eskişehir Belediyesi','ESKISEHIR',39.78,30.52),
            ('Gaziantep Belediyesi','GAZIANTEP',37.07,37.38),
            ('Giresun Belediyesi','GIRESUN',40.91,38.39),
            ('Gümüşhane Belediyesi','GUMUSHANE',40.46,39.48),
            ('Hakkari Belediyesi','HAKKARI',37.57,43.74),
            ('Hatay Belediyesi','HATAY',36.20,36.16),
            ('Isparta Belediyesi','ISPARTA',37.76,30.55),
            ('Mersin Belediyesi','MERSIN',36.81,34.63),
            ('İzmir Belediyesi','IZMIR',38.42,27.14),
            ('Kars Belediyesi','KARS',40.60,43.10),
            ('Kastamonu Belediyesi','KASTAMONU',41.38,33.78),
            ('Kayseri Belediyesi','KAYSERI',38.73,35.48),
            ('Kırklareli Belediyesi','KIRKLARELI',41.74,27.22),
            ('Kırşehir Belediyesi','KIRSEHIR',39.15,34.16),
            ('Kocaeli Belediyesi','KOCAELI',40.77,29.92),
            ('Konya Belediyesi','KONYA',37.87,32.48),
            ('Kütahya Belediyesi','KUTAHYA',39.42,29.98),
            ('Malatya Belediyesi','MALATYA',38.35,38.31),
            ('Manisa Belediyesi','MANISA',38.61,27.43),
            ('Kahramanmaraş Belediyesi','KMARAS',37.58,36.93),
            ('Mardin Belediyesi','MARDIN',37.31,40.74),
            ('Muğla Belediyesi','MUGLA',37.22,28.36),
            ('Muş Belediyesi','MUS',38.73,41.49),
            ('Nevşehir Belediyesi','NEVSEHIR',38.62,34.71),
            ('Niğde Belediyesi','NIGDE',37.97,34.68),
            ('Ordu Belediyesi','ORDU',40.98,37.88),
            ('Rize Belediyesi','RIZE',41.02,40.52),
            ('Sakarya Belediyesi','SAKARYA',40.78,30.40),
            ('Samsun Belediyesi','SAMSUN',41.29,36.33),
            ('Siirt Belediyesi','SIIRT',37.93,41.94),
            ('Sinop Belediyesi','SINOP',42.03,35.15),
            ('Sivas Belediyesi','SIVAS',39.75,37.02),
            ('Tekirdağ Belediyesi','TEKIRDAG',40.98,27.51),
            ('Tokat Belediyesi','TOKAT',40.31,36.55),
            ('Trabzon Belediyesi','TRABZON',41.00,39.72),
            ('Tunceli Belediyesi','TUNCELI',39.11,39.55),
            ('Şanlıurfa Belediyesi','SURFA',37.17,38.79),
            ('Uşak Belediyesi','USAK',38.68,29.41),
            ('Van Belediyesi','VAN',38.49,43.41),
            ('Yozgat Belediyesi','YOZGAT',39.82,34.81),
            ('Zonguldak Belediyesi','ZONGULDAK',41.45,31.79),
            ('Aksaray Belediyesi','AKSARAY',38.37,34.03),
            ('Bayburt Belediyesi','BAYBURT',40.26,40.22),
            ('Karaman Belediyesi','KARAMAN',37.18,33.22),
            ('Kırıkkale Belediyesi','KIRIKKALE',39.85,33.52),
            ('Batman Belediyesi','BATMAN',37.88,41.13),
            ('Şırnak Belediyesi','SIRNAK',37.52,42.46),
            ('Bartın Belediyesi','BARTIN',41.64,32.34),
            ('Ardahan Belediyesi','ARDAHAN',41.11,42.70),
            ('Iğdır Belediyesi','IGDIR',39.92,44.04),
            ('Yalova Belediyesi','YALOVA',40.65,29.28),
            ('Karabük Belediyesi','KARABUK',41.20,32.63),
            ('Kilis Belediyesi','KILIS',36.72,37.12),
            ('Osmaniye Belediyesi','OSMANIYE',37.07,36.25),
            ('Düzce Belediyesi','DUZCE',40.84,31.16),
            -- Örnek ilçe belediyeleri
            ('Gelibolu Belediyesi','GELIBOLU',40.41,26.67),
            ('Çeşme Belediyesi','CESME',38.32,26.30),
            ('Bodrum Belediyesi','BODRUM',37.03,27.43),
            ('Alanya Belediyesi','ALANYA',36.54,32.00)
        ) AS t(name, code, lat, lng)
    LOOP
        -- Zaten varsa atla (idempotent)
        IF EXISTS (SELECT 1 FROM public.municipalities WHERE code = m.code) THEN
            CONTINUE;
        END IF;

        mid := gen_random_uuid();

        INSERT INTO public.municipalities (id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
        VALUES (mid, m.name, m.code, true, now(), NULL, NULL, false);

        INSERT INTO public.municipality_boundaries
            (id, municipality_id, name, boundary_geometry, is_active, created_at, updated_at, deleted_at, is_deleted)
        VALUES (
            gen_random_uuid(), mid, m.name || ' - yaklaşık sınır',
            ST_Multi(ST_MakeEnvelope(m.lng - half, m.lat - half, m.lng + half, m.lat + half, 4326)),
            true, now(), NULL, NULL, false
        );

        -- Kategoriler
        c_yol   := gen_random_uuid();
        c_cop   := gen_random_uuid();
        c_ayd   := gen_random_uuid();
        c_park  := gen_random_uuid();
        c_traf  := gen_random_uuid();
        c_hay   := gen_random_uuid();
        c_diger := gen_random_uuid();

        INSERT INTO public.complaint_categories (id, municipality_id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
        VALUES
            (c_yol,   mid, 'Yol ve Kaldırım',   'YOL_KALDIRIM',     true, now(), NULL, NULL, false),
            (c_cop,   mid, 'Çöp ve Temizlik',   'COP_TEMIZLIK',     true, now(), NULL, NULL, false),
            (c_ayd,   mid, 'Aydınlatma',        'AYDINLATMA',       true, now(), NULL, NULL, false),
            (c_park,  mid, 'Park ve Bahçe',     'PARK_BAHCE',       true, now(), NULL, NULL, false),
            (c_traf,  mid, 'Trafik',            'TRAFIK',           true, now(), NULL, NULL, false),
            (c_hay,   mid, 'Sokak Hayvanları',  'SOKAK_HAYVANLARI', true, now(), NULL, NULL, false),
            (c_diger, mid, 'Diğer',             'DIGER',            true, now(), NULL, NULL, false);

        -- Birimler
        d_fen      := gen_random_uuid();
        d_temizlik := gen_random_uuid();
        d_park     := gen_random_uuid();
        d_zabita   := gen_random_uuid();
        d_vet      := gen_random_uuid();

        INSERT INTO public.departments (id, municipality_id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
        VALUES
            (d_fen,      mid, 'Fen İşleri',        'FEN_ISLERI',       true, now(), NULL, NULL, false),
            (d_temizlik, mid, 'Temizlik İşleri',   'TEMIZLIK_ISLERI',  true, now(), NULL, NULL, false),
            (d_park,     mid, 'Park ve Bahçeler',  'PARK_BAHCELER',    true, now(), NULL, NULL, false),
            (d_zabita,   mid, 'Zabıta',            'ZABITA',           true, now(), NULL, NULL, false),
            (d_vet,      mid, 'Veteriner İşleri',  'VETERINER_ISLERI', true, now(), NULL, NULL, false);

        -- Kategori → birim yönlendirme kuralları
        INSERT INTO public.category_department_rules
            (id, municipality_id, category_id, department_id, default_priority, is_active, created_at, updated_at, deleted_at, is_deleted)
        VALUES
            (gen_random_uuid(), mid, c_yol,   d_fen,      'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), mid, c_cop,   d_temizlik, 'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), mid, c_ayd,   d_fen,      'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), mid, c_park,  d_park,     'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), mid, c_traf,  d_zabita,   'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), mid, c_hay,   d_vet,      'Normal', true, now(), NULL, NULL, false),
            (gen_random_uuid(), mid, c_diger, d_zabita,   'Low',    true, now(), NULL, NULL, false);
    END LOOP;
END $$;
