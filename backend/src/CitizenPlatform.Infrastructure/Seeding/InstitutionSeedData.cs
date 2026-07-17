using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Infrastructure.Seeding;

/// <summary>
/// Türkiye geneli dağıtım kurumları seed verisi (elektrik/su/doğalgaz) ve hizmet illeri.
/// Elektrik: 21 dağıtım bölgesinin tamamı (81 ili kapsar). Su/doğalgaz: büyük metropoller.
/// İl adları belediye il adlarıyla eşleşecek şekilde resmî yazımla verilir.
/// </summary>
public sealed record InstitutionSeed(string Name, string Code, InstitutionType Type, string CenterProvince, string[] Provinces);

public static class InstitutionSeedData
{
    public static readonly IReadOnlyDictionary<InstitutionType, (string Name, string Code)[]> Categories =
        new Dictionary<InstitutionType, (string, string)[]>
        {
            [InstitutionType.Electricity] =
            [
                ("Elektrik Kesintisi", "ELK_KESINTI"),
                ("Arıza Bildirimi", "ELK_ARIZA"),
                ("Sokak Aydınlatması Arızası", "ELK_AYDINLATMA"),
                ("Sayaç İşlemleri", "ELK_SAYAC"),
                ("Fatura İtirazı", "ELK_FATURA"),
                ("Yeni Abonelik", "ELK_ABONELIK")
            ],
            [InstitutionType.Water] =
            [
                ("Su Kesintisi", "SU_KESINTI"),
                ("Su Kaçağı / Arıza", "SU_ARIZA"),
                ("Kanalizasyon / Tıkanıklık", "SU_KANAL"),
                ("Sayaç İşlemleri", "SU_SAYAC"),
                ("Fatura İtirazı", "SU_FATURA"),
                ("Yeni Abonelik", "SU_ABONELIK")
            ],
            [InstitutionType.NaturalGas] =
            [
                ("Gaz Kesintisi", "GAZ_KESINTI"),
                ("Gaz Kaçağı (Acil)", "GAZ_KACAK"),
                ("Sayaç İşlemleri", "GAZ_SAYAC"),
                ("Fatura İtirazı", "GAZ_FATURA"),
                ("Yeni Abonelik", "GAZ_ABONELIK")
            ]
        };

    /// <summary>
    /// Kurum birimleri — belediye birimlerinden (Fen İşleri, Zabıta, Temizlik...) tamamen ayrı,
    /// sektöre özel operasyon yapısı.
    /// </summary>
    public static readonly IReadOnlyDictionary<InstitutionType, (string Name, string Code)[]> Departments =
        new Dictionary<InstitutionType, (string, string)[]>
        {
            [InstitutionType.Electricity] =
            [
                ("Arıza ve Bakım Ekibi", "ARIZA_BAKIM"),
                ("Kesinti Yönetimi", "KESINTI"),
                ("Şebeke Operasyon", "SEBEKE"),
                ("Sokak Aydınlatma Ekibi", "AYDINLATMA"),
                ("Sayaç ve Ölçüm", "SAYAC_OLCUM"),
                ("Abonelik ve Tahakkuk", "ABONE_TAHAKKUK"),
                ("Müşteri Hizmetleri", "MUSTERI")
            ],
            [InstitutionType.Water] =
            [
                ("Su Arıza Ekibi", "SU_ARIZA"),
                ("Kanalizasyon Ekibi", "KANALIZASYON"),
                ("Şebeke İşletme", "SEBEKE"),
                ("Sayaç ve Ölçüm", "SAYAC_OLCUM"),
                ("Abonelik ve Tahakkuk", "ABONE_TAHAKKUK"),
                ("Müşteri Hizmetleri", "MUSTERI")
            ],
            [InstitutionType.NaturalGas] =
            [
                ("Acil Müdahale (Gaz Kaçağı)", "ACIL_MUDAHALE"),
                ("Şebeke Bakım", "SEBEKE_BAKIM"),
                ("Sayaç ve Ölçüm", "SAYAC_OLCUM"),
                ("Abonelik ve Tahakkuk", "ABONE_TAHAKKUK"),
                ("Müşteri Hizmetleri", "MUSTERI")
            ]
        };

