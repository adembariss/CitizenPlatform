# Örnek talepler oluşturur (public API üzerinden, gerçek akışla).
# Her nokta için: konumdan belediye çözümlenir -> kategoriler alınır -> şikayet gönderilir.
# Kullanım:  ./scripts/seed-example-complaints.ps1   (API http://localhost:5080 açık olmalı)

param(
    [string]$ApiBase = "http://localhost:5080"
)

$ErrorActionPreference = "Stop"

# Farklı şehirlere örnek talepler (koordinatlar ilgili belediyenin sınır kutusu içinde)
$samples = @(
    @{ lat = 40.41;  lng = 26.67;  category = "YOL_KALDIRIM";     title = "Kaldırım çökmüş";        desc = "Liman caddesindeki kaldırım çökmüş, yaya geçişi tehlikeli." }
    @{ lat = 40.41;  lng = 26.67;  category = "AYDINLATMA";       title = "Sokak lambası yanmıyor"; desc = "Sahil kenarındaki lambalar iki gündür yanmıyor." }
    @{ lat = 39.93;  lng = 32.85;  category = "COP_TEMIZLIK";     title = "Çöp konteyneri taşmış";  desc = "Kızılay'da konteyner dolmuş, etraf kirli." }
    @{ lat = 38.42;  lng = 27.14;  category = "PARK_BAHCE";       title = "Parkta kırık bank";      desc = "Kordon'daki parkta banklar kırık." }
    @{ lat = 40.18;  lng = 29.07;  category = "TRAFIK";           title = "Trafik ışığı arızalı";   desc = "Heykel meydanındaki ışık sürekli kırmızıda kalıyor." }
    @{ lat = 36.90;  lng = 30.70;  category = "SOKAK_HAYVANLARI"; title = "Aç sokak hayvanları";    desc = "Konyaaltı'nda beslenme noktası gerekiyor." }
    @{ lat = 37.00;  lng = 35.32;  category = "DIGER";            title = "Rögar kapağı açık";      desc = "Cadde ortasındaki rögar kapağı yerinde değil." }
    @{ lat = 41.29;  lng = 36.33;  category = "YOL_KALDIRIM";     title = "Yolda çukur";            desc = "Atakum sahil yolunda derin çukur var." }
)

Write-Host "API: $ApiBase" -ForegroundColor Cyan
$created = 0

foreach ($s in $samples) {
    # 1) Konumdan belediyeyi çöz
    $resolve = Invoke-RestMethod -Uri "$ApiBase/api/public/municipalities/resolve?lat=$($s.lat)&lng=$($s.lng)"
    if (-not $resolve.data.isSuccess) {
        Write-Host "  ATLANDI ($($s.lat),$($s.lng)) -> belediye bulunamadı" -ForegroundColor Yellow
        continue
    }
    $muniId = $resolve.data.municipalityId
    $muniName = $resolve.data.municipalityName

    # 2) Belediyenin kategorilerini al, uygun kategoriyi bul
    $cats = Invoke-RestMethod -Uri "$ApiBase/api/public/municipalities/$muniId/categories"
    $cat = $cats.data | Where-Object { $_.code -eq $s.category } | Select-Object -First 1
    if (-not $cat) { $cat = $cats.data | Select-Object -First 1 }

    # 3) Şikayeti gönder.
    # NOT: Windows PowerShell 5.1'de ConvertTo-Json ondalıkları yerel kültürle (virgül)
    # yazabildiği için JSON gövdesini invariant biçimde elle kuruyoruz.
    $inv = [System.Globalization.CultureInfo]::InvariantCulture
    $lat = $s.lat.ToString($inv)
    $lng = $s.lng.ToString($inv)
    $titleJson = ($s.title | ConvertTo-Json)
    $descJson = ($s.desc | ConvertTo-Json)
    $catJson = ($cat.id | ConvertTo-Json)
    $body = "{""categoryId"":$catJson,""title"":$titleJson,""description"":$descJson,""latitude"":$lat,""longitude"":$lng,""isAnonymous"":true,""source"":""CitizenWeb""}"

    $res = Invoke-RestMethod -Uri "$ApiBase/api/public/complaints" -Method Post -ContentType "application/json; charset=utf-8" -Body ([System.Text.Encoding]::UTF8.GetBytes($body))
    Write-Host ("  OK  {0,-24} {1,-16} -> {2}" -f $muniName, $cat.name, $res.data.trackingCode) -ForegroundColor Green
    $created++
}

Write-Host "`nToplam oluşturulan talep: $created" -ForegroundColor Cyan
