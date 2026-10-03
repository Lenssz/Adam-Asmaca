# Adam Asmaca proje incelemesi

İnceleme tarihi: 1 Ekim 2026. Bu çalışma kaynak kodu, sahne/prefab verileri, paketler ve proje ayarları üzerinden yapılmıştır. Oyun kodu değiştirilmemiştir. Aşağıda doğrudan dosyalarda saptanan sorunlar ile çalışma zamanında ayrıca doğrulanması gereken riskler ayrılmıştır.

## 1. Projenin amacı ve teknik yapı

Proje, Türkçe ve İngilizce oyun terimlerini kullanan, altı kategorili bir adam asmaca oyunudur. Tek oyunculu modda kelime çözülür; iki oyunculu modda iki kişi aynı kelimeyi birbirinden bağımsız çözmeye çalışır. Multiplayer sıra tabanlı değildir: hedef, rakipten önce kelimeyi tamamlamaktır.

- Unity sürümü: `6000.3.6f1`.
- Ürün: `Adam Asmaca`; şirket alanı: `Lenssz`; sürüm: `1.3`.
- Mevcut üretilmiş C# proje dosyası Android hedeflidir. Android ayarları ARM64 ve IL2CPP içerir; minimum SDK 25'tir.
- Arayüz: Unity UGUI ve TextMesh Pro. Canvas referans çözünürlüğü 1080 × 1920'dir.
- Görselleştirme: URP 17.3.0 ve 2D Renderer; animasyonlarda DOTween kullanılır.
- Girdi: Input System 1.18.0; `activeInputHandler` yeni sisteme ayarlıdır.
- Ağ: Photon PUN 2.52; yerel multiplayer denemeleri için ParrelSync 1.5.2 kuruludur.
- PlayFab SDK kuruludur ancak `Assets/Scripts` içinde PlayFab çağrısı veya oturum açma entegrasyonu yoktur. Mevcut profil ve ekonomi PlayFab'a bağlı değildir.
- AI ipucu: `UnityWebRequest` üzerinden Gemini `gemini-2.5-flash:generateContent` adresine doğrudan istemci isteği.
- Kalıcı kayıt: `PlayerPrefs`. Oyuncu adı, altın ve kategori kelime torbaları cihazda saklanır.

Build listesi sırayla `SampleScene`, `GAMESCENE`, `MultiplayerGameScene` içerir. Normal giriş noktası `SampleScene`dir. `Assets/Settings/Scenes/URP2DSceneTemplate.unity` bir şablondur; build listesinde yoktur.

## 2. Ana menü ve sahne akışı

`SampleScene` içinde `GameManager`, `LobbyManager`, `CoinDisplay` ve kategori panelinde `MenuController` bulunur. Kategori butonlarının gerçek Inspector çağrıları `LobbyManager.OnCategoryButtonClicked` metoduna bağlıdır; `MenuController.ChooseGame` mevcut buton akışının ana giriş noktası değildir.

`LobbyManager.Start` oyuncu adını yükler ve mod seçme panelini açar. İsim `PlayerName` kaydından alınır; kayıt yoksa `Oyuncu_###` oluşturulur. Profil ekranı adı değiştirir ve en fazla 10 karaktere indirir. Bu bir hesap/kimlik doğrulama sistemi değil, yerel takma ad sistemidir.

Tek oyunculu seçimde kategori paneli açılır. Kategori seçilince `GameManager.SelectCategory` indeksi kaydeder ve `GAMESCENE`yi yükler. Çok oyunculu seçimde Photon bağlantısı ve lobiye giriş yapılır. Kategori seçildikten sonra oda özelliği `C` ile aynı kategoride, kapasitesi iki olan oda aranır. Oda bulunmazsa yeni oda oluşturulur. İkinci kişi geldiğinde Master Client `PhotonNetwork.LoadLevel` ile çok oyunculu sahneyi açar.

Kategori eşlemesi iki oyun yöneticisinde ve AI ipucu kodunda ayrı ayrı tanımlıdır:

