# Kâğıt menüsü ve sayfa etkileşimleri

Ana menü (`SampleScene`), kategori, profil ve iki oyun sahnesi aynı defter kâğıdı ve karakalem dilini kullanır. Şeffaf menü çerçeveleri, altı karakalem kategori logosu, Patrick Hand ve özgün karakter sprite'ları korunur. Kategori indeksleri, isim kuralları, ekonomi ve mevcut public buton metotları korunur.

## Açılış

`PaperMenuIntro` uygulama oturumunun ilk açılışında yaklaşık **4,5 saniye** oynar. Her ana tuşun çerçevesi **0,55**, yazısı **0,35 saniye** sürer. Öğeler kendi `seconds` sürelerini kullanır; aralarda kısa bir kalem kaldırma hareketi vardır. Karakter mevcut UV çizgi yollarını kullanır. Çerçeveler dört kenarları boyunca açılır; metinler soldan sağa kırpılır. Metinler için gerçek harf konturu yolları oluşturulmamıştır. Kalem ucu ilerleyen çizgiyle eşleşir; doğal kalem sesi temas aralarında susar.

`Menu safe area` üzerinde `drawingOrder` içindeki `seconds`, `liftDuration` ve `soundVolume` Inspector'dan ayarlanabilir. `duration` bu sürelerden hesaplanır. Menüye dönüşte uzun açılış tekrar etmez. Play Mode bağlam menüsündeki `Replay opening drawing` yeniden izletir. Duraklatma, bileşen kapatma veya ekran boyutu değişimi kalemi, sesi ve maskeleri temizler; tamamlanmış menü görünümü geri gelir.

## Parmağın altında kâğıt

`PaperPressFeedback` menü, kategori ve iki oyun sahnesindeki bütün tuşlara eklenir; sonradan oluşturulan harf tuşlarına prefab üzerinden gelir. Temasın gerçek konumunda küçük gölge ve ince çizgi kırışıklıkları oluşur. Basma **0,08 saniyede** yerleşir, tutulurken kalır, bırakılınca **0,18 saniyede** kaybolur. Tuş içeriği **%1,5** sıkışır. Görsel native UI mesh ile çizilir. Parmağı dışarı sürüklemek basma görünümünü ve tıklama uygunluğunu iptal eder; pasif tuşlar tepki vermez.

Inspector: `pressDuration`, `releaseDuration`, `compression`, `creaseStrength`. Harf klavyesindeki büyük zıplama kaldırılmıştır; doğru/yanlış renkleri korunur. Harf basmak sayfa geçişini başlatmaz.

## Silgili geçiş

Üç sahnedeki `Paper interaction service` üzerinde `PaperPageTransition` bulunur. İlk servis sahneler arasında yaşar; sonraki kopyalar kaldırılır. Eski sayfanın kamera görüntüsü başlık ve altınla birlikte yakalanır. Kâğıt korunarak çizimler **0,45 saniyede** düzensiz silgi sınırı ve görünen küçük karakalem silgiyle temizlenir. Kategori/profil gibi panel girişleri **0,7**, oyun UI girişi **0,35 saniyede** birlikte ortaya çıkar. Geçiş karakterin yeni bir fizik parçasını çizmesini tetiklemez. İlk kelime kutuları ayrıca zıplamaz; sonraki kelime animasyonları korunur.

Tek oyunculu sahne silinme sırasında asenkron yüklenir; kâğıt örtüsü sahne hazır olana kadar kalır. Yükleme uzarsa temiz kâğıtta “Hazırlanıyor…” görünür. Çok oyunculu bağlantı, oda ve sahne yükleme çağrıları hemen gönderilir; görsel geçiş ağ işlemlerini bekletmez. Güncel ağ paneli bildirimi bekleyen eski panel hedefini değiştirebilir.

