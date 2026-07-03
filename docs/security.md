# Güvenlik Notları

## Dosya Yükleme Güvenliği

Şikayet fotoğrafları yalnızca kontrollü multipart endpointleri üzerinden kabul edilir. Backend hem bildirilen `Content-Type` değerini hem de dosya imzasını kontrol eder; bu yüzden bir HTML/script dosyasını resim uzantısıyla göndermek yeterli değildir.

Mevcut kontroller:

- İzin verilen tipler: `image/jpeg`, `image/png`, `image/webp`.
- Varsayılan dosya başına limit: `10 MB`.
- ASP.NET multipart request body limiti config üzerinden sınırlandırılır.
- Dosyalar `wwwroot` altında tutulmaz.
- Saklanan dosya adı sunucu tarafından random üretilir.
- Orijinal dosya adı sadece sanitize edilmiş metadata olarak saklanır.
- Local path her zaman config storage root altında resolve edilir; path traversal engellenir.
- Her dosya için SHA256 hash hesaplanıp saklanır.
- Gelecek antivirüs veya malware taraması için `IFileSafetyScanner` vardır; mevcut implementasyon `NoOpFileSafetyScanner`dır.

## EXIF Konumu Yardımcı Veridir

Fotoğraf EXIF GPS bilgisi hiç olmayabilir, eski olabilir, değiştirilebilir veya başka bir görselden kopyalanmış olabilir. Bu yüzden belediye tespiti hiçbir zaman EXIF konumu üzerinden yapılmaz.

Belediye tespiti yalnızca vatandaşın gönderdiği veya cihazdan alınan ana koordinatla yapılır:

- `latitude`
- `longitude`

EXIF GPS varsa attachment kaydına yazılır ve inceleme bağlamı için complaint üzerindeki fotoğraf EXIF alanlarına kopyalanabilir. Bu veri karar verici değil, yardımcı veridir.

## KVKK Notları

Şikayet fotoğrafları yüz, plaka, bina girişi, işyeri tabelası veya gömülü EXIF metadata gibi kişisel veri içerebilir. Platform, yüklenen fotoğrafları kişisel veri barındırabilecek kayıtlar olarak ele almalıdır.

Operasyon önerileri:

- Vatandaşa fotoğrafların konum ve cihaz metadata bilgisi içerebileceği açıkça belirtilmelidir.
- Saklama süreleri belediye ve şikayet yaşam döngüsüne göre netleştirilmelidir.
- Personel erişimi rol, belediye, birim ve iş amacı bazında sınırlandırılmalıdır.
- Sonraki audit iterasyonlarında attachment erişimleri loglanmalıdır.
- Public clientlara raw storage path veya object key döndürülmemelidir.
- Production öncesi malware tarama entegrasyonu eklenmelidir.