| İndeks | Kategori | Veritabanı alanı |
| --- | --- | --- |
| 0 | League of Legends | `lolWords` |
| 1 | Valorant | `valorantWords` |
| 2 | Counter-Strike | `csWords` |
| 3 | Genel | `generalWords` |
| 4 | Ülkeler | `countryWords` |
| 5 | Minecraft | `minecraftWords` |

## 3. Tek oyunculu oyun mantığı

`GameManager` bir singleton'dır ve `DontDestroyOnLoad` ile sahneler arasında kalır. Sonradan oluşan ikinci yönetici yok edilir. `sceneLoaded` olayı `GAMESCENE` açıldığında oyunu başlatır.

Kelime seçimi kategoriye ait bir torba üzerinden yapılır. Torba önce RAM sözlüğüne, varsa `CategoryPool_<indeks>` PlayerPrefs kaydından yüklenir. Rastgele seçilen kelime torbadan çıkarılır ve kalan liste virgülle birleştirilerek kaydedilir. Amaç, kategori bitene kadar kelimelerin tekrar gelmesini engellemektir. Kelime Türkçe kültür kurallarıyla büyük harfe çevrilir.

Yeni kelime seçildiğinde tahmin edilen harfler, yanlış sayısı ve sonuç bayrakları sıfırlanır. `OnWordChanged` arayüzü ve adam asmaca çizimini yeniler. Aynı harfin ikinci tahmini işleme alınmaz. Doğru harf kelimedeki bütün konumları açar; yanlış harf sayacı artırır. Bütün karakterler tahmin edilince oyun kazanılır ve 1 altın eklenir. Altı yanlışta oyun kaybedilir.

Yönetici ile görünüm arasındaki bağlantılar:

- `OnWordChanged` → `WordDisplay.BuildWordDisplay`, `KeyboardUI.ResetKeyboard`, `HangmanDrawer.ResetDrawing`.
- `OnLetterGuessed` → harf kutularının açılması, tuş rengi/deaktifleşmesi ve yanlışta vücut parçasının kuyruğa eklenmesi.
- `OnGameEnd` → `GameUI.ShowResult`. Kaybedilince bütün harfler açılır, kamera titrer ve yaklaşık 0,8 saniye sonra sonuç paneli görünür.

Tekrar oyna aynı sahnede `LoadNewWord` çağırır. Menüye dönüş `SampleScene`yi yükler. `GameSceneBootstrap` oyunu başlatan alternatif bir script olarak vardır, fakat incelenen üç sahnede kullanılmamaktadır; mevcut sahne akışında ikinci bir başlangıç çağrısı oluşturmaz.

## 4. Çok oyunculu oyun mantığı

`MultiplayerGameManager` her istemcide kendi tahminlerini ve yanlış sayısını tutar. Master Client oda kategorisinden bir kelime seçer ve `RPC_ReceiveWord` ile herkese gönderir. Tek oyunculu torba sistemi burada kullanılmaz; multiplayer kelimeleri rastgele tekrar gelebilir.

Her oyuncu için kendi harf kutuları ve rakibin ilerleme kutuları üretilir. Oyuncu doğru harf bildiğinde kendi harflerini görür; rakibe yalnızca açılan kutuların indeksleri gönderilir. Rakip kutuları yeşile döner. Bununla birlikte gizli kelimenin kendisi de başlangıçta iki istemciye gönderilmiş olduğundan, protokol kelimeyi istemciden gizlememektedir.

`KeyboardUI` hangi yöneticiye tahmin ileteceğini `PhotonNetwork.InRoom` ile belirler. Oda içindeyse çok oyunculu yöneticiye, değilse `GameManager`a gider. Bu ortak klavye iki modda da kullanılır.

