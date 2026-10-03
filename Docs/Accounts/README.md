# PlayFab hesapları ve arkadaşla 1v1

Title ID: `13C073`. Klasik CloudScript `Backend/PlayFab/social.js`, 3 Ekim 2026 tarihinde Game Manager'da **Revision 2 (live)** olarak yayımlandı. Revision 1 panelde geri dönüş için duruyor.

## İstemci

- `AccountService`: kullanıcı adı/şifreyle kayıt ve giriş; kullanıcı adı cihazda hatırlanır, şifre/oturum bileti kaydedilmez. **Beni hatırla** isteğe bağlıdır ve başlangıçta kapalıdır. Seçilince oturumu açık hesap için kriptografik rastgele 256 bit `CustomId` üretilir ve PlayFab'a bağlanır. Sonraki açılışta `LoginWithCustomID(CreateAccount=false)` aynı PlayFab ID'ye giriş yapar; kimlik farklıysa kayıt reddedilir. Misafir tek oyunculu oyun devam eder; çevrimiçi oyun giriş ekranından geçer.
- Kullanıcı adı 3–20 ASCII harf/rakam, şifre 6–100 karakter. Canlı PlayFab testi alt çizgi içeren kullanıcı adını reddettiği için aynı kural istemcide uygulanır. Görünen profil ismi giriş kullanıcı adından ayrıdır; hesaplı oyuncularda PlayFab'ın 3 karakter minimumu ve oyunun 10 karakter maksimumu uygulanır.
- `PaperAccountUI`: giriş/kayıt, profil, ID kopyalama, arkadaş arama/liste/gelen/gönderilen, kategori seçimi ve davet bildirimi. Var olan kâğıt, basma ve silgi geçişleri kullanılır. Ana sahneye servis tarafından çalışma zamanında bağlanır.
- `FriendsService`: 5 saniye kutu yenileme, 20 saniye müsaitlik, ön plan kontrolü; sunucu 60 saniye eski durumu çevrimdışı sayar. Listeler beş kayıtlık sayfalarla alınır. İstemcinin PlayFab `GetAccountInfo` çağrısı kullanıcı adını ID'ye çevirir; sosyal işlemler oturum kimliğiyle CloudScript üzerinden yürür.
- `FriendMatchService`: `eu`, `paper-social-v1`, görünmeyen iki kişilik özel oda, davetli ID rezervasyonu, `C` kategori özelliği ve iki kimlik doğrulaması. 60 saniyelik davet, ret/iptal/ayrılma temizliği; mevcut çok oyunculu oyun/rövanş sahnesi kullanılır. İki gerçek Photon istemcisiyle özel odaya davet/kabul/katılma doğrulandı.

Altın cihazdaki mevcut kayıtta kalır. Sohbet, push ve kısa arkadaş kodu eklenmedi. Sunucu gizli anahtarı istemciye eklenmedi.

## Beni hatırla (3 Ekim 2026)

Kullanıcı, önceki sadece kullanıcı adı hatırlama kararını değiştirerek yeniden açılışta otomatik giriş istedi. Şifre yerine rastgele cihaz giriş anahtarı kaydedilir. Windows/Windows Editor'de CurrentUser DPAPI ile şifreli dosya, Android API 23+ üzerinde Android Keystore AES-GCM anahtarı ve şifreli uygulama özel SharedPreferences kaydı kullanılır. Projenin minimum Android SDK'sı 25'tir. Başka platformlarda seçenek kapalıdır; düz metin yedek yöntem yoktur. Yeni kodlar `RememberedAccountStore.cs` ve `Assets/Plugins/Android/RememberedAccount.java`.

