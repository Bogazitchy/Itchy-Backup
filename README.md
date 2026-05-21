# Itchy Backup

Teknik servis ortamları için geliştirilmiş kapsamlı Windows yedekleme aracı.

![Version](https://img.shields.io/badge/version-v0.8.5-6C5CE7?style=flat-square)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4?style=flat-square&logo=windows)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)

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
| Sistem araçları | Windows sürücüleri ve WiFi profilleri dışa aktarımı |
| Özel klasörler | Kullanıcının seçtiği klasör ve dosya konumları |

### Güvenlik ve Doğrulama

- ZIP + AES-256 parola koruması
- SHA-256 checksum manifesti ve doğrulama
- VSS ile açık dosya desteği
- Outlook/veritabanı gibi hot backup riskleri için uyarı
- Yedek öncesi disk alanı, hedef, VSS, OneDrive, FAT32 ve yönetici yetkisi kontrolü
- Parola alanlarında maskeli giriş
- Parola alanlarının yanında göz butonu ile geçici göster/gizle desteği

### Profil ve Otomasyon

- Profil sistemi
- Varsayılan hazır profiller:
  - Hızlı Format
  - Standart Servis
  - Muhasebe PC
  - Tarayıcı Kurtarma
  - Tam Kullanıcı
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
- Var olan dosyaların üzerine yazma seçeneği
- Şifreli ZIP geri yükleme için maskeli/gösterilebilir parola alanı

### İzleme ve Raporlama

- Canlı ilerleme, hız ve tahmini süre
- Sonuç ekranında kategori, dosya, hata, uyarı, süre ve boyut özeti
- Checksum doğrulama sonucu
- HTML müşteri/servis raporu
- Geçmiş ekranında checksum doğrulama
- İki yedeği karşılaştırma
- Windows toast bildirimi
- Webhook bildirimi
- SMTP e-posta bildirimi

### Bildirim Ayarları

Bildirim ekranındaki alanlar etiketlidir:

- Webhook URL
- SMTP sunucusu
- Port
- SMTP kullanıcı adı
- SMTP parolası
- Gönderen e-posta
- Alıcı e-posta
- SMTP SSL/TLS seçeneği

### Arayüz

- Dark, Light ve sade Servis teması
- Accent renk seçimi
- Tek pencere navigasyon
- Hazır yedek profilleri sol profil listesinde
- Yedek öncesi kontrol paneli
- Göz ikonlu parola göster/gizle kontrolleri
- Teknik servis akışına uygun yoğun ama okunabilir düzen

## Kurulum

### Kullanıcılar için

1. Releases sayfasından Setup veya Portable sürümü indirin.
2. VSS, sürücü dışa aktarımı ve bazı sistem konumları için uygulamayı yönetici olarak çalıştırın.
3. İlk açılışta varsayılan yedek hedefini ve tercih ettiğiniz temayı ayarlayın.
4. Profil listesinden hazır profillerden birini seçebilir veya kendi profilinizi kaydedebilirsiniz.

### Geliştiriciler için

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
├── src/ItchyBackup/
│   ├── Models/
│   ├── ViewModels/
│   ├── Views/
│   ├── Services/
│   └── Resources/Styles/
├── installer/
├── build.bat
└── README.md
```

Öne çıkan servisler:

- `BackupEngine`: Ana yedekleme motoru
- `RestoreEngine`: Geri yükleme motoru
- `ProfileService`: Varsayılan/hazır profiller ve profil yönetimi
- `BackupPreflightService`: Yedek öncesi kontrol
- `BackupCompareService`: İki yedeği karşılaştırma
- `BackupReportService`: HTML sonuç raporu
- `ChecksumService`: SHA-256 manifest ve doğrulama
- `NotificationService`: Toast, webhook ve SMTP bildirimi
- `ThemeService`: Tema ve accent renk yönetimi

## Yol Haritası

- Daha ayrıntılı e-posta şablonları
- ZIP içeriği için doğrudan yedek karşılaştırma
- Checksum tabanlı güvenli artımlı mod
- Otomatik güncelleme kontrolü
- Daha ayrıntılı müşteri teslim raporu

## Teknolojiler

- C# / WPF
- .NET 8
- CommunityToolkit.Mvvm
- SharpZipLib
- Newtonsoft.Json
- Windows Task Scheduler, VSS, pnputil, netsh

## Geliştirici

M. Mert