Kelimeyi ilk tamamlayan oyuncu `RPC_GameOver` gönderir. Altı yanlış yapan oyuncunun tahminleri durdurulur ve rakibe asıldığı bildirilir. Tek kişinin asılması maçı hemen bitirmez; rakip oynamaya devam eder. İkisi de asılırsa `-1` kazanan numarasıyla beraberlik ilan edilir. Kazanan 2, kaybeden 1 altın alır; beraberlikte ödül yoktur.

İki oyuncu da tekrar oyna istediğinde Master Client `RPC_StartNewRound` gönderir. Yeni tur kutuları, klavyeyi, yanlış sayısını, ölüm ve rematch bayraklarını sıfırlar. Menüye dönüş odadan ayrılma tamamlandıktan sonra gerçekleşir.

## 5. Fizik ve görsel bileşenler

`HangmanDrawer`, yanlışları bir kuyruğa alır ve 0,4 saniye arayla vücut parçalarını açar. Yeni turda `RagdollAdam` prefabını ipin ucuna oluşturur. Kafadaki `DistanceJoint2D`, sahnedeki ip ucu Rigidbody2D'sine bağlanır.

`RagdollController` altı parçayı başlangıçta görünmez yapar ve colliderlarını kapatır. Yanlış sayısına göre kafa, gövde, iki kol ve iki bacak sırayla açılır; DOTween ile orijinal ölçeğine büyür. Görünmez parçaların Rigidbody2D simülasyonu kapatılmadığından fizik davranışları görünmeden devam eder.

`HangmanPart` fare tıklaması ve Physics2D raycast ile parçaya yatay impuls ve tork uygular. Kod yalnızca `Mouse.current` dinler; dokunmatik girdiyi açıkça işlemez. Android cihazdaki etkileşim ayrıca denenmelidir.

`KeyButton`, `LetterSlot` ve `OpponentLetter` prefabları klavye ve kelime gösteriminde kullanılır. `LolAnim.controller` buton durum animasyonları içerir. Projeye ait sekiz PNG görsel ve render/input ayarları da taranmıştır.

## 6. İpucu ve ekonomi

`EconomyManager` statik bir yardımcıdır. `PlayerCoins` için okuma, ekleme ve yeterli bakiye varsa harcama yapar. Para değişikliği olayı yayınlamaz; `CoinDisplay` yalnızca başlangıçta, yeniden aktifleştiğinde veya açıkça çağrıldığında güncellenir.

`AIHintManager` panel ilk açıldığında mevcut tek oyunculu kelimeyi ve kategoriyi alır. Son istenen kelimeyi bellekte tutarak aynı kelimede ikinci isteği engeller. Prompt en fazla sekiz kelimelik, cevabı içermeyen bir ipucu ister. HTTP 429 durumunda sıradaki API anahtarına geçilir; bütün anahtarlar denenene kadar devam edilir. HTTP başarı durumunda ücret kesilir ve yanıt içinden metin ayıklanır.

Kod varsayılanı 3 altın olsa da `GAMESCENE.unity:1203` içindeki kayıtlı değer **5 altındır**. Mevcut sahnedeki maliyeti Inspector değeri belirler. Multiplayer sahnesinde AIHintManager bileşeni bulunmaz; ipucu kodu da kelimeyi yalnızca `GameManager`dan alır.

## 7. Kelime verisinin gerçek durumu

Her iki yönetici de `WordDataBase.prefab` içindeki bileşene referans verir. C# liste başlangıç değerleri ile prefabın kaydedilmiş listeleri farklıdır. Dolayısıyla kaynak listesine kelime eklemek, mevcut serialized listelerde o kelimenin otomatik olarak kullanılacağı anlamına gelmez.

| Alan | C# başlangıç listesi | Prefabda kayıtlı liste | Açıklama |
| --- | ---: | ---: | --- |
| LoL | 190 | 188 | `MEL`, `ZAHEEN` prefabda yok. |
| Valorant | 62 | 60 | `VETO`, `MIKS` prefabda yok. |
| CS | 31 | 43 | Prefabda kodda bulunmayan 12 rakamlı silah adı var. |
| Genel | 499 | Alan kayıtlı değil | Kaynakta `FIRIN` iki kez var; yorumdaki 500 sayısı doğru değil. |
| Ülkeler | 190 | Alan kayıtlı değil | Kaynakta `MAKEDONYA` iki kez var. |
| Minecraft | 131 | Alan kayıtlı değil | Kaynakta `ŞALTER` iki kez var. |

