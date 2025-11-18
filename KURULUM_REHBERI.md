# Crystal Reports to SSRS Converter - Kurulum Rehberi

## 📦 Hızlı Başlangıç

### Adım 1: Crystal Reports SDK Kurulumu

1. **SAP Crystal Reports for Visual Studio** indirin:
   - URL: https://www.sap.com/products/technology-platform/crystal-reports.html
   - Veya Visual Studio Marketplace'den "SAP Crystal Reports, developer version for Microsoft Visual Studio"

2. Crystal Reports SDK'yı kurun (tipik yol):
   ```
   C:\Program Files (x86)\SAP BusinessObjects\Crystal Reports for .NET Framework 4.0\
   ```

### Adım 2: Proje Açma

1. **Visual Studio 2017 veya üzeri** ile `CrystalToSSRS.sln` dosyasını açın

2. Solution Explorer'da projeye sağ tıklayın → **"Restore NuGet Packages"** (eğer varsa)

### Adım 3: Crystal Reports Referanslarını Güncelleme

**ÖNEMLİ**: Crystal Reports DLL yolları sisteminize göre farklı olabilir.

1. Solution Explorer'da **References** klasörünü açın

2. Aşağıdaki referanslara sağ tıklayıp **Properties** açın:
   - CrystalDecisions.CrystalReports.Engine
   - CrystalDecisions.Shared
   - CrystalDecisions.ReportSource

3. Her biri için **Path** değerini kontrol edin ve gerekirse güncelleyin

**Alternatif Yöntem** - .csproj dosyasını manuel düzenleme:

```xml
<Reference Include="CrystalDecisions.CrystalReports.Engine">
  <HintPath>C:\Program Files (x86)\SAP BusinessObjects\Crystal Reports for .NET Framework 4.0\Common\SAP BusinessObjects Enterprise XI 4.0\win32_x86\dotnet\CrystalDecisions.CrystalReports.Engine.dll</HintPath>
</Reference>
```

Yukarıdaki `<HintPath>` değerini kendi sisteminizin yoluna göre değiştirin.

### Adım 4: Derleme

1. Visual Studio menüsünden: **Build → Build Solution** (veya Ctrl+Shift+B)

2. Hata varsa:
   - Crystal Reports referanslarını kontrol edin
   - .NET Framework 4.7'nin kurulu olduğundan emin olun

3. Başarılı derleme sonrası:
   ```
   bin\Debug\CrystalToSSRS.exe
   ```

### Adım 5: İlk Çalıştırma

1. **Debug** veya **Release** modunda çalıştırın (F5)

2. Splash screen görünecek ve ana form açılacak

3. Test için bir Crystal Reports (.rpt) dosyası açın:
   - Dosya → RPT Aç
   - Rapor yapısını inceleyin
   - RDL Önizleme yapın

## 🔧 Sorun Giderme

### "Could not load file or assembly 'CrystalDecisions...' " Hatası

**Çözüm 1**: DLL yollarını kontrol edin
```bash
# PowerShell ile DLL'leri bulun:
Get-ChildItem -Path "C:\Program Files*" -Recurse -Filter "CrystalDecisions.*.dll" -ErrorAction SilentlyContinue
```

**Çözüm 2**: Copy Local = True yapın
- Her Crystal referansına sağ tık → Properties
- **Copy Local** = **True**

**Çözüm 3**: Crystal Reports Runtime'ı kurun
- Uygulamayı dağıtırken Crystal Reports Runtime gereklidir
- SAP'den ilgili runtime'ı indirin

### Build Hatası: "The type or namespace name 'CrystalDecisions' could not be found"

1. References klasöründe sarı ünlem işareti olan referansları silin
2. Add Reference → Browse ile DLL'leri manuel ekleyin
3. Yollar:
   ```
   C:\Program Files (x86)\SAP BusinessObjects\...\CrystalDecisions.CrystalReports.Engine.dll
   C:\Program Files (x86)\SAP BusinessObjects\...\CrystalDecisions.Shared.dll
   C:\Program Files (x86)\SAP BusinessObjects\...\CrystalDecisions.ReportSource.dll
   ```

### .NET Framework 4.7 Bulunamıyor

1. Control Panel → Programs → Turn Windows features on/off
2. ".NET Framework 4.7 Advanced Services" işaretleyin
3. Veya Visual Studio Installer'dan ekleyin

## 📚 Ek Bilgiler

### Geliştirme Araçları

**Önerilen IDE:**
- Visual Studio 2017 / 2019 / 2022 (Community, Professional veya Enterprise)

**Alternatif (Deneysel):**
- JetBrains Rider (Crystal Reports referansları manuel eklenmeli)

### Dağıtım İçin

Uygulamayı son kullanıcılara dağıtırken:

1. **Crystal Reports Runtime** kurulu olmalı
2. Veya gerekli DLL'leri uygulama klasörüne kopyalayın (Copy Local = True)
3. .NET Framework 4.7 gereklidir

### Lisans Bilgisi

- **Geliştirme**: SAP Crystal Reports for Visual Studio (Ücretsiz)
- **Dağıtım**: Uygulamanızı dağıtmak için Crystal Reports Runtime gerekir

## 📞 Destek

Sorun yaşarsanız:
1. README.md dosyasındaki "Sorun Giderme" bölümünü okuyun
2. Crystal Reports SDK versiyonunuzu kontrol edin
3. GitHub Issues'a bildirin (varsa)

## ✅ Kurulum Başarılı mı?

Test etmek için:
1. Uygulamayı çalıştırın
2. Dosya → RPT Aç
3. Örnek bir Crystal Reports dosyası açın
4. Sol panelde rapor yapısını görün
5. RDL Önizleme yapın

Eğer tüm adımlar sorunsuz çalışıyorsa, kurulum başarılıdır! 🎉

---

**Son Güncelleme**: 2025
**Versiyon**: 1.0.0
