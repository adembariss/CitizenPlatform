-- İstanbul'un 39 ilçe belediyesi.
--
-- Bu belediyelere kasıtlı olarak COĞRAFİ SINIR verilmez; İstanbul ilçeleri çok
-- yoğun/bitişik olduğundan yaklaşık kutu sınırlar yanlış yönlendirmeye yol açar.
-- Bunun yerine vatandaş "İl → İlçe" adres seçicisinden ilçeyi seçer ve belediye
-- DOĞRUDAN belirlenir (ST_Contains gerekmez). Şikayet konumu olarak ilçe merkez
-- koordinatı kullanılır. Gerçek/resmi ilçe poligonlarının import'u bir sonraki adımdır.

DO $$
DECLARE
    m RECORD;
    mid uuid;
    c_yol uuid; c_cop uuid; c_ayd uuid; c_park uuid; c_traf uuid; c_hay uuid; c_diger uuid;
    d_fen uuid; d_temizlik uuid; d_park uuid; d_zabita uuid; d_vet uuid;
BEGIN
    FOR m IN
        SELECT * FROM (VALUES
            ('Adalar Belediyesi','IST_ADALAR',40.876,29.093),
            ('Arnavutköy Belediyesi','IST_ARNAVUTKOY',41.184,28.740),
            ('Ataşehir Belediyesi','IST_ATASEHIR',40.984,29.107),
            ('Avcılar Belediyesi','IST_AVCILAR',40.980,28.717),
            ('Bağcılar Belediyesi','IST_BAGCILAR',41.039,28.856),
            ('Bahçelievler Belediyesi','IST_BAHCELIEVLER',41.000,28.859),
            ('Bakırköy Belediyesi','IST_BAKIRKOY',40.980,28.877),
            ('Başakşehir Belediyesi','IST_BASAKSEHIR',41.093,28.802),
            ('Bayrampaşa Belediyesi','IST_BAYRAMPASA',41.047,28.912),
            ('Beşiktaş Belediyesi','IST_BESIKTAS',41.043,29.007),
            ('Beykoz Belediyesi','IST_BEYKOZ',41.125,29.109),
            ('Beylikdüzü Belediyesi','IST_BEYLIKDUZU',41.003,28.641),
            ('Beyoğlu Belediyesi','IST_BEYOGLU',41.036,28.977),
            ('Büyükçekmece Belediyesi','IST_BUYUKCEKMECE',41.020,28.575),
            ('Çatalca Belediyesi','IST_CATALCA',41.143,28.461),
            ('Çekmeköy Belediyesi','IST_CEKMEKOY',41.038,29.180),
            ('Esenler Belediyesi','IST_ESENLER',41.043,28.876),
            ('Esenyurt Belediyesi','IST_ESENYURT',41.029,28.673),
            ('Eyüpsultan Belediyesi','IST_EYUPSULTAN',41.048,28.934),
            ('Fatih Belediyesi','IST_FATIH',41.019,28.949),
            ('Gaziosmanpaşa Belediyesi','IST_GAZIOSMANPASA',41.058,28.912),
            ('Güngören Belediyesi','IST_GUNGOREN',41.017,28.871),
            ('Kadıköy Belediyesi','IST_KADIKOY',40.983,29.030),
            ('Kağıthane Belediyesi','IST_KAGITHANE',41.085,28.972),
            ('Kartal Belediyesi','IST_KARTAL',40.888,29.190),
            ('Küçükçekmece Belediyesi','IST_KUCUKCEKMECE',41.000,28.775),
            ('Maltepe Belediyesi','IST_MALTEPE',40.935,29.130),
            ('Pendik Belediyesi','IST_PENDIK',40.877,29.234),
            ('Sancaktepe Belediyesi','IST_SANCAKTEPE',41.001,29.231),
            ('Sarıyer Belediyesi','IST_SARIYER',41.167,29.057),
            ('Silivri Belediyesi','IST_SILIVRI',41.073,28.246),
            ('Sultanbeyli Belediyesi','IST_SULTANBEYLI',40.968,29.267),
            ('Sultangazi Belediyesi','IST_SULTANGAZI',41.106,28.867),
            ('Şile Belediyesi','IST_SILE',41.176,29.612),
            ('Şişli Belediyesi','IST_SISLI',41.060,28.987),
            ('Tuzla Belediyesi','IST_TUZLA',40.816,29.300),
            ('Ümraniye Belediyesi','IST_UMRANIYE',41.016,29.121),
            ('Üsküdar Belediyesi','IST_USKUDAR',41.026,29.015),
            ('Zeytinburnu Belediyesi','IST_ZEYTINBURNU',40.994,28.905)
        ) AS t(name, code, lat, lng)
    LOOP
        IF EXISTS (SELECT 1 FROM public.municipalities WHERE code = m.code) THEN
            CONTINUE;
        END IF;

        mid := gen_random_uuid();

        INSERT INTO public.municipalities
            (id, name, code, is_active, province, center_latitude, center_longitude, created_at, updated_at, deleted_at, is_deleted)
        VALUES (mid, m.name, m.code, true, 'İstanbul', m.lat, m.lng, now(), NULL, NULL, false);

        c_yol := gen_random_uuid(); c_cop := gen_random_uuid(); c_ayd := gen_random_uuid();
        c_park := gen_random_uuid(); c_traf := gen_random_uuid(); c_hay := gen_random_uuid(); c_diger := gen_random_uuid();

        INSERT INTO public.complaint_categories (id, municipality_id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
        VALUES
            (c_yol,   mid, 'Yol ve Kaldırım',   'YOL_KALDIRIM',     true, now(), NULL, NULL, false),
            (c_cop,   mid, 'Çöp ve Temizlik',   'COP_TEMIZLIK',     true, now(), NULL, NULL, false),
            (c_ayd,   mid, 'Aydınlatma',        'AYDINLATMA',       true, now(), NULL, NULL, false),
            (c_park,  mid, 'Park ve Bahçe',     'PARK_BAHCE',       true, now(), NULL, NULL, false),
            (c_traf,  mid, 'Trafik',            'TRAFIK',           true, now(), NULL, NULL, false),
            (c_hay,   mid, 'Sokak Hayvanları',  'SOKAK_HAYVANLARI', true, now(), NULL, NULL, false),
            (c_diger, mid, 'Diğer',             'DIGER',            true, now(), NULL, NULL, false);

        d_fen := gen_random_uuid(); d_temizlik := gen_random_uuid(); d_park := gen_random_uuid();
        d_zabita := gen_random_uuid(); d_vet := gen_random_uuid();

        INSERT INTO public.departments (id, municipality_id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
        VALUES
            (d_fen,      mid, 'Fen İşleri',        'FEN_ISLERI',       true, now(), NULL, NULL, false),
            (d_temizlik, mid, 'Temizlik İşleri',   'TEMIZLIK_ISLERI',  true, now(), NULL, NULL, false),
            (d_park,     mid, 'Park ve Bahçeler',  'PARK_BAHCELER',    true, now(), NULL, NULL, false),
            (d_zabita,   mid, 'Zabıta',            'ZABITA',           true, now(), NULL, NULL, false),
            (d_vet,      mid, 'Veteriner İşleri',  'VETERINER_ISLERI', true, now(), NULL, NULL, false);

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