Son üç alanın başlangıç değerlerinin Unity yüklemesindeki sonucu Play Mode'da ayrıca görülmelidir. Prefabın `m_EditorClassIdentifier` alanında eski `LolWordDataBase` adı durur; gerçek C# sınıfı `WordDatabase`dir. Bu bir tutarlılık/bakım noktasıdır; tek başına kesin bir derleme veya missing-script hatası olarak değerlendirilmemiştir. Unity 6.3, bazı koşullarda dosya ve sınıf isimleri farklı olsa da sınıfı çözebilir: [Unity script adlandırma belgesi](https://docs.unity.com/en-us/engine/6000.3/manual/scripting/get-started/naming-scripts).

## 8. Öncelikli bulgular

### A. Dosyalardan doğrudan saptanan sorunlar

1. **CS kategorisinde çözülemeyen kelimeler var.** `WordDataBase.prefab:297` listesinde `AK47`, `M4A4`, `P250`, `MP5`, `MP9`, `MAC10`, `UMP45`, `XM1014`, `MAG7`, `SG553`, `SCAR20`, `M249` bulunur. `KeyboardUi.cs:30` klavyesi rakam içermiyor. Kazanma koşulu bütün karakterlerin tahminini istediği için bu kelimeler mevcut arayüzle tamamlanamaz. Bu durum iki modu da etkiler.

2. **Çok oyunculu sahne fizik sistemine geçirilmemiş.** `MultiplayerGameScene.unity:1780` civarındaki HangmanDrawer bileşeni eski `drawColor`, `textureWidth`, `drawSpeed` alanlarını taşıyor; güncel kodun istediği `ragdollPrefab` ve `ropeTipAnchor` alanları yok. `ResetDrawing` bunlar olmadan ragdoll oluşturamaz. Tek oyunculu sahnede iki referans da vardır.

3. **Kuyruktan çıkış arayüzü kendi panelini tekrar kapatıyor.** `LobbyManager.cs:184` içindeki `LeaveQueue`, kategori panelini açıyor. Ancak `SampleScene.unity:1440` ile başlayan aynı butonun ikinci Inspector listener'ı `CategorySelectionPanel.SetActive(false)` çağırıyor (`:1452`). Bu ikinci çağrı, metodun açtığı paneli yeniden kapatır.

4. **Kelime torbası yeniden dolduğunda RAM kaydı güncellenmiyor.** `GameManager.cs:123`, boş torba için yalnızca yerel `currentPool` değişkenine yeni liste atıyor; `categoryPools[selectedCategoryIndex]` boş eski listeyi tutmaya devam ediyor. İlk torba tükendikten sonra aynı oturumdaki sonraki her seçim tam listeden başlar; tekrarsız seçim davranışı bozulur.

5. **API anahtarları hem kaynakta hem sahnede açık olarak bulunuyor.** `AIHintManager.cs:15` ve `GAMESCENE` içindeki serialized bileşen anahtarlar içeriyor. Değerler bu rapora kopyalanmamıştır. Paylaşılan repo/build'den çıkarılabilirler. Mevcut anahtarların iptali/yenilenmesi ve isteğin sunucu üzerinden yapılması bu somut bulgunun çözümüdür.

6. **Oyun sürerken rakibin ayrılması ele alınmıyor.** `MultiplayerGameManager.cs:255` yalnızca `isGameOver` zaten true ise arayüzü güncelliyor ve odadan ayrılıyor. Devam eden maçta rakip ayrılınca bir sonuç/menü davranışı uygulanmıyor. Ağ kopması için ayrıca `OnDisconnected` işleyicisi de yok.

