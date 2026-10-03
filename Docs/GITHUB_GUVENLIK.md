# GitHub paylaşım güvenliği — 3 Ekim 2026

**Mevcut dosyalar temizlendi; eski Git geçmişi temiz değil. Repo henüz herkese açılmamalı.** Commit/push, force-push ve GitHub görünürlük değişikliği yapılmadı. Bu rapor anahtar değerlerini içermez.

## Bulunanlar ve yapılan değişiklikler

| Dosya | Bulgu / işlem |
|---|---|
| `Assets/Scripts/AIHintManager.cs` | Dört gerçek Google/Gemini API anahtarı kaynak koddaydı. Sabit anahtarlar kaldırıldı. Kod artık isteğe bağlı yerel JSON dosyasını okuyor. Kod Git'te kalabilir. |
| `Assets/Scenes/GAMESCENE.unity` | Aynı dört anahtar `apiKeys` alanında sahneye kaydedilmişti (inceleme öncesinde yaklaşık satır 1493). Bu alan kaldırıldı; oyun sahnesi Git'te kalabilir. |
| `Assets/Resources/LocalSecrets/GeminiSettings.json` | Dört anahtar yerel kullanım korunarak buraya taşındı. Klasör ve Unity meta dosyaları ignore edildi. Dosya düz metindir; sadece Git paylaşımından korunur. |
| `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset` | Photon Realtime App ID var. App ID istemcide kullanılan bir tanımlayıcıdır, hesap şifresi/sunucu secret değildir. Kullanıcının kendi servis yapılandırması olarak asset ve meta Git takibinden çıkarıldı, ignore edildi. Yerel dosyalar korundu. |
| `Assets/PlayFabSDK/Shared/Public/Resources/PlayFabSharedSettings.asset` | Title ID mevcut; DeveloperSecretKey boş. Title ID gizli anahtar değildir. İleride secret eklenerek yanlışlıkla paylaşılmasını önlemek için asset ve meta ignore edildi, takipten çıkarıldı. DeveloperSecretKey oyun istemcisine hiçbir zaman eklenmemeli. |
| `Assets/PlayFabEditorExtensions/Editor/Resources/PlayFabEditorPrefsSO.asset` | İncelenen dosyada token/parola yok, yerel SDK/editor tercihleri var. Yerel ayar ve meta ignore edildi, takipten çıkarıldı. |
| `Docs/Accounts/*` | Canlı test hesap kimlikleri ve oturum açılmış panel ekran görüntüleri bulunuyor. README hariç paylaşım dışında tutuldu; resimler için otomatik metin taraması yeterli değildir. |
| `Assets/Editor/PhotonAccountValidation.cs` | Sabit Photon App ID kaldırıldı; test yerel Photon ayarından okuyor. |
| `Assets/Resources/PerformanceTestRun*.json*` | Makineye/koşuya ait üretilen test bilgileri ignore edildi. |

`.env` dosyaları, `secrets/`, `credentials/`, imzalama anahtarları (`.jks`, `.keystore`, `.p12`, `.pfx`, `.pem`, `.key`) da ignore edildi. Örnek `.env.example` dosyaları paylaşılabilir. Library/Temp/.utmp/Builds/log/APK kuralları önceden vardı ve korundu.

`git rm --cached` sadece Git indeksindeki takibi kaldırdı; yerel ayar dosyaları silinmedi. `git status` içinde bunların staged `D` görünmesi beklenir. Bu kaldırmaları, temizlenmiş kodu/sahneyi ve `.gitignore` değişikliğini birlikte commit etmek gerekir. Henüz commit oluşturulmadı; proje daha önceki çalışmalardan başka değişiklikler de içeriyor.

## Eski commit'ler — paylaşımı engelleyen bulgu

`1c93062` ve `0fdb592` commit'lerinde hem `AIHintManager.cs` hem `GAMESCENE.unity` gerçek Gemini anahtarlarını içeriyor. Yeni bir temiz commit yapmak veya dosyayı ignore etmek bu geçmişi silmez. Yerelde erişilebilen tüm Git ref'leri tarandı; GitHub'daki erişilemeyen PR referansları, önbellekler veya fork'lar bu taramanın kapsamı değildir.

