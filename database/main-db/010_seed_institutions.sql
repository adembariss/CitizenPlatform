-- Dağıtım kurumları (elektrik / su / doğalgaz) referans verisi: kurum, hizmet bölgesi,
-- kategori ve birimler. Production dahil her ortamda çalışır — demo kullanıcı OLUŞTURMAZ
-- (demo hesapları yalnızca Development seeder'ı açar).
--
-- İdempotent: koda göre kurum, (kurum, kod) ikilisine göre kategori/birim ve
-- (kurum, il) ikilisine göre hizmet bölgesi yeniden eklenmez. Tekrar tekrar çalıştırılabilir.
--
-- type: 1=Elektrik, 2=Su, 3=Doğalgaz
DO $$
DECLARE
    rec       RECORD;
    inst_id   uuid;
    prov      text;
    cat       RECORD;
    dep       RECORD;
BEGIN
    FOR rec IN
        SELECT * FROM (VALUES
            -- ---------- Elektrik: 21 dağıtım bölgesi (81 ilin tamamını kapsar) ----------
            ('EL_BEDAS',      'BEDAŞ – Boğaziçi Elektrik',                1, 'İstanbul',      ARRAY['İstanbul']),
            ('EL_AYEDAS',     'AYEDAŞ – İstanbul Anadolu Yakası Elektrik',1, 'İstanbul',      ARRAY['İstanbul']),
            ('EL_TOROSLAR',   'TOROSLAR EDAŞ',                            1, 'Adana',         ARRAY['Adana','Gaziantep','Mersin','Hatay','Osmaniye','Kilis']),
            ('EL_BASKENT',    'Başkent EDAŞ',                             1, 'Ankara',        ARRAY['Ankara','Zonguldak','Kastamonu','Kırıkkale','Karabük','Çankırı','Bartın']),
            ('EL_DICLE',      'DEDAŞ – Dicle Elektrik',                   1, 'Diyarbakır',    ARRAY['Şanlıurfa','Diyarbakır','Mardin','Batman','Şırnak','Siirt']),
            ('EL_GEDIZ',      'GDZ – Gediz Elektrik',                     1, 'İzmir',         ARRAY['İzmir','Manisa']),
            ('EL_ULUDAG',     'UEDAŞ – Uludağ Elektrik',                  1, 'Bursa',         ARRAY['Bursa','Balıkesir','Çanakkale','Yalova']),
            ('EL_MERAM',      'MEDAŞ – Meram Elektrik',                   1, 'Konya',         ARRAY['Konya','Aksaray','Niğde','Nevşehir','Karaman','Kırşehir']),
            ('EL_SEDAS',      'SEDAŞ – Sakarya Elektrik',                 1, 'Kocaeli',       ARRAY['Kocaeli','Sakarya','Düzce','Bolu']),
            ('EL_YEDAS',      'YEDAŞ – Yeşilırmak Elektrik',              1, 'Samsun',        ARRAY['Samsun','Ordu','Çorum','Amasya','Sinop']),
            ('EL_AYDEM',      'ADM – Aydem Elektrik',                     1, 'Denizli',       ARRAY['Aydın','Denizli','Muğla']),
            ('EL_AKDENIZ',    'AEDAŞ – Akdeniz Elektrik',                 1, 'Antalya',       ARRAY['Antalya','Isparta','Burdur']),
            ('EL_OSMANGAZI',  'OEDAŞ – Osmangazi Elektrik',               1, 'Eskişehir',     ARRAY['Eskişehir','Afyonkarahisar','Kütahya','Uşak','Bilecik']),
            ('EL_ARAS',       'Aras EDAŞ',                                1, 'Erzurum',       ARRAY['Erzurum','Ağrı','Kars','Erzincan','Iğdır','Ardahan','Bayburt']),
            ('EL_VEDAS',      'VEDAŞ – Van Gölü Elektrik',                1, 'Van',           ARRAY['Van','Muş','Bitlis','Hakkari']),
            ('EL_CORUH',      'Çoruh EDAŞ',                               1, 'Trabzon',       ARRAY['Trabzon','Giresun','Rize','Artvin','Gümüşhane']),
            ('EL_FIRAT',      'FEDAŞ – Fırat Elektrik',                   1, 'Elazığ',        ARRAY['Malatya','Elazığ','Bingöl','Tunceli']),
            ('EL_AKEDAS',     'AKEDAŞ – Kahramanmaraş Elektrik',          1, 'Kahramanmaraş', ARRAY['Kahramanmaraş','Adıyaman']),
            ('EL_CAMLIBEL',   'ÇEDAŞ – Çamlıbel Elektrik',                1, 'Sivas',         ARRAY['Sivas','Tokat','Yozgat']),
            ('EL_TREDAS',     'TREDAŞ – Trakya Elektrik',                 1, 'Tekirdağ',      ARRAY['Tekirdağ','Kırklareli','Edirne']),
            ('EL_KCETAS',     'KCETAŞ – Kayseri Elektrik',                1, 'Kayseri',       ARRAY['Kayseri']),

            -- ---------- Su ve Kanalizasyon İdareleri: 30 büyükşehrin tamamı ----------
            ('SU_ISTANBUL',      'İSKİ – İstanbul Su ve Kanalizasyon',      2, 'İstanbul',      ARRAY['İstanbul']),
            ('SU_ANKARA',        'ASKİ – Ankara Su ve Kanalizasyon',        2, 'Ankara',        ARRAY['Ankara']),
            ('SU_IZMIR',         'İZSU – İzmir Su ve Kanalizasyon',         2, 'İzmir',         ARRAY['İzmir']),
            ('SU_BURSA',         'BUSKİ – Bursa Su ve Kanalizasyon',        2, 'Bursa',         ARRAY['Bursa']),
            ('SU_ANTALYA',       'ASAT – Antalya Su ve Atıksu',             2, 'Antalya',       ARRAY['Antalya']),
            ('SU_ADANA',         'ASKİ – Adana Su ve Kanalizasyon',         2, 'Adana',         ARRAY['Adana']),
            ('SU_MERSIN',        'MESKİ – Mersin Su ve Kanalizasyon',       2, 'Mersin',        ARRAY['Mersin']),
            ('SU_HATAY',         'HatSU – Hatay Su ve Kanalizasyon',        2, 'Hatay',         ARRAY['Hatay']),
            ('SU_DIYARBAKIR',    'DİSKİ – Diyarbakır Su ve Kanalizasyon',   2, 'Diyarbakır',    ARRAY['Diyarbakır']),
            ('SU_KONYA',         'KOSKİ – Konya Su ve Kanalizasyon',        2, 'Konya',         ARRAY['Konya']),
            ('SU_GAZIANTEP',     'GASKİ – Gaziantep Su ve Kanalizasyon',    2, 'Gaziantep',     ARRAY['Gaziantep']),
            ('SU_KAYSERI',       'KASKİ – Kayseri Su ve Kanalizasyon',      2, 'Kayseri',       ARRAY['Kayseri']),
            ('SU_ESKISEHIR',     'ESKİ – Eskişehir Su ve Kanalizasyon',     2, 'Eskişehir',     ARRAY['Eskişehir']),
            ('SU_SAMSUN',        'SASKİ – Samsun Su ve Kanalizasyon',       2, 'Samsun',        ARRAY['Samsun']),
            ('SU_KOCAELI',       'İSU – Kocaeli Su ve Kanalizasyon',        2, 'Kocaeli',       ARRAY['Kocaeli']),
            ('SU_DENIZLI',       'DESKİ – Denizli Su ve Kanalizasyon',      2, 'Denizli',       ARRAY['Denizli']),
            ('SU_MUGLA',         'MUSKİ – Muğla Su ve Kanalizasyon',        2, 'Muğla',         ARRAY['Muğla']),
            ('SU_MALATYA',       'MASKİ – Malatya Su ve Kanalizasyon',      2, 'Malatya',       ARRAY['Malatya']),
            ('SU_VAN',           'VASKİ – Van Su ve Kanalizasyon',          2, 'Van',           ARRAY['Van']),
            ('SU_SANLIURFA',     'ŞUSKİ – Şanlıurfa Su ve Kanalizasyon',    2, 'Şanlıurfa',     ARRAY['Şanlıurfa']),
            ('SU_AYDIN',         'ASKİ – Aydın Su ve Kanalizasyon',         2, 'Aydın',         ARRAY['Aydın']),
            ('SU_BALIKESIR',     'BASKİ – Balıkesir Su ve Kanalizasyon',    2, 'Balıkesir',     ARRAY['Balıkesir']),
            ('SU_ERZURUM',       'ESKİ – Erzurum Su ve Kanalizasyon',       2, 'Erzurum',       ARRAY['Erzurum']),
            ('SU_KAHRAMANMARAS', 'KASKİ – Kahramanmaraş Su ve Kanalizasyon',2, 'Kahramanmaraş', ARRAY['Kahramanmaraş']),
            ('SU_MANISA',        'MASKİ – Manisa Su ve Kanalizasyon',       2, 'Manisa',        ARRAY['Manisa']),
            ('SU_MARDIN',        'MARSU – Mardin Su ve Kanalizasyon',       2, 'Mardin',        ARRAY['Mardin']),
            ('SU_ORDU',          'OSKİ – Ordu Su ve Kanalizasyon',          2, 'Ordu',          ARRAY['Ordu']),
            ('SU_SAKARYA',       'SASKİ – Sakarya Su ve Kanalizasyon',      2, 'Sakarya',       ARRAY['Sakarya']),
            ('SU_TEKIRDAG',      'TESKİ – Tekirdağ Su ve Kanalizasyon',     2, 'Tekirdağ',      ARRAY['Tekirdağ']),
            ('SU_TRABZON',       'TİSKİ – Trabzon Su ve Kanalizasyon',      2, 'Trabzon',       ARRAY['Trabzon']),

            -- ---------- Doğalgaz dağıtım şirketleri (lisanslı bölgeleriyle) ----------
            ('GAZ_IGDAS',             'İGDAŞ – İstanbul Doğalgaz',      3, 'İstanbul',  ARRAY['İstanbul']),
            ('GAZ_BASKENT',           'Başkentgaz – Ankara Doğalgaz',   3, 'Ankara',    ARRAY['Ankara']),
            ('GAZ_IZMIR',             'İzmirgaz – İzmir Doğalgaz',      3, 'İzmir',     ARRAY['İzmir']),
            ('GAZ_BURSA',             'Bursagaz – Bursa Doğalgaz',      3, 'Bursa',     ARRAY['Bursa']),
            ('GAZ_KOCAELI',           'İzgaz – Kocaeli Doğalgaz',       3, 'Kocaeli',   ARRAY['Kocaeli']),
            ('GAZ_ESKISEHIR',         'Esgaz – Eskişehir Doğalgaz',     3, 'Eskişehir', ARRAY['Eskişehir']),
            ('GAZ_KAYSERI',           'Kayserigaz – Kayseri Doğalgaz',  3, 'Kayseri',   ARRAY['Kayseri']),
            ('GAZ_GAZIANTEP',         'Gazdaş – Gaziantep Doğalgaz',    3, 'Gaziantep', ARRAY['Gaziantep']),
            ('GAZ_SAMSUN',            'Samgaz – Samsun Doğalgaz',       3, 'Samsun',    ARRAY['Samsun']),
            ('GAZ_TRAKYA',            'Trakya Bölgesi Doğalgaz',        3, 'Tekirdağ',  ARRAY['Tekirdağ','Edirne','Kırklareli']),
            ('GAZ_PALGAZ',            'Palgaz – Balıkesir Doğalgaz',    3, 'Balıkesir', ARRAY['Balıkesir']),
            ('GAZ_AKSA_CUKUROVA',     'Aksa Çukurova Doğalgaz',         3, 'Adana',     ARRAY['Adana','Mersin']),
            ('GAZ_AKSA_BILECIK_BOLU', 'Aksa Bilecik-Bolu Doğalgaz',     3, 'Bolu',      ARRAY['Bilecik','Bolu']),
            ('GAZ_ENERYA_KONYA',      'Enerya Konya Gaz',               3, 'Konya',     ARRAY['Konya']),
            ('GAZ_AGDAS',             'Agdaş – Adapazarı Gaz',          3, 'Sakarya',   ARRAY['Sakarya'])
        ) AS t(code, name, type, center, provinces)
    LOOP
        SELECT id INTO inst_id
        FROM public.institutions
        WHERE code = rec.code AND NOT is_deleted;

        IF inst_id IS NULL THEN
            inst_id := gen_random_uuid();
            INSERT INTO public.institutions
                (id, name, code, type, is_active, province, created_at, updated_at, deleted_at, is_deleted)
            VALUES
                (inst_id, rec.name, rec.code, rec.type, true, rec.center, now(), NULL, NULL, false);
        END IF;

        -- Hizmet bölgeleri (district NULL = ilin tamamı)
        FOREACH prov IN ARRAY rec.provinces
        LOOP
            IF NOT EXISTS (
                SELECT 1 FROM public.institution_service_areas
                WHERE institution_id = inst_id AND province = prov AND district IS NULL AND NOT is_deleted
            ) THEN
                INSERT INTO public.institution_service_areas
                    (id, institution_id, province, district, created_at, updated_at, deleted_at, is_deleted)
                VALUES
                    (gen_random_uuid(), inst_id, prov, NULL, now(), NULL, NULL, false);
            END IF;
        END LOOP;

        -- Sektöre özel kategoriler
        FOR cat IN
            SELECT * FROM (VALUES
                (1, 'Elektrik Kesintisi',          'ELK_KESINTI'),
                (1, 'Arıza Bildirimi',             'ELK_ARIZA'),
                (1, 'Sokak Aydınlatması Arızası',  'ELK_AYDINLATMA'),
                (1, 'Sayaç İşlemleri',             'ELK_SAYAC'),
                (1, 'Fatura İtirazı',              'ELK_FATURA'),
                (1, 'Yeni Abonelik',               'ELK_ABONELIK'),
                (2, 'Su Kesintisi',                'SU_KESINTI'),
                (2, 'Su Kaçağı / Arıza',           'SU_ARIZA'),
                (2, 'Kanalizasyon / Tıkanıklık',   'SU_KANAL'),
                (2, 'Sayaç İşlemleri',             'SU_SAYAC'),
                (2, 'Fatura İtirazı',              'SU_FATURA'),
                (2, 'Yeni Abonelik',               'SU_ABONELIK'),
                (3, 'Gaz Kesintisi',               'GAZ_KESINTI'),
                (3, 'Gaz Kaçağı (Acil)',           'GAZ_KACAK'),
                (3, 'Sayaç İşlemleri',             'GAZ_SAYAC'),
                (3, 'Fatura İtirazı',              'GAZ_FATURA'),
                (3, 'Yeni Abonelik',               'GAZ_ABONELIK')
            ) AS c(type, name, code)
            WHERE c.type = rec.type
        LOOP
            IF NOT EXISTS (
                SELECT 1 FROM public.complaint_categories
                WHERE institution_id = inst_id AND code = cat.code || '_' || rec.code AND NOT is_deleted
            ) THEN
                INSERT INTO public.complaint_categories
                    (id, municipality_id, institution_id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
                VALUES
                    (gen_random_uuid(), NULL, inst_id, cat.name, cat.code || '_' || rec.code, true, now(), NULL, NULL, false);
            END IF;
        END LOOP;

        -- Sektöre özel birimler (belediye birimlerinden bağımsız yapı)
        FOR dep IN
            SELECT * FROM (VALUES
                (1, 'Arıza ve Bakım Ekibi',        'ARIZA_BAKIM'),
                (1, 'Kesinti Yönetimi',            'KESINTI'),
                (1, 'Şebeke Operasyon',            'SEBEKE'),
                (1, 'Sokak Aydınlatma Ekibi',      'AYDINLATMA'),
                (1, 'Sayaç ve Ölçüm',              'SAYAC_OLCUM'),
                (1, 'Abonelik ve Tahakkuk',        'ABONE_TAHAKKUK'),
                (1, 'Müşteri Hizmetleri',          'MUSTERI'),
                (2, 'Su Arıza Ekibi',              'SU_ARIZA'),
                (2, 'Kanalizasyon Ekibi',          'KANALIZASYON'),
                (2, 'Şebeke İşletme',              'SEBEKE'),
                (2, 'Sayaç ve Ölçüm',              'SAYAC_OLCUM'),
                (2, 'Abonelik ve Tahakkuk',        'ABONE_TAHAKKUK'),
                (2, 'Müşteri Hizmetleri',          'MUSTERI'),
                (3, 'Acil Müdahale (Gaz Kaçağı)',  'ACIL_MUDAHALE'),
                (3, 'Şebeke Bakım',                'SEBEKE_BAKIM'),
                (3, 'Sayaç ve Ölçüm',              'SAYAC_OLCUM'),
                (3, 'Abonelik ve Tahakkuk',        'ABONE_TAHAKKUK'),
                (3, 'Müşteri Hizmetleri',          'MUSTERI')
            ) AS d(type, name, code)
            WHERE d.type = rec.type
        LOOP
            IF NOT EXISTS (
                SELECT 1 FROM public.departments
                WHERE institution_id = inst_id AND code = dep.code AND NOT is_deleted
            ) THEN
                INSERT INTO public.departments
                    (id, municipality_id, institution_id, name, code, is_active, created_at, updated_at, deleted_at, is_deleted)
                VALUES
                    (gen_random_uuid(), NULL, inst_id, dep.name, dep.code, true, now(), NULL, NULL, false);
            END IF;
        END LOOP;
    END LOOP;

    -- Erken geliştirme sırasında tahmini il eşlemesiyle girilmiş doğalgaz kayıtları:
    -- yerlerini lisans bölgeleri doğrulanmış kurumlar aldı, pasife çekilir.
    UPDATE public.institutions
    SET is_active = false, updated_at = now()
    WHERE code IN ('GAZ_ADANA', 'GAZ_AKSA', 'GAZ_ENERYA') AND is_active;
END $$;