Çıkışta yerel kayıt hemen temizlenir ve çevrimiçiyken `UnlinkCustomID` gönderilir. Çevrimdışı çıkışta sunucu tarafındaki kaldırma gerçekleşmeyebilir; yerel anahtar silinir. İnternet hatasında otomatik giriş kaydı korunur ve menüden tekrar denenebilir; tek oyunculu oyun kullanılabilir. Bilinmeyen/silinmiş veya engellenmiş hesapta kayıt temizlenir; yanlışlıkla yeni misafir hesabı oluşturulmaz. Oturum değiştirme sırasında eski isteğin sonucu yeni hesaba uygulanmaz. PlayFab'ın CustomId ilişkilendirmesi bir hesap alanıdır; başka bir cihazda yeni anahtar bağlanınca önceki cihazın anahtarı geçersizleşebilir. Anahtar, parola değişikliğinden ayrı bir giriş kimliğidir; bu sürümde tüm cihazlardan çıkış ekranı yoktur.

Canlı `RememberLoginValidation.RunBatch` PASS: kayıt + anahtar bağlama, Windows şifreli dosyada şifre/kullanıcı adı/anahtar düz metninin bulunmaması, SDK oturumu ve servis nesneleri sıfırlandıktan sonra aynı hesabın şifresiz geri gelmesi, profil ID'si, çıkışta yerel kayıt ve canlı ilişkinin kaldırılması, seçilmemiş seçenekte kayıt olmaması, geçersiz anahtarın silinmesi. Test ayrı doğrulama projesinde geçici QA hesabıyla yapıldı; rapor `remember-login-validation.txt`. Android Java kodu Android 35 SDK'ya karşı derlendi; Android cihazda Keystore/uygulama yeniden açılışı henüz denenmedi.

## Sunucu verileri

`social_v1_` önekli UserInternalData istemci tarafından yazılamaz. Her arkadaş çifti için ID sırasıyla seçilen tek bir oyuncuda ilişki kaydı ve iki tarafta ayrı indeks bulunur. Kabul alıcıya, iptal gönderene aittir; istek ID'si eski işlemlerin yeni isteği değiştirmesini önler. Aynı istek/cevap tekrarında aynı durum korunur. Yerleşik tek yönlü PlayFab listeleri kabulden sonra iki tarafa yazılır; kısmi hatalar sonraki kutu yenilemesinde onarılır. Silinen ilişkinin durum kaydı eski kabulü canlandırmaz.

Davetler gönderenin özel verisinde ve tarafların işaretçilerinde tutulur. Erken iptal kaydı gecikmiş yayımlamayı engeller. Oda adı tahmin edilmesi güç rastgele kimliktir. Photon kimliği custom authentication ile PlayFab ID olmalıdır. Bu sürüm PUN'un mevcut istemci tarafından yönetilen oyun modelini korur; rekabetçi sunucu otoriteli anti-cheat sağlamaz. UserInternalData çoklu kayıt işlemlerinde CAS/transaction sağlamadığından eşzamanlı sunucu yazımları için tam atomiklik iddiası yoktur; seri tekrar/kısmi hata onarımı test edilmiştir.

## Panel kurulumu