1. Google AI Studio/Cloud'da bu dört eski anahtarı iptal et ve gerekiyorsa yeni anahtar üret. Yeni anahtarları yalnızca yerel ayara koy. Önceki `AdamAsmaca1.4.apk` anahtar içeren kaynaklardan üretildi; anahtarları içeriyor kabul et. Eski APK'yı yayımlama.
2. Mevcut repoyu public yapmadan önce geçmişi de temizle. İki seçenek var: temiz mevcut dosyalardan, `.git` klasörünü taşımadan yeni bir public repo oluşturmak; veya ayrı bir klonda `git-filter-repo` ile gizli verileri geçmişten kaldırıp mevcut repo geçmişini yeniden yazmak. İkinci yol commit kimliklerini değiştirir ve force-push gerektirir; bu çalışmada uygulanmadı. Eski private repoyu yeni public repoyla karıştırma.
3. Geçmişi yeniden yazarken yalnızca Gemini değil, yerel Photon/PlayFab ayar dosyalarını da geçmişten çıkar; mevcut kaynak/sahne geçmişi için anahtar değiştirme filtresi kullan veya bu iki dosyanın geçmiş sürümlerini kaldır. Yeni klon üzerinden doğrula. Diğer kişiler eski klonlarından temizlenmemiş geçmişi geri push etmemeli.
4. GitHub secret scanning uyarılarını kontrol et. Anahtarı iptal etmek, geçmişteki kopyayı geçersiz kılar; yine de temiz geçmişle paylaşım yap.

GitHub'ın [gizli verileri kaldırma rehberi](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository) anahtarı iptal/değiştirmeyi ve geçmiş temizliğinin sınırlarını açıklıyor.

## Kendin nasıl kontrol edebilirsin?

Proje kökünde PowerShell aç:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Check-PublicSecrets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Check-PublicSecrets.ps1 -History
git check-ignore -v --no-index Assets/Resources/LocalSecrets/GeminiSettings.json
git check-ignore -v --no-index Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset
git ls-files -- Assets/Resources/LocalSecrets Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset Assets/PlayFabSDK/Shared/Public/Resources/PlayFabSharedSettings.asset
git status --short
```

İlk tarama mevcut, Git'e girebilecek metin dosyalarında PASS vermeli. İkinci tarama bugün **4 geçmiş dosya/commit bulgusu** ile başarısız oluyor; geçmiş temizlenmeden PASS bekleme. `git check-ignore` doğru `.gitignore` kuralını göstermeli. `git ls-files` gizlenen ayar dosyaları için boş dönmeli. Tarama anahtarın kendisini yazmaz, yalnızca dosya/konum bildirir.

Commit öncesinde değişiklikleri kendi bilgisayarında gözden geçir; `git add` sonrasında staged dosyaları da kontrol et. `git add -f` kullanma; ignore korumasını aşar. Metin tarayıcı Google anahtarları, yaygın GitHub/AWS anahtarları, private-key başlıkları ve dolu PlayFab DeveloperSecretKey alanlarını kontrol eder. Her anahtar biçimini, resimleri veya APK içindeki verileri algılamaz; PASS tam güvenlik garantisi değildir.

## Yerel oyun ve temiz klon kurulumu

Mevcut bilgisayarda Photon/PlayFab dosyaları korunmuştur. Gemini için yerel `Assets/Resources/LocalSecrets/GeminiSettings.json` biçimi:

```json
{"apiKeys":["REPLACE_WITH_YOUR_LOCAL_KEY"]}
```

Anahtar yoksa oyun açılabilir, Gemini ipucu anahtar bulunamadığını gösterir; bu durumda altın kesilmez. Public klonda anahtar gelmez.

Temiz klonda `Docs/PublicSetup/PhotonServerSettings.asset.example` ve `PlayFabSharedSettings.asset.example` dosyalarını yukarıdaki özgün Resources yollarına, `.example` uzantısını kaldırarak kopyala; yanlarındaki `.meta.example` örneklerini de özgün `.asset.meta` adlarıyla kopyala. Unity Inspector'da kendi App ID / Title ID'ni gir. Örneklerde servis ID'leri ve secret alanı boş bırakıldı. PlayFab editor tercih dosyası eklenti tarafından yeniden oluşturulabilir. Bu dosyalar yeniden oluşturulduğunda da ignore edilir.

**APK güvenliği ayrı:** Resources içindeki yerel JSON build'e girer. Bu yöntem GitHub kaynak paylaşımını temizler, telefondaki anahtarı gizlemez. Dağıtılacak sürümde Gemini isteği sunucuda yapılmalı; anahtar APK'ya girmemeli. Google'ın [API anahtarı güvenliği rehberi](https://ai.google.dev/gemini-api/docs/api-key) istemci uygulamalarında backend proxy kullanılmasını öneriyor. Bu çalışmada backend proxy uygulanmadı.

## Doğrulama

Mevcut Git'e uygun metin taraması PASS; geçmiş taraması beklenen 4 bulguyu yakaladı. Dört anahtarın yerel JSON'a taşındığı ve ayar dosyalarının diskte durduğu doğrulandı. Güncel oyun/editor C# derlemesi geçti (önceden mevcut SDK/obsolete API uyarıları var). Telefonda yeni build alınmadı, gerçek Gemini çağrısı yapılmadı. PlayFab sunucu secret'ı veya GitHub token'ı belirtilen taramalarda bulunmadı; yeni anahtarların iptal/oluşturma işlemleri yapılmadı.