`IsTransitioning` tekrar tıklamaları engeller. Raycast örtüsü, CanvasGroup giriş kilidi ve karakter sürükleme kontrolü geçiş boyunca çalışır. Duraklatma, boyut değişimi veya iptal geçerli hedefi tamamlayarak geçici materyalleri ve kilitleri kaldırır. Unity'nin başlamış asenkron yüklemesi iptal edilemediği için yükleme güvenle tamamlanır. Bağlantı kesilirse ağ beklemesi temizlenir. Süreler zaman ölçeğinden bağımsızdır.

Inspector: `eraseDuration`, `categoryEntryDuration`, `gameEntryDuration`. Görüntü dokusu aynı boyutta yeniden kullanılır; uzun kenarı en fazla 1560 pikseldir. Kırışıklık ve silgi ölçeklenebilir UI koordinatları kullanır.

## Yeniden oluşturma ve kaynaklar

Tools → Pencil Stickman → **6 Apply paper interactions** üç sahneye ve `PaperKeyButton.prefab` varlığına bileşenleri bağlar. Önce sahne düzenlemelerini kaydedin. `PaperMenuBuilder` ve `PaperThemeBuilder` yeniden oluştururken etkileşimleri de korur. Mevcut fizik, karakter, doğal çizim sesi ve oyun çerçeveleri korunur.

Çalışma shader'ları `PaperErase.shader`, `MenuOrderedStroke.shader`; native kırışıklık/silgi için ek bitmap varlık yoktur. Önceki logoların üretim istemleri [logo-prompts.json](logo-prompts.json), görsel/font/ses lisansları [PaperTheme notları](../PaperTheme/README.md) içinde korunur.

## Doğrulama ve önizleme

Unity 6000.3.6f1 ayrı test kopyasında `PaperMenuValidation.RunBatch` ve `PaperInteractionValidation` ile kontrol edildi:

- 4,5 saniyelik yapılandırma, tuş başına 0,55 + 0,35 süre, gerçek çizim render'ları; iptal sonrası piksel düzeyinde aynı tamamlanmış görünüm, kalem/maskelerin temizlenmesi ve oturumda yeniden başlamama.
- Merkez ve kenarda temas, basılı tutma, bırakma, dışarı sürükleme iptali, pasif tuşlar; kırışıklık ve silginin gerçekten render edilen UI mesh'leri.
- Ana menü → kategori → oyun → menü, profil kaydetme ve geri, çok oyunculu sahneye harici yükleme → menü; tekrar tıklama tek işlem, duraklatma/iptal ve viewport boyutu değişiminde kilitlerin temizlenmesi.
- Gerçek asenkron yükleme etkinleştirmesi test kapısıyla bekletilerek temiz kâğıt ve yükleme yazısı; ağ yükleme çağrısının animasyondan önce gönderilmesi, simüle edilen güncel oda/panel bildirimi.
- İki oyun klavyesinde ortak geri bildirim; harf basışında renk/pasiflik, büyük zıplama veya sayfa geçişi olmaması; sayfa girişinde yanlış cevap/parça çizimi olmaması.
- 720×1560 ve 1080×1920 render'ları; font, şeffaf çerçeve, kare kategori alanları, altı logo ve tıklama bağlantılarının korunması.

Test kopyası ayrı şirket/ürün adıyla PlayerPrefs kullanır; test isim kaydı asıl oyuncu verisini değiştirmez. Gerçek iki istemcili Photon oturumu, fiziksel telefonda dokunma, yazılım klavyesi ve GPU performansı doğrulanmadı. Çok oyunculu testler yerel sahne yükleme ve simüle edilmiş olaylarla sınırlıdır.

Gerçek Unity karelerinden hazırlanmış sessiz GIF'ler: [açılış](menu-opening.gif), [sayfa geçişi ve bekletilmiş yükleme](paper-navigation.gif), [9:16 sayfa geçişi](paper-navigation-9x16.gif). GIF'ler önizleme için döngüye girer. Basma görünümü [press-held.png](press-held.png), yavaş yükleme [slow-load-paper.png](slow-load-paper.png). Kontrol raporları [results.txt](results.txt), [interaction-results.txt](interaction-results.txt); önceki olay bağlantıları [bindings.txt](bindings.txt).

