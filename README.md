<div align="center">

<img src="src/ItchyBackup/Resources/Icons/app.ico" width="80" height="80" alt="Itchy Backup Logo"/>

# Itchy Backup

**Teknik servis ortamları için geliştirilmiş kapsamlı Windows yedekleme aracı**

[![Version](https://img.shields.io/badge/version-v0.7-6C5CE7?style=flat-square)](https://github.com/Bogazitchy/Itchy-Backup/releases)
[![Platform](https://img.shields.io/badge/platform-Windows-0078D4?style=flat-square&logo=windows)](https://github.com/Bogazitchy/Itchy-Backup/releases)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-00CEC9?style=flat-square)](LICENSE)

[📥 İndir](#-indirme) • [✨ Özellikler](#-özellikler) • [🛠️ Kurulum](#-kurulum) • [📸 Ekran Görüntüleri](#-ekran-görüntüleri)

</div>

---

## 📥 İndirme

| Sürüm | Açıklama | İndir |
|---|---|---|
| **Setup (.exe)** | Kurulum sihirbazı — önerilen | [⬇ Setup indir](https://github.com/Bogazitchy/Itchy-Backup/releases/latest) |
| **Portable (.exe)** | Kurulum gerektirmez, direkt çalıştır | [⬇ Portable indir](https://github.com/Bogazitchy/Itchy-Backup/releases/latest) |

> Her iki sürüm de **.NET 8 Runtime dahildir** — ek kurulum gerekmez.

---

## ✨ Özellikler

### 🗂️ Yedekleme Kategorileri

| Kategori | İçerik |
|---|---|
| **Kullanıcı Klasörleri** | Masaüstü, Belgelerim, İndirilenler, Resimler, Videolar, Müzik, AppData |
| **Tarayıcı Verileri** | Chrome, Firefox, Edge, Opera, Brave, Vivaldi — şifreler, yer imleri, çerezler |
| **Outlook / Mail** | PST, OST dosyaları, imzalar, şablonlar, otomatik tamamlama |
| **Veritabanları** | Firebird, SQLite, SQL Server, Access — otomatik tespit |
| **Sanal Makineler** | VMware (.vmdk/.vmx), VirtualBox (.vdi/.vbox), Hyper-V (.vhdx) |
| **Bulut Depolama** | OneDrive, Google Drive, MEGA, Dropbox — yerel dosyalar |
| **Özel Klasörler** | İstediğiniz herhangi bir klasör veya konumu ekleyin |

### 🔐 Güvenlik & Doğrulama
- **AES-256 şifreleme** — ZIP yedeklerine şifre koruması
- **SHA-256 checksum** — Yedek tamamlandıktan sonra otomatik doğrulama
- **VSS (Volume Shadow Copy)** — Açık PST, OST, SQL dosyalarını kopyalar
- **Hot Backup tespiti** — SQL Server, Firebird, Outlook açıksa uyarı verir

### ♻️ Geri Yükleme
- Mevcut yedek listesini otomatik görüntüleme
- **Hiyerarşik klasör ağacı** — iki seviyeli alt klasör görünümü, seçim alt klasör bazında çalışır
- **Kısmi geri yükleme** — yalnızca seçilen klasörleri/alt klasörleri geri yükle
- ZIP yedeklerini şifre ile geri yükleme
- Mevcut dosyaların üzerine yazma seçeneği
- Canlı ilerleme ve iptal desteği

### ⏰ Otomasyon
- **Zamanlayıcı** — Gün ve saat seçerek otomatik yedekleme
- **Windows Görev Zamanlayıcısı** entegrasyonu
- **Profil sistemi** — Ayarları kaydedip farklı bilgisayarlarda yükle
- **Profil düzenleme** — Mevcut profili düzenle, ad değiştir, oluşturma tarihi korunur

### 📊 İzleme & Raporlama
- **Canlı ilerleme çubuğu** — Dosya bazlı gerçek zamanlı ilerleme
- **Hız göstergesi** — Anlık kopyalama hızı (MB/s)
- **Tahmini süre** — Kalan süre tahmini
- **Yedekleme sonuç raporu** — Tamamlanan yedek için özet
- **Geçmiş** — Son 50 yedeğin listesi ve durumu
- **Log dosyası** — Her yedek için detaylı kayıt

### 🎨 Arayüz
- **Yenilenen dark tema** — derin mor gradyan arka plan, pencere üzerinde glow efekti
- Logo animasyonu — nefes alan ışık efekti (titbar)
- **Yeniden tasarlanan pencere butonları** — özel vektör ikonlar, kapat butonu kırmızıya döner (Windows 11 stili)
- Tek pencere navigasyon (Yedek Seç, Geçmiş, Zamanlayıcı, Geri Yükle, Ayarlar)
- Yedek bitince klasörü otomatik açma

---

## 🛠️ Kurulum

### Kullanıcılar İçin

1. [Releases](https://github.com/Bogazitchy/Itchy-Backup/releases/latest) sayfasından Setup veya Portable sürümü indir
2. **Setup:** Kurulum sihirbazını çalıştır (yönetici yetkisi gerekir)
3. **Portable:** `.exe` dosyasını sağ tıkla → **Yönetici olarak çalıştır**

> ⚠️ **VSS ve sistem dosyaları için yönetici yetkisi zorunludur.**

### Geliştiriciler İçin

**Gereksinimler:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 veya VS Code
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) *(setup oluşturmak için opsiyonel)*

**Derleme:**
```bash
git clone https://github.com/Bogazitchy/Itchy-Backup.git
cd Itchy-Backup
```

Visual Studio'da `ItchyBackup.sln` dosyasını açın veya:

```bash
# Portable ve Setup (Runtime dahil, tek komut)
build.bat
```

`build.bat` çalıştırıldığında `build/output/` altında şunlar üretilir:
- `ItchyBackup_v0.7_portable.exe` — runtime dahil tek dosya
- `ItchyBackup_v0.7_Setup.exe` — kurulum sihirbazı (runtime dahil)

---

## 📁 Proje Yapısı

```
ItchyBackup/
├── src/ItchyBackup/
│   ├── Models/               # Veri modelleri
│   ├── ViewModels/           # MVVM ViewModels
│   ├── Views/                # WPF XAML arayüzler
│   ├── Services/
│   │   ├── BackupEngine.cs       # Ana yedekleme motoru
│   │   ├── RestoreEngine.cs      # Geri yükleme motoru + BackupFolderItem
│   │   ├── CategoryBuilder.cs    # Kategori & otomatik tespit
│   │   ├── ChecksumService.cs    # SHA-256 doğrulama
│   │   ├── ZipService.cs         # ZIP + AES-256
│   │   ├── VssService.cs         # Volume Shadow Copy
│   │   ├── HotBackupDetector.cs  # Çalışan servis tespiti
│   │   ├── NetworkShareHelper.cs # SMB/UNC bağlantısı
│   │   ├── NotificationService.cs# Bildirim servisi
│   │   ├── ProfileService.cs     # Profil kaydet/yükle/sil
│   │   └── LogService.cs         # Log dosyası
│   └── Resources/Styles/     # Dark tema XAML
├── installer/
│   └── ItchyBackup.iss       # Inno Setup script
├── build.bat                 # Build scripti
└── README.md
```

---

## 🗺️ Yol Haritası

- [x] Kullanıcı klasörleri yedekleme
- [x] Tarayıcı verileri yedekleme
- [x] Outlook PST/OST yedekleme
- [x] Veritabanı tespiti ve yedekleme
- [x] Sanal makine yedekleme
- [x] ZIP + AES-256 şifreleme
- [x] SHA-256 checksum doğrulama
- [x] VSS (açık dosya) desteği
- [x] Profil sistemi
- [x] Otomatik zamanlayıcı
- [x] Özel klasör ekleme
- [x] Yedekten geri yükleme (hiyerarşik klasör ağacı, alt klasör bazlı seçim)
- [x] Profil düzenleme (ad değiştirme, güncelleme)
- [x] Ağ paylaşımına yedekleme (SMB/UNC)
- [x] Artımlı yedekleme
- [x] Setup içinde .NET 8 Runtime dahil (bağımsız kurulum)
- [x] Yenilenen arayüz (gradyan tema, animasyonlar, vektör pencere ikonları)
- [ ] Yedek karşılaştırma (kaldırıldı — yeniden tasarlanacak)
- [ ] E-posta bildirimi

---

## 🧰 Kullanılan Teknolojiler

- **C# / WPF** (.NET 8, Windows)
- **CommunityToolkit.Mvvm** — MVVM altyapısı
- **SharpZipLib** — ZIP & AES-256 şifreleme
- **Newtonsoft.Json** — Profil & ayar yönetimi
- **System.ServiceProcess** — Servis tespiti

---

## 👤 Geliştirici

**M. Mert** — [@Bogazitchy](https://github.com/Bogazitchy)

---

<div align="center">

**Itchy Backup** — Teknik servis iş akışı için tasarlandı.

⭐ Beğendiyseniz star atmayı unutmayın!

</div>
