# 🗄️ Itchy Backup — Teknik Servis Yedekleme Aracı

Teknik servis ortamları için geliştirilmiş kapsamlı Windows yedekleme programı.

---

## 📦 Özellikler

| Özellik | Detay |
|---|---|
| **Kullanıcı Klasörleri** | Masaüstü, Belgelerim, İndirilenler, Resimler, Videolar, Müzik, AppData |
| **Tarayıcılar** | Chrome, Firefox, Edge, Opera, Brave, Vivaldi — şifreler, yer imleri, çerezler |
| **Outlook** | PST, OST, imzalar, şablonlar, NK2 otomatik tamamlama |
| **Veritabanları** | Firebird, SQLite, SQL Server, Access, MySQL — otomatik tespit |
| **Sanal Makineler** | VMware (.vmdk/.vmx), VirtualBox (.vdi/.vbox), Hyper-V (.vhdx) |
| **Bulut Depolama** | OneDrive, Google Drive, MEGA, Dropbox — yerel dosyalar |
| **VSS Desteği** | Açık PST, OST, SQL dosyalarını Volume Shadow Copy ile kopyalar |
| **AES-256 Şifreleme** | ZIP ile şifreli yedek alınabilir |
| **SHA-256 Doğrulama** | Her yedek sonrası checksum manifest oluşturulur ve doğrulanır |
| **Hot Backup Uyarısı** | SQL Server, Firebird, MySQL, Outlook çalışıyorsa uyarı verir |
| **Profil Sistemi** | Ayarları JSON profili olarak kaydet/yükle |
| **Log Dosyası** | Her yedek işlemi için detaylı log dosyası |
| **Eski Yedek Temizliği** | Son N yedek saklanır, eskiler otomatik silinir |
| **Tarih Damgası** | `Yedek_2025-05-06_14-32` formatında klasör adı |

---

## 🛠️ Geliştirme Ortamı Kurulumu

### Gereksinimler
- **Visual Studio 2022** veya **VS Code + C# Dev Kit**
- **.NET 8 SDK** — [İndir](https://dotnet.microsoft.com/download/dotnet/8.0)
- **WiX Toolset v4** (Setup için opsiyonel) — `dotnet tool install --global wix`

### Projeyi Klonla ve Aç
```bash
git clone https://github.com/Bogazitchy/ItchyBackup.git
cd ItchyBackup
```

Visual Studio'da `src\ItchyBackup\ItchyBackup.csproj` dosyasını aç.

### NuGet Paketleri
Proje ilk açıldığında otomatik restore edilir. Manuel restore:
```bash
cd src/ItchyBackup
dotnet restore
```

### Kullanılan Paketler
| Paket | Versiyon | Amaç |
|---|---|---|
| CommunityToolkit.Mvvm | 8.3.2 | MVVM altyapısı |
| Newtonsoft.Json | 13.0.3 | Profil JSON serialization |
| SharpZipLib | 1.4.2 | ZIP + AES-256 şifreleme |
| System.Management | 8.0.0 | WMI/servis sorgulama |
| AlphaVSS.Win10x64 | 2.0.1 | Volume Shadow Copy (VSS) |

---

## 🔨 Build

### Hızlı Build (Windows)
```
build.bat
```

Bu script:
1. Uygulamayı Release modda publish eder
2. `build\output\ItchyBackup_v1.0.0_portable.exe` oluşturur
3. WiX kuruluysa `ItchyBackup_v1.0.0_Setup.msi` de oluşturur

### Manuel Build
```bash
dotnet publish src/ItchyBackup/ItchyBackup.csproj \
  -c Release \
  -r win-x64 \
  --self-contained false \
  -p:PublishSingleFile=true \
  -o build/publish
```

### Setup.exe (WiX)
```bash
dotnet tool install --global wix
cd installer
wix build ItchyBackup.wxs -d "SourceDir=..\build\publish" -out ..\build\output\Setup.msi
```

---

## 🚀 Kullanım

1. **Yönetici olarak çalıştır** (VSS ve sistem dosyaları için zorunlu)
2. Sol panelden **kategorileri** genişlet, yedeklenecek öğeleri işaretle
3. Sağ panelden **hedef klasörü** seç
4. İsteğe bağlı: ZIP sıkıştırma, AES-256 şifreleme, VSS, checksum ayarla
5. **Yedeği Başlat** butonuna tıkla

### Profil Kaydetme
Ayarları "Profil Olarak Kaydet" ile kaydet → bir sonraki bilgisayarda sol panelden yükle.

---

## 📁 Proje Yapısı

```
ItchyBackup/
├── src/ItchyBackup/
│   ├── Models/           # BackupCategory, BackupItem, BackupProfile, BackupProgress
│   ├── ViewModels/       # MainViewModel (MVVM)
│   ├── Views/            # MainWindow.xaml
│   ├── Services/
│   │   ├── BackupEngine.cs       # Ana yedekleme motoru
│   │   ├── CategoryBuilder.cs    # Kategori & otomatik tespit
│   │   ├── ChecksumService.cs    # SHA-256 manifest
│   │   ├── ZipService.cs         # ZIP + AES-256
│   │   ├── VssService.cs         # Volume Shadow Copy
│   │   ├── HotBackupDetector.cs  # Çalışan servis tespiti
│   │   ├── ProfileService.cs     # Profil kaydet/yükle
│   │   ├── LogService.cs         # Log dosyası
│   │   └── NotificationService.cs # Windows bildirim
│   ├── Helpers/
│   │   └── Converters.cs         # XAML value converters
│   └── Resources/
│       └── Styles/               # Dark tema XAML
├── installer/
│   └── ItchyBackup.wxs          # WiX installer tanımı
├── build.bat                    # Tek tıkla build scripti
└── README.md
```

---

## ⚠️ Önemli Notlar

### VSS (Volume Shadow Copy)
- VSS için **Yönetici yetkisi** zorunlu (app.manifest ile otomatik talep edilir)
- Windows 7+ destekler
- SQL Server veya Outlook açıkken PST/MDF dosyalarını VSS ile kopyalar

### Tarayıcı Verileri
- Tarayıcı **kapalıyken** yedek almak tercih edilir (kilitli profil dosyaları sorun yaratabilir)
- Chrome/Edge şifre veritabanı (`Login Data`) kopyalanır; şifreler Windows DPAPI ile şifrelenmiştir, başka PC'de doğrudan açılmaz

### SQL Server Hot Backup
- SQL Server çalışıyorsa VSS ile yedek alınır
- Tam tutarlılık için SQL Server Agent backup job'u tercih edilir

---

## 📋 Yol Haritası (v2)

- [ ] Zamanlayıcı (Görev Zamanlayıcısı entegrasyonu)
- [ ] Ağ paylaşımına yedekleme (SMB/UNC)
- [ ] Artımlı yedekleme (sadece değişen dosyalar)
- [ ] Çoklu dil desteği
- [ ] Yedek geçmişi görünümü

---

*Itchy Backup — Teknik servis iş akışı için tasarlandı.*
