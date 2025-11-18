# Crystal Reports to SSRS Converter

Crystal Reports (.rpt) dosyalarını SQL Server Reporting Services (SSRS) RDL formatına dönüştüren profesyonel bir masaüstü uygulaması.

## 🎯 Özellikler

### Ana Özellikler
- ✅ **RPT Dosya Okuma**: Crystal Reports dosyalarını tam olarak parse eder
- ✅ **Oracle Veri Kaynağı Desteği**: Oracle veritabanı bağlantılarını otomatik algılar
- ✅ **Milimetrik Kontrol**: Tasarım elemanlarını mm, pixel ve twips cinsinden düzenleyebilme
- ✅ **Formül Dönüştürücü**: Crystal Reports formüllerini SSRS Expression'a otomatik çevirir
- ✅ **RDL Üretimi**: SSRS 2016+ uyumlu RDL dosyaları oluşturur
- ✅ **Görsel Tasarım Editörü**: 1:1 ölçekli tasarım görünümü ve cetveller

### Formül Dönüştürme
- String fonksiyonları (ToText, UpperCase, Trim, vb.)
- Numerik fonksiyonları (Round, Abs, Ceiling, vb.)
- Tarih fonksiyonları (Year, Month, Day, vb.)
- If-Then-Else yapıları → IIF() dönüşümü
- Field referansları → Fields!FieldName.Value
- Parameter referansları → Parameters!ParamName.Value

### UI Özellikleri
- 📊 TreeView ile rapor yapısı görüntüleme
- 🔧 PropertyGrid ile milimetrik özellik düzenleme
- 📐 Yatay ve dikey cetveller (mm bazlı)
- 🎨 Grid sistemi ile hizalama
- 📝 Syntax highlighting ile RDL önizleme
- 📤 Toplu formül dönüştürme ve CSV export

## 📋 Gereksinimler

### Geliştirme Ortamı
- Visual Studio 2017 veya üzeri
- .NET Framework 4.7
- Windows Forms

### Gerekli Kütüphaneler
Crystal Reports SDK'ları (aşağıdaki DLL'ler gerekli):
- `CrystalDecisions.CrystalReports.Engine.dll`
- `CrystalDecisions.Shared.dll`
- `CrystalDecisions.ReportSource.dll`

**Not**: Crystal Reports SDK'larını SAP'ın resmi sitesinden indirmeniz gerekir.

## 🚀 Kurulum

### 1. Crystal Reports SDK Kurulumu
1. SAP Crystal Reports for Visual Studio'yu indirin ve kurun
2. SDK DLL'lerinin konumunu not edin (genellikle):
   ```
   C:\Program Files (x86)\SAP BusinessObjects\Crystal Reports for .NET Framework 4.0\Common\SAP BusinessObjects Enterprise XI 4.0\win32_x86\dotnet\
   ```

### 2. Proje Kurulumu
1. Solution'ı Visual Studio'da açın:
   ```
   CrystalToSSRS.sln
   ```

2. Crystal Reports referanslarını güncelleyin:
   - Solution Explorer'da `References` → `CrystalDecisions.*` referanslarına sağ tık
   - Properties'den doğru DLL yolunu gösterin

3. Projeyi derleyin:
   ```
   Build > Build Solution (Ctrl+Shift+B)
   ```

## 📖 Kullanım

### Temel Kullanım

1. **RPT Dosyası Açma**
   - Dosya → RPT Aç menüsünden Crystal Reports dosyanızı seçin
   - Rapor yapısı otomatik olarak analiz edilir

2. **Yapıyı İnceleme**
   - Sol panel: TreeView'da rapor bileşenleri
   - Sections, Parameters, Formulas, Tables görüntülenir

3. **Milimetrik Düzenleme**
   - TreeView'dan bir nesne seçin
   - PropertyGrid'de boyutları mm, px veya twips cinsinden düzenleyin
   - Değişiklikler anında tasarım panelinde görünür

4. **Formül Dönüştürme**
   - Formula node'una çift tıklayın
   - Crystal formülü otomatik olarak SSRS Expression'a çevrilir
   - Sonucu kopyalayabilir veya kaydedebilirsiniz

5. **RDL Oluşturma**
   - Dosya → RDL Olarak Kaydet
   - SSRS 2016 formatında RDL dosyası oluşturulur

### Toplu İşlemler

**Tüm Formülleri Dönüştürme:**
- Araçlar → Formül Dönüştürücü
- "Tümünü Dönüştür" butonuna basın
- Sonuçları CSV olarak dışa aktarabilirsiniz

**Veri Kaynağı Ayarları:**
- Araçlar → Veri Kaynağı Ayarları
- Oracle bağlantı parametrelerini düzenleyin

## 📁 Proje Yapısı