    public static readonly InstitutionSeed[] Institutions =
    [
        // ---- Elektrik: 21 dağıtım bölgesi (tam Türkiye kapsamı) ----
        new("BEDAŞ – Boğaziçi Elektrik", "EL_BEDAS", InstitutionType.Electricity, "İstanbul", ["İstanbul"]),
        new("AYEDAŞ – İstanbul Anadolu Yakası Elektrik", "EL_AYEDAS", InstitutionType.Electricity, "İstanbul", ["İstanbul"]),
        new("TOROSLAR EDAŞ", "EL_TOROSLAR", InstitutionType.Electricity, "Adana", ["Adana", "Gaziantep", "Mersin", "Hatay", "Osmaniye", "Kilis"]),
        new("Başkent EDAŞ", "EL_BASKENT", InstitutionType.Electricity, "Ankara", ["Ankara", "Zonguldak", "Kastamonu", "Kırıkkale", "Karabük", "Çankırı", "Bartın"]),
        new("DEDAŞ – Dicle Elektrik", "EL_DICLE", InstitutionType.Electricity, "Diyarbakır", ["Şanlıurfa", "Diyarbakır", "Mardin", "Batman", "Şırnak", "Siirt"]),
        new("GDZ – Gediz Elektrik", "EL_GEDIZ", InstitutionType.Electricity, "İzmir", ["İzmir", "Manisa"]),
        new("UEDAŞ – Uludağ Elektrik", "EL_ULUDAG", InstitutionType.Electricity, "Bursa", ["Bursa", "Balıkesir", "Çanakkale", "Yalova"]),
        new("MEDAŞ – Meram Elektrik", "EL_MERAM", InstitutionType.Electricity, "Konya", ["Konya", "Aksaray", "Niğde", "Nevşehir", "Karaman", "Kırşehir"]),
        new("SEDAŞ – Sakarya Elektrik", "EL_SEDAS", InstitutionType.Electricity, "Kocaeli", ["Kocaeli", "Sakarya", "Düzce", "Bolu"]),
        new("YEDAŞ – Yeşilırmak Elektrik", "EL_YEDAS", InstitutionType.Electricity, "Samsun", ["Samsun", "Ordu", "Çorum", "Amasya", "Sinop"]),
        new("ADM – Aydem Elektrik", "EL_AYDEM", InstitutionType.Electricity, "Denizli", ["Aydın", "Denizli", "Muğla"]),
        new("AEDAŞ – Akdeniz Elektrik", "EL_AKDENIZ", InstitutionType.Electricity, "Antalya", ["Antalya", "Isparta", "Burdur"]),
        new("OEDAŞ – Osmangazi Elektrik", "EL_OSMANGAZI", InstitutionType.Electricity, "Eskişehir", ["Eskişehir", "Afyonkarahisar", "Kütahya", "Uşak", "Bilecik"]),
        new("Aras EDAŞ", "EL_ARAS", InstitutionType.Electricity, "Erzurum", ["Erzurum", "Ağrı", "Kars", "Erzincan", "Iğdır", "Ardahan", "Bayburt"]),
        new("VEDAŞ – Van Gölü Elektrik", "EL_VEDAS", InstitutionType.Electricity, "Van", ["Van", "Muş", "Bitlis", "Hakkari"]),
        new("Çoruh EDAŞ", "EL_CORUH", InstitutionType.Electricity, "Trabzon", ["Trabzon", "Giresun", "Rize", "Artvin", "Gümüşhane"]),
        new("FEDAŞ – Fırat Elektrik", "EL_FIRAT", InstitutionType.Electricity, "Elazığ", ["Malatya", "Elazığ", "Bingöl", "Tunceli"]),
        new("AKEDAŞ – Kahramanmaraş Elektrik", "EL_AKEDAS", InstitutionType.Electricity, "Kahramanmaraş", ["Kahramanmaraş", "Adıyaman"]),
        new("ÇEDAŞ – Çamlıbel Elektrik", "EL_CAMLIBEL", InstitutionType.Electricity, "Sivas", ["Sivas", "Tokat", "Yozgat"]),
        new("TREDAŞ – Trakya Elektrik", "EL_TREDAS", InstitutionType.Electricity, "Tekirdağ", ["Tekirdağ", "Kırklareli", "Edirne"]),
        new("KCETAŞ – Kayseri Elektrik", "EL_KCETAS", InstitutionType.Electricity, "Kayseri", ["Kayseri"]),

        // ---- Su ve Kanalizasyon İdareleri (büyükşehirler) ----
        new("İSKİ – İstanbul Su ve Kanalizasyon", "SU_ISTANBUL", InstitutionType.Water, "İstanbul", ["İstanbul"]),
        new("ASKİ – Ankara Su ve Kanalizasyon", "SU_ANKARA", InstitutionType.Water, "Ankara", ["Ankara"]),
        new("İZSU – İzmir Su ve Kanalizasyon", "SU_IZMIR", InstitutionType.Water, "İzmir", ["İzmir"]),
        new("BUSKİ – Bursa Su ve Kanalizasyon", "SU_BURSA", InstitutionType.Water, "Bursa", ["Bursa"]),
        new("ASAT – Antalya Su ve Atıksu", "SU_ANTALYA", InstitutionType.Water, "Antalya", ["Antalya"]),
        new("ASKİ – Adana Su ve Kanalizasyon", "SU_ADANA", InstitutionType.Water, "Adana", ["Adana"]),
        new("MESKİ – Mersin Su ve Kanalizasyon", "SU_MERSIN", InstitutionType.Water, "Mersin", ["Mersin"]),
        new("HatSU – Hatay Su ve Kanalizasyon", "SU_HATAY", InstitutionType.Water, "Hatay", ["Hatay"]),
        new("DİSKİ – Diyarbakır Su ve Kanalizasyon", "SU_DIYARBAKIR", InstitutionType.Water, "Diyarbakır", ["Diyarbakır"]),
        new("KOSKİ – Konya Su ve Kanalizasyon", "SU_KONYA", InstitutionType.Water, "Konya", ["Konya"]),
        new("GASKİ – Gaziantep Su ve Kanalizasyon", "SU_GAZIANTEP", InstitutionType.Water, "Gaziantep", ["Gaziantep"]),
        new("KASKİ – Kayseri Su ve Kanalizasyon", "SU_KAYSERI", InstitutionType.Water, "Kayseri", ["Kayseri"]),
        new("ESKİ – Eskişehir Su ve Kanalizasyon", "SU_ESKISEHIR", InstitutionType.Water, "Eskişehir", ["Eskişehir"]),
        new("SASKİ – Samsun Su ve Kanalizasyon", "SU_SAMSUN", InstitutionType.Water, "Samsun", ["Samsun"]),
        new("İSU – Kocaeli Su ve Kanalizasyon", "SU_KOCAELI", InstitutionType.Water, "Kocaeli", ["Kocaeli"]),
        new("DESKİ – Denizli Su ve Kanalizasyon", "SU_DENIZLI", InstitutionType.Water, "Denizli", ["Denizli"]),
        new("MUSKİ – Muğla Su ve Kanalizasyon", "SU_MUGLA", InstitutionType.Water, "Muğla", ["Muğla"]),
        new("MASKİ – Malatya Su ve Kanalizasyon", "SU_MALATYA", InstitutionType.Water, "Malatya", ["Malatya"]),
        new("VASKİ – Van Su ve Kanalizasyon", "SU_VAN", InstitutionType.Water, "Van", ["Van"]),
        new("ŞUSKİ – Şanlıurfa Su ve Kanalizasyon", "SU_SANLIURFA", InstitutionType.Water, "Şanlıurfa", ["Şanlıurfa"]),

        // ---- Doğalgaz dağıtım şirketleri (başlıca) ----
        new("İGDAŞ – İstanbul Doğalgaz", "GAZ_IGDAS", InstitutionType.NaturalGas, "İstanbul", ["İstanbul"]),
        new("BAŞKENTGAZ – Ankara Doğalgaz", "GAZ_BASKENT", InstitutionType.NaturalGas, "Ankara", ["Ankara"]),
        new("İZMİRGAZ – İzmir Doğalgaz", "GAZ_IZMIR", InstitutionType.NaturalGas, "İzmir", ["İzmir"]),
        new("BURSAGAZ – Bursa Doğalgaz", "GAZ_BURSA", InstitutionType.NaturalGas, "Bursa", ["Bursa"]),
        new("İZGAZ – Kocaeli Doğalgaz", "GAZ_KOCAELI", InstitutionType.NaturalGas, "Kocaeli", ["Kocaeli"]),
        new("ESGAZ – Eskişehir Doğalgaz", "GAZ_ESKISEHIR", InstitutionType.NaturalGas, "Eskişehir", ["Eskişehir"]),
        new("ADAGAZ – Adana Doğalgaz", "GAZ_ADANA", InstitutionType.NaturalGas, "Adana", ["Adana"]),
        new("KAYSERİGAZ – Kayseri Doğalgaz", "GAZ_KAYSERI", InstitutionType.NaturalGas, "Kayseri", ["Kayseri"]),
        new("Gazdaş – Gaziantep Doğalgaz", "GAZ_GAZIANTEP", InstitutionType.NaturalGas, "Gaziantep", ["Gaziantep"]),
        new("SAMGAZ – Samsun Doğalgaz", "GAZ_SAMSUN", InstitutionType.NaturalGas, "Samsun", ["Samsun"]),
        new("Aksa Doğalgaz", "GAZ_AKSA", InstitutionType.NaturalGas, "Balıkesir", ["Balıkesir", "Manisa", "Ordu", "Giresun", "Aksaray", "Elazığ", "Şanlıurfa", "Van", "Çanakkale", "Yalova"]),
        new("Enerya Doğalgaz", "GAZ_ENERYA", InstitutionType.NaturalGas, "Antalya", ["Antalya", "Karaman", "Niğde", "Nevşehir", "Erzincan", "Aydın"])
    ];
}