1. PlayFab Photon eklentisi mevcut Realtime App ID `YOUR_PHOTON_APP_ID` ile **Installed**. İlk `Chat App ID cannot be null` form hatası, kullanıcı yöntemi açıkça onayladıktan sonra boş sohbet alanının formda yeniden işlenmesiyle giderildi. Kaydetme sonrası panel geçici sunucu hatası gösterdi; yenileme kurulumu doğruladı ve canlı API iki hesaba Photon token üretti. Sohbet uygulaması/sahte ID eklenmedi. Eklenti gizli anahtarı açılmadı veya istemciye yazılmadı.
2. [Photon Dashboard](https://dashboard.photonengine.com/) → Adam Asmaca → Manage → Authentication → Custom Server URL: `https://13C073.playfabapi.com/photon/authenticate` kaydedildi. Anonim bağlantılar kapalı, sağlayıcı ulaşılamazsa istemciler reddedilir. Webhook bu akış için gerekli değildir ve eklenmedi. [Photon resmî entegrasyon belgesi](https://doc.photonengine.com/pun/current/reference/playfab). Panel kanıtları `photon-auth-live.jpg` ve `playfab-photon-installed.jpg`.
3. Kullanıcı 3 Ekim 2026'da SMTP hizmeti olmadığını belirterek e-posta kurtarmanın beklemesini seçti. Gelecekte PlayFab SMTP eklentisi, Account Recovery şablonu ve kurtarma geri çağrı sayfası yapılandırılmalı; `Assets/Resources/OnlineAccountSettings.asset` içindeki `recoveryEmailTemplateId` girilmeli. Boşken arayüz hizmetin kurulmadığını açıkça gösterir. Contact Email kaydetme istemci akışı hazır; gerçek e-posta teslimi henüz denenmedi.

## Doğrulama

- Derleyici: PASS. Mevcut Photon netstandard uyumluluk ve KeyboardUi eski Unity API uyarıları sürüyor.
- `node Backend/PlayFab/social.test.cjs`: PASS; kimlik yetkisi, çevrimdışı kutu, çapraz/tekrar istekler, karşılıklı kabul, ikinci liste yazımı hata onarımı, çıkarma, eski ID, davet kabul/başlat/iptal/zaman aşımı, erken iptal/geç yayımlama, müsaitlik ve sayfalama. Sunucu API mock; paralel canlı istek yarışları bu test kapsamı dışında.
- Unity `AccountSocialValidation.RunBatch`: PASS; 720×1560 ve 1080×1920 arayüz, misafir kapısı, sınır değerleri, pasif davetler, gerçek oyun/menü sahne değişimi, çizimin tetiklenmemesi ve çıkış temizliği. Arayüz veri taşımacısı Editor test yanıtları kullanır. Ekran görüntüleri bu klasörde.
- Canlı hesap testi: kayıt/giriş, aynı kullanıcı adı ve bir yanlış şifre denemesi PASS. İkinci yanlış şifre isteği PlayFab hız sınırına takıldı; başarı sayılmadı.
- Canlı `Social` testi: kullanıcı adıyla ID arama, çevrimdışı istek, alıcı yetkisi, kabul ve yerleşik iki yönlü arkadaş listesi, davet kategori/kabul/başlat/iptal ve arkadaş çıkarma PASS. `live-social-results.txt` raporu. Bu, Photon odası oynanışı testi değildir.
- `PhotonAccountValidation.RunBatch`: iki gerçek Photon Realtime istemcisi, canlı PlayFab token/ID doğrulaması, eu, davetten önce oda oluşturma, gelen davet/kabul/doğrudan katılma, gizli oda/kapasite 2/C=3/iki rezervasyon kimliği, davet başlatma ve ayrılma/temizlik PASS. Şifreler/biletler yalnızca bellekte kaldı. Test ayrı Unity Editor kopyasında iki bağımsız ağ istemcisi kullanır; iki tam oyun arayüzü değildir. Son rapor `photon-account-validation.txt`.
- Gizli arkadaş odası rastgele eşleşmeye alınmadı (`NoRandomMatchFound`), PASS. Son ağ testi rastgele oyuncuları etkilememek için `paper-social-v1.validation` sürümünde yürütüldü; oyun sürümü `paper-social-v1` kalır.
- İki tam oyun arayüzüyle kelime RPC/rövanş, fiziksel telefon dokunması/performansı ve SMTP teslimi henüz doğrulanmadı. E-posta kurulumu kullanıcı isteğiyle ertelendi.
- Ayrı Unity doğrulama kopyasının Editor Search Database indekslemesinde önceden de görülen `ArgumentOutOfRangeException` çıktı; hesap oyun kontrolleri PASS ve oyun çalışma zamanı hatası görülmedi. Ana kullanıcı Unity oturumu kapatılmadı.

QA hesaplarının şifreleri ve biletleri yalnızca doğrulama işlemi belleğinde tutuldu; hiçbir rapora yazılmadı. Canlı test arkadaşlıkları çıkarıldı ve müsaitlik çevrimdışı bırakıldı. QA hesaplarının kendisi silinmedi.