### B. Kodun izin verdiği, çalışma zamanında doğrulanması gereken riskler

7. **Aynı tur birden fazla sonuç ve ödül alabilir.** `RPC_GameOver` başında önceki bitişi reddeden bir kontrol veya tur kimliği yoktur. İki oyuncu yakın zamanda kelimeyi tamamladığında ikisi de `RpcTarget.All` ile kendisini kazanan ilan edebilir. İkinci RPC sonucu tekrar işler ve tekrar altın ekler. Sonuç merkezi olarak bir kez kararlaştırılmalı, ödül de tur başına bir kez verilmelidir. Photon, `All` çağrısının gönderen istemcide hemen çalıştığını ve tüm istemcilerde ortak sıralama için `AllViaServer` davranışını açıklar: [Photon RPC sıralama belgesi](https://doc.photonengine.com/pun/current/gameplay/rpcsandraiseevent). Yalnızca target değiştirmek, ödülün bir kez verilmesi ve gönderici doğrulaması ihtiyacını tamamen çözmez.

8. **Tur hazır olmadan tahmin yapılabilir.** Yeni tur klavyeyi hemen açar; `secretWord` sıfırlanmaz ve kelime teslimi için hazır bayrağı yoktur. İlk turda kelime ulaşmadan tıklama null erişimine, rematch'te eski kelime ve boş kutu listesiyle işlem yapılmasına yol açabilir. Master'ın başlangıç RPC'sinden önce bütün istemcilerin hazır olduğunu doğrulayan bir protokol de yoktur. Scene sync vardır, fakat ayrıca bir tur hazır onayı bulunmaz; bu nedenle ilk kelimenin zamanlaması canlı ağ testinde incelenmelidir.

9. **Kuyruk iptali ağ işlemlerini geçersiz kılmıyor.** `LeaveQueue` yalnızca zaten odadaysa ayrılma gönderir. Oda arama/oluşturma isteği sürerken iptal edilirse sonradan gelen `OnJoinRandomFailed` oda açabilir veya `OnJoinedRoom` maça geçebilir. Bir kuyruk durumu/istek kimliği yoktur. `OnCreateRoomFailed`, `OnJoinRoomFailed`, bağlantı kesilmesi ve lobiye giriş hataları için arayüz toparlama kodları da yoktur.

10. **AI yanıtı yanlış tura veya başarısız ipucuna ücret kesebilir.** HTTP 200 alındığında metin doğrulanmadan `SpendCoin` çağrılır ve dönüş değeri yok sayılır. Yanıtı olmayan/bozuk içerik de ücretlenebilir. İstek sırasında yeni kelimeye geçilirse eski yanıtın yeni tura ait olup olmadığı denetlenmez. `ExtractTextFromJson` gerçek JSON parse etmek yerine `"text": "` alt dizisini arar; farklı boşluk biçimi ve escaped tırnaklar için güvenilir değildir. Timeout ve iptal yönetimi de yoktur.

11. **Ağ çağrılarında gönderen ve veri doğrulaması yoktur.** RPC'ler `PhotonMessageInfo` üzerinden göndereni kontrol etmez; herhangi bir istemci sonuç, yeni tur veya kelime RPC'sini çağırabilir. Rakip kutu indeksleri sınır kontrolü olmadan kullanılır. Ekonomi de yalnızca PlayerPrefs olduğundan çevrimiçi rekabette güvenilir ödül kaydı sağlamaz.

12. **Başlangıç/animasyon yaşam döngüsünde tekrar ve geç callback riski var.** `WordDisplay`, hem yeni kelime olayında hem `Start` içinde kutuları oluşturur. Normal sahne yüklemesinde bu iki yol aynı kelime için çalışabilir. Eski kutulara ait `DOVirtual.DelayedCall` callback'leri iptal edilmiyor. Benzer durum sonuç paneli ve multiplayer kutu animasyonlarında da var; hızlı sahne/tur değişimleriyle denenmelidir. `GameUI` ve `WordDisplay.OnEnable` singleton'ın varlığını varsayar; oyun sahnesini doğrudan açmak normal menü akışından daha kırılgandır.

Ek bakım noktaları: kategori tanımları üç yerde tekrarlanır; public `Action` alanları dışarıdan değiştirilebilir; ekonomi negatif miktarları doğrulamaz; isim kontrolü yalnızca boş/null kontrolüdür ve yalnızca boşluk içeren adlara izin verir; DOTween ve UI ile oyun kuralı aynı multiplayer sınıfında iç içedir. Bunlar öncelikli oynanış sorunlarından sonra ele alınabilir.

## 9. Yapılan doğrulamalar ve sınırlar

- Projeye ait 16 C# scriptinin tamamı okundu; toplam 1.706 satır.
- Üç build sahnesindeki özel script bileşenleri, Inspector alanları ve kalıcı buton çağrıları incelendi. Beş oyun prefabı ve altı kelime listesi karşılaştırıldı.
- Üç oyun sahnesi ve beş prefabın asset GUID'leri `Assets` ve mevcut `Library/PackageCache` ile karşılaştırıldı: çözülemeyen asset GUID'i bulunmadı. Aynı dosyalardaki doğrudan yerel `fileID` referanslarında eksik hedef saptanmadı. Bu kontrol semantik alan eksikliği veya görsel doğruluk garantisi değildir.
- Kurulu SDK'ların ilgili ağ kodu, proje paket sürümleri, render/input/build ayarları ve Git durumu incelendi. Üçüncü taraf SDK'ların bütün kaynakları satır satır denetlenmedi; proje entegrasyonları esas alındı.
- MSBuild ile kontrol denemesi yerel Microsoft SDK dizinine erişim kısıtı nedeniyle ilerleyemedi. Ardından 16 proje scripti Roslyn ile mevcut Unity/Photon/DOTween derleme referansları kullanılarak ayrı bir DLL'e derlendi: **0 hata, 2 uyarı**. Uyarılar eski `FindObjectOfType` kullanımı ve Photon'un netstandard 2.0 referansının 2.1 ile eşleştirilmesidir. Geçici çıktılar Git tarafından yok sayılan `.utmp/analysis` dizinindedir.
- Bu, Unity Player/Android build veya Play Mode testi değildir. Canlı Photon maçı, gerçek Gemini yanıtı, dokunmatik girdi ve görsel yerleşim çalıştırılarak doğrulanmadı. API anahtarları kullanılarak dış istek gönderilmedi.
- İnceleme başlangıcında `Assets/PlayFabEditorExtensions/Editor/Resources/PlayFabEditorPrefsSO.asset` zaten değişmişti; bu dosyaya dokunulmadı. Yeni kalıcı çıktı yalnızca bu inceleme raporudur.

## 10. Önerilen düzeltme sırası

1. Serialized kelime verisini kaynakla tutarlı hale getirip rakamlı kelimelerin oynanabilirliğini düzeltmek.
2. Multiplayer sahnesini fizik prefabı ve ip ucu referanslarıyla tamamlamak.
3. Kuyruktan çıkış listener'ını ve kuyruk iptal/bağlantı hata durumlarını düzeltmek.
4. Kelime torbasının RAM sözlüğünü yenilemek; eski PlayerPrefs havuzları için veri sürümü düşünmek.
5. API anahtarlarını yenilemek; ipucunu sunucuya taşımak ve yanıt/ücret/tur eşlemesini doğrulamak.
6. Multiplayer'da tur kimliği, hazır durumu, tek sonuç kararı, tek ödül ve oyuncu ayrılma davranışı eklemek.
7. ParrelSync ile eşzamanlı son harf, çift asılma, rematch, kuyruk iptali ve bağlantı kopması senaryolarını denemek; Android üzerinde fizik/dokunmatik ve uzun kelime yerleşimini kontrol etmek.
