# Changelog

## v1.7.0 - Kontrol Merkezi ve Servis Akışı Güncellemesi

- Uygulama açılışına Dashboard/Kontrol Merkezi eklendi.
- Dashboard; son yedek, hedef klasör, zamanlayıcı ve son log uyarılarını kartlar halinde gösteriyor.
- Yedekleme ekranına profil bazlı yedek kuyruğu eklendi.
- Seçili profiller kuyruğa alınabiliyor, kuyruk sırayla çalıştırılabiliyor ve tamamlanan profil listeden düşüyor.
- Zamanlayıcı ekranına görev yönetimi eklendi; Itchy Backup görevleri listeleniyor, test çalıştırılabiliyor ve silinebiliyor.
- Program içi Log Merkezi eklendi; loglar hata/uyarı/bilgi filtresi ve metin aramasıyla izlenebiliyor.
- Geri yükleme ekranındaki klasör seçimi ağaç görünümüne dönüştürüldü.
- Yedek Testi eklendi; seçili yedek için manifest, checksum, ZIP açılabilirliği, rapor ve içerik kontrollerinden sağlık puanı üretiliyor.
- HTML rapora sağlık puanı, müşteri adı, teknisyen adı ve gizlilik modu eklendi.
- Ayarlara müşteri/teknisyen rapor bilgileri ve müşteri raporunda kişisel yolları gizleme seçeneği eklendi.
- Uygulama, kurulum ve paket sürümü `v1.7.0` olarak güncellendi.

## v1.6.0 - Profil, Zamanlayıcı ve Log Güncellemesi

- Yedek kaynaklarında kategori başlığına tıklayınca kategori açılıp kapanır hale getirildi.
- Profil yönetimi yeniden ele alındı; `+` butonu artık yeni profil oluşturuyor.
- Seçili profil düzenleme akışı yeniden adlandırma ve mevcut ayarları güncelleme için doğrudan dialog açacak şekilde düzeltildi.
- Profil içe aktarma, dışa aktarma, silme ve uygulama komutları ayrı aksiyonlar olarak korunup netleştirildi.
- Zamanlayıcı ekranına yedek hedef klasörü alanı ve klasör seçme komutu eklendi.
- Windows Görev Zamanlayıcısı komutu artık profil ile birlikte hedef klasörü de uygulamaya geçiriyor.
- `--autobackup` çalışma modu tamamlandı; zamanlayıcıdan açılan uygulama arayüz göstermeden profili çalıştırıp yedek alabiliyor.
- Müşteri e-postası ekranından gönderen SMTP hesap kurulumu kaldırıldı; kullanıcı yalnızca alıcı e-posta adresini giriyor.
- Uygulama logları detaylandırıldı; sürüm, sistem, hedef, seçenekler, kaynak listesi, kategori özetleri, dosya kopyalama/değişmedi/atlandı bilgileri ve rapor yolu kayıt altına alınıyor.
- Uygulama, kurulum ve paket sürümü `v1.6.0` olarak güncellendi.

## v1.5.0 - Arayüz ve İş Akışı Güncellemesi

- Yedek kategorileri ikonlu ve katlanabilir hale getirildi.
- Kaydedilmiş profiller yedek ekranına taşınarak yatay kaydırılabilir profil şeridi oluşturuldu.
- Profil şeridi, tüm profil sayılarına uyum sağlayan tam genişlik açılır profil seçiciye dönüştürüldü.
- Hazır şablon düğmeleri kaldırılarak profil seçimi tek bir alanda toplandı.
- Rotasyon seçeneklerine yedek sayısı ve gün sınırı girişleri eklendi.
- Zamanlayıcı saat seçimi saat ve dakika açılır listeleriyle yenilendi.
- Ağ kimlik bilgisi alanları ayrı ve açıklayıcı başlıklarla düzenlendi.
- Ayarlar ekranındaki GitHub bağlantısı küçük ikon düğmesine dönüştürüldü.
- Müşteri e-postası akışı `info@itchy.com.tr` gönderen hesabına göre sadeleştirildi.
- Uygulama, kurulum ve paket sürümü `v1.5.0` olarak güncellendi.

## v1.0.0 - Tam Sürüm

- Uygulama sürümü `v1.0.0` olarak güncellendi.
- Yeni Itchy Backup logo/ikon kimliği ve zümrüt yeşili tema korundu.
- Yedeklere `backup_manifest.json` eklendi.
- HTML servis raporu genişletildi.
- Restore motoruna çakışma politikası eklendi:
  - Atla
  - Üzerine yaz
  - Yeni isimle geri yükle
- Restore sonrası `restore_report_*.txt` oluşturuluyor.
- Windows Sistem Geri Yükleme noktası oluşturma, listeleme ve Windows restore ekranına erişim eklendi.
- Profil içe/dışa aktarma eklendi.
- Webhook/SMTP için test bildirimi eklendi.
- SMTP parolası Windows DPAPI ile korunur hale getirildi.
- Windows başlangıcında açılma ayarı gerçek registry entegrasyonuna bağlandı.
- README ve build/installer sürüm bilgileri v1.0.0 için güncellendi.