```
CrystalToSSRS/
├── Models/                          # Veri modelleri
│   └── CrystalReportModel.cs       # RPT yapısı model sınıfları
├── Converters/                      # Dönüştürücüler
│   ├── CrystalReportParser.cs      # RPT dosya parser
│   └── FormulaToExpressionConverter.cs  # Formül dönüştürücü
├── RDLGenerator/                    # RDL üretici
│   └── RDLBuilder.cs               # XML RDL oluşturucu
├── UI/                             # Kullanıcı arayüzü
│   ├── MainForm.cs                 # Ana form
│   ├── PropertyWrappers.cs         # Milimetrik kontrol wrappers
│   ├── FormulaConverterForm.cs     # Formül dönüştürme formu
│   ├── BatchFormulaConverterForm.cs # Toplu dönüştürme
│   ├── RdlPreviewForm.cs           # RDL önizleme
│   └── DataSourceSettingsForm.cs   # Veri kaynağı ayarları
├── Properties/                      # Proje özellikleri
│   ├── AssemblyInfo.cs
│   ├── Resources.resx
│   └── Settings.settings
├── Program.cs                       # Giriş noktası
├── App.config                       # Uygulama yapılandırması
├── CrystalToSSRS.csproj            # Proje dosyası
└── CrystalToSSRS.sln               # Solution dosyası
```

## 🎨 Ekran Görüntüleri

### Ana Ekran
- Sol: Rapor yapısı TreeView + Özellikler PropertyGrid
- Sağ: 1:1 ölçekli tasarım görünümü
- Üst: Menü ve araç çubuğu
- Alt: Durum çubuğu (pozisyon, boyut bilgisi)

### Milimetrik Kontrol
PropertyGrid'de her nesne için:
- **Pozisyon (mm)**: Sol, Üst
- **Boyut (mm)**: Genişlik, Yükseklik
- **Pozisyon (px)**: Pixel cinsinden değerler
- **Pozisyon (twips)**: Crystal Reports formatı

## 🔧 Gelişmiş Özellikler

### Formül Dönüştürme Örnekleri

**Crystal Reports:**
```vb
If {Order.Amount} > 1000 Then "Premium" Else "Standard"
```

**SSRS Expression:**
```vb
=IIF(Fields!Amount.Value > 1000, "Premium", "Standard")
```

**Crystal Reports:**
```vb
{Customer.FirstName} & " " & {Customer.LastName}
```

**SSRS Expression:**
```vb
=Fields!FirstName.Value + " " + Fields!LastName.Value
```

### Desteklenen Crystal Fonksiyonları

| Crystal Reports | SSRS Expression |
|----------------|-----------------|
| ToText()       | CStr()          |
| UpperCase()    | UCase()         |
| Round()        | Math.Round()    |
| Year()         | Year()          |
| CurrentDate    | Today           |
| IsNull()       | IsNothing()     |

## ⚠️ Bilinen Kısıtlamalar

1. **Subreport Desteği**: Şu anda subreport'lar desteklenmemektedir
2. **Chart Dönüşümü**: Grafik dönüşümleri için manuel işlem gerekir
3. **Cross-tab**: Cross-tab raporları için ek geliştirme gereklidir
4. **Formatting**: Bazı özel formatlamalar manuel düzeltme gerektirebilir

## 🐛 Sorun Giderme

### Crystal Reports DLL'leri Bulunamıyor
**.csproj** dosyasındaki `<Reference>` elementlerinin `<HintPath>` değerlerini kendi sistem yolunuza göre güncelleyin.

### RDL Dosyası SSRS'de Açılmıyor
- RDL Önizleme → XML Validasyonu ile kontrol edin
- SSRS sürümünüzün 2016 veya üzeri olduğundan emin olun

### Formül Dönüştürme Hatası
- Desteklenmeyen fonksiyonlar manuel olarak düzeltilmelidir
- Hata mesajında hangi kısmın sorunlu olduğu gösterilir

## 📝 TODO / Geliştirme Planı

- [ ] Subreport desteği ekle
- [ ] Chart dönüşümlerini otomatikleştir
- [ ] Cross-tab desteği
- [ ] Barcode desteği
- [ ] PDF export
- [ ] Batch (toplu) dosya dönüştürme
- [ ] Configuration dosyası ile mapping customization
- [ ] Unit testler

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/AmazingFeature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add some AmazingFeature'`)
4. Branch'inizi push edin (`git push origin feature/AmazingFeature`)
5. Pull Request açın

## 📄 Lisans

Bu proje şahsi ve ticari kullanım için ücretsizdir.

## 👤 İletişim

Proje geliştirici: Recep Şahin
Email: receptive61@gmail.com

## 🙏 Teşekkürler

- SAP Crystal Reports SDK
- Microsoft SQL Server Reporting Services
- .NET Framework Community

---

**Not**: Bu araç IFS de kullanılan Crystal Reports'tan SSRS'e geçiş sürecini kolaylaştırmak için tasarlanmıştır. Karmaşık raporlar için manuel kontrol ve düzeltme gerekebilir.
