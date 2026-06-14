# Itchy Backup

Teknik servis ortamları için geliştirilmiş kapsamlı Windows yedekleme ve geri yükleme aracı.

<p align="center">
  <img src="src/ItchyBackup/Resources/Icons/app-logo.png" width="520" alt="Itchy Backup Logo">
</p>

![Version](https://img.shields.io/badge/version-v1.0.0-007A4D?style=flat-square)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4?style=flat-square&logo=windows)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)

## v1.0 Özeti

- Yeni Itchy Backup logosu, uygulama ikonu ve zümrüt yeşili marka teması
- Baştan tasarlanan teknik servis konsolu: sabit navigasyon, iki kolonlu yedek çalışma alanı ve ayrı geri yükleme merkezi
- `backup_manifest.json` ile yedek kimliği, seçilen öğeler, dosya listesi ve meta bilgiler
- SHA-256 doğrulama, HTML servis raporu ve restore raporu
- Güçlendirilmiş geri yükleme: kısmi restore, çakışma politikası, ZIP parola desteği
- Windows Sistem Geri Yükleme noktası oluşturma, listeleme ve Windows restore ekranına erişim
- Profil içe/dışa aktarma, hazır profiller ve zamanlayıcı
- Webhook/SMTP test bildirimi
- SMTP parolası için Windows kullanıcı hesabına bağlı DPAPI koruması

## Özellikler

### Yedekleme Kategorileri

| Kategori | İçerik |
|---|---|
| Kullanıcı klasörleri | Masaüstü, Belgeler, İndirilenler, Resimler, Videolar, Müzik, AppData |
| Tarayıcı verileri | Chrome, Firefox, Edge, Opera, Brave, Vivaldi |
| Outlook / Mail | PST, OST, imzalar, şablonlar, otomatik tamamlama |
| Veritabanları | Firebird, SQLite, SQL Server, Access |
| Sanal makineler | VMware, VirtualBox, Hyper-V dosyaları |
| Bulut depolama | OneDrive, Google Drive, MEGA, Dropbox yerel dosyaları |
| Sistem araçları | Windows sürücüleri, WiFi profilleri ve Windows Sistem Geri Yükleme |
| Özel klasörler | Kullanıcının seçtiği klasör ve dosya konumları |

### Güvenlik ve Doğrulama

- ZIP + AES-256 parola koruması
- SHA-256 checksum manifesti ve otomatik doğrulama
- `backup_manifest.json` ile yedek içeriği ve meta veri kaydı
- VSS ile açık dosya desteği
- Outlook/veritabanı gibi hot backup riskleri için uyarı
- Yedek öncesi disk alanı, hedef, VSS, OneDrive, FAT32 ve yönetici yetkisi kontrolü
- Parola alanlarında maskeli giriş ve göz butonu
- SMTP parolasını düz metin yerine Windows DPAPI ile koruma

### Profil ve Otomasyon

- Profil sistemi
- Hazır profiller:
  - Hızlı Format
  - Standart Servis
  - Muhasebe PC
  - Tarayıcı Kurtarma
  - Tam Kullanıcı
- Profil içe/dışa aktarma
- Windows Görev Zamanlayıcısı entegrasyonu
- Artımlı yedekleme
- Çoklu hedef klasörüne kopyalama
- Yedek rotasyonu: son N yedeği tut veya X günden eskiyi sil
- UNC/SMB ağ paylaşımı ve isteğe bağlı kimlik bilgisi desteği

### Geri Yükleme

- Yedek listesini otomatik görüntüleme
- ZIP veya klasör yedeğinden geri yükleme
- Kısmi geri yükleme ve klasör ağacı
- Geri yükleme önizlemesi
- Var olan dosyalar için çakışma politikası:
  - Atla
  - Üzerine yaz
  - Yeni isimle geri yükle
- Şifreli ZIP geri yükleme için maskeli/gösterilebilir parola alanı
- Restore sonrası `restore_report_*.txt`

### Windows Sistem Geri Yükleme

- Program içinden sistem geri yükleme noktası oluşturma
- Son geri yükleme noktalarını listeleme
- Windows Geri Yükleme ekranını açma
- Sistem Koruması ayarlarına erişme

### İzleme ve Raporlama

- Canlı ilerleme, hız ve tahmini süre
- Sonuç ekranında kategori, dosya, hata, uyarı, süre ve boyut özeti
- Checksum doğrulama sonucu
- HTML müşteri/servis raporu
- Geçmiş ekranında checksum doğrulama
- İki yedeği karşılaştırma
- Windows toast bildirimi
- Webhook bildirimi
- SMTP e-posta bildirimi ve test butonu

### Arayüz

- Baştan tasarlanmış, masaüstü kullanımına odaklı teknik servis konsolu
- Sabit sol navigasyon ve aynı ekranda erişilebilen profil iş akışları
- Kaynak seçimi ile yedek ayarlarını yan yana sunan iki kolonlu çalışma alanı
- Dosya geri yükleme ile Windows Sistem Geri Yükleme araçlarını birleştiren geri yükleme merkezi
- İşlem özeti, ön kontrol, profil kaydetme ve yedek başlatma için sabit alt eylem çubuğu
- Nötr grafit koyu tema, temiz açık tema ve sade Servis teması
- Logo paletine uyarlanmış zümrüt yeşili vurgu rengi
- Accent renk seçimi
- Hazır ve kullanıcı tarafından kaydedilen profillerin sol profil listesinde yönetimi
- Özel klasör ekleme, profil içe/dışa aktarma, düzenleme ve silme için hızlı komutlar
- Yedek öncesi kontrol, canlı ilerleme ve işlem iptali
- Göz ikonlu parola göster/gizle kontrolleri
- Webhook ve SMTP alanlarında açık, alan üstü bilgi etiketleri
- Teknik servis akışına uygun yoğun, tutarlı ve okunabilir düzen

## Kurulum

### Kullanıcılar İçin

1. Releases sayfasından Setup veya Portable sürümü indirin.
2. VSS, sürücü dışa aktarımı, WiFi profilleri ve Windows geri yükleme noktası için uygulamayı yönetici olarak çalıştırın.
3. İlk açılışta varsayılan yedek hedefini ve tercih ettiğiniz temayı ayarlayın.
4. Profil listesinden hazır profillerden birini seçebilir veya kendi profilinizi kaydedebilirsiniz.

### Geliştiriciler İçin

Gereksinimler:

- .NET 8 SDK
- Visual Studio 2022 veya VS Code
- Setup üretimi için Inno Setup 6

Derleme:

```bat
dotnet build ItchyBackup.sln
```

Portable ve setup çıktıları için:

```bat
build.bat
```

## Proje Yapısı

```text
ItchyBackup/
|-- src/ItchyBackup/
|   |-- Models/
|   |-- ViewModels/
|   |-- Views/
|   |-- Services/
|   `-- Resources/Styles/
|-- installer/
|-- build.bat
`-- README.md
```

Öne çıkan servisler:

- `BackupEngine`: Ana yedekleme motoru
- `RestoreEngine`: Geri yükleme motoru
- `SystemRestoreService`: Windows sistem geri yükleme noktası yönetimi
- `BackupManifestService`: v1 yedek manifesti
- `ProfileService`: Hazır profiller, profil içe/dışa aktarma
- `BackupPreflightService`: Yedek öncesi kontrol
- `BackupCompareService`: İki yedeği karşılaştırma
- `BackupReportService`: HTML sonuç raporu
- `ChecksumService`: SHA-256 manifest ve doğrulama
- `NotificationService`: Toast, webhook ve SMTP bildirimi
- `SecretService`: DPAPI tabanlı yerel parola koruması
- `ThemeService`: Tema ve accent renk yönetimi

## Teknolojiler

- C# / WPF
- .NET 8
- CommunityToolkit.Mvvm
- SharpZipLib
- Newtonsoft.Json
- Windows Task Scheduler, VSS, pnputil, netsh, System Restore

## Geliştirici

M. Mert
