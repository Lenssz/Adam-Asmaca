# Android build düzeltmesi — 3 Ekim 2026

Unity 6000.3.6f1 / Gradle 8.13 / AGP 8.10.0 build hatası `:launcher:mergeLibDexDebug` görevinde oluşuyordu. Üst mesaj `Could not resolve all files for configuration ':launcher:debugRuntimeClasspath'` idi. Ayrıntıda DexingNoClasspathTransform, mevcut proje kökünün dışındaki `C:\Users\5070\AdamAsmaca\...\UnityPlayerGameActivity$GameActivitySurfaceView.class` yolunu kullanıyordu.

Mevcut proje `C:\Users\ahmet\Downloads\AdamAsmaca\AdamAsmaca`. Taşınan eski projenin Gradle artımlı çıktıları yeni konumda kaldığı için kök dizin uyuşmazlığı oluşmuştu.

Yalnızca `Library/Bee/Android/Prj/IL2CPP/Gradle` altındaki `.gradle`, `launcher/build`, `unityLibrary/build` geçici çıktıları temizlendi. Kaynak dosyalar veya proje ayarları değiştirilmedi. Kök `build` klasöründeki kilitli eski HTML raporu temizlenemedi; derlemeyi engellemedi ve yeni rapor oluştu.

Unity'nin mevcut üretilmiş Android projesi aynı JDK/Gradle ile `assembleDebug --no-build-cache --console=plain` komutuyla tekrar derlendi. **BUILD SUCCESSFUL in 40s; 66 actionable tasks: 66 executed.** Önceden başarısız mergeLibDexDebug geçti. Beni hatırla Android Java sınıfı da APK build'inde derlendi.

APK: `Builds/Android/AdamAsmaca-debug-20261003-0450.apk` (debug, arm64, yaklaşık 69 MiB). Gradle çıktısı `.utmp/android-gradle-repair.log`. Bu doğrulama Gradle paketleme aşamasını yeniden çalıştırdı; açık Unity Editor kapatılmadı. Fiziksel telefonda kurulum/oyun testi yapılmadı.

Proje başka konuma taşınırken üretilmiş `Library/Bee/Android` ve Gradle build önbelleklerini taşımayın. Yeni konumda yeniden üretilmelerine izin verin. Gradle 9 deprecation ve SDK read-only uyarıları bu başarılı build'in engeli değildi.

## Tam Unity Android build — AdamAsmaca1.4

Sonraki Editor denemesi 18 hata ile erken durdu. Log, `Run script only build` aşamasında `PPtr cast failed when dereferencing! Casting from Texture2D to MonoScript at FileID 2800000!` hatalarını gösterdi. Kaynak YAML dosyalarında bu Texture2D fileID'sini kullanan hatalı `m_Script` kaydı bulunmadı. Artımlı build verilerinin etkisini kaldırmak için güncel Assets ve ProjectSettings, `.utmp/drawing-validation-project` doğrulama kopyasına aktarıldı. Kullanıcının açık Editor oturumu kapatılmadı.

Unity 6000.3.6f1 ile `buildScriptsOnly=false`, `buildAppBundle=false`, `BuildOptions.CleanBuildCache`, Android hedefi ve üç etkin oyun sahnesi kullanılarak tam IL2CPP build alındı. **Build Finished, Result: Success; errors=0.** Önceki PPtr hatası bu build'de tekrarlanmadı. Log: `.utmp/android-full-phone-build.log`; özet: `.utmp/android-phone/build-result.txt`.

Teslim edilen APK: `C:/Users/ahmet/Downloads/AdamAsmaca1.4.apk`; gerçek dosya boyutu 54.807.238 bayt (52,27 MiB). Paket `com.Lenssz.AdamAsmaca`, sürüm adı `1.4`, versionCode `1`, min SDK 25, target SDK 36, arm64-v8a. `apksigner verify --verbose` APK v2 imzasını doğruladı; bir imzalayan var. Bu doğrudan telefon kurulumu için APK'dır; mağaza yayını yapılmadı. Fiziksel telefonda kurulum ve çalışma kontrolü yapılmadı.

Açık asıl Editor'un eski artımlı build verileri bu işlemle yeniden oluşturulmadı. Sonraki Editor build'inde yalnızca script build'i yerine tam/clean build kullanılmalıdır.
