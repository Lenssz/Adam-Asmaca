# Kâğıt teması, ekran sınırları ve gerçek kalem kaydı

`GAMESCENE`, `MultiplayerGameScene` ve çizim atölyesi aynı hafif kırışık defter kâğıdını ve grafit darağacını kullanır. Karakter ve darağacı sol üsttedir. İlk oyun teması aşamasında menü/lobi kapsam dışındaydı; sonraki kullanıcı isteğiyle ana menü ve aynı sahnedeki kategori/profil panelleri de eşleştirildi (bkz. `../PaperMenu/README.md`). Oyunlara özel `Assets/Prefabs/PaperTheme` kopyaları kullanılır; ortak eski UI prefabları korunur.

## Çalışma ve ayarlar

- `PaperScreenBounds`: güvenli alanın dört kenarında statik 2D duvarlar. Malzeme sürtünmesi 0,08; sekme 0,10. Ekran/aspect/güvenli alan değişikliklerinde güncellenir. Kamera sarsıntısı referans merkezi değiştirmez. Duvarlar Ignore Raycast katmanındadır.
- `PaperScenePresentation`: arka planı tam ekran yapar, darağacının ölçülmüş ip ucunu `RopeTip` ile eşleştirir. Görüntü oranı değişince mevcut gövdeleri ve bağlantı noktasını aynı miktarda taşır; yanlış sayısını sıfırlamaz.
- `PaperSafeArea`: oyun UI elemanlarını cihazın güvenli alanında tutar. `PaperWordLayout` uzun kelimelerde kutu genişliğini ve yazı boyutunu ayarlar.
- İki bacağa kendi rigidbody'leri üzerinde ayak collider'ı eklendi. Parçanın bütün collider'ları çizim bitince açılır. Ek collider'lar üzerinden aynı rigidbody tutulabilir; karakterin bütün collider çiftleri arasında çarpışma engeli korunur.
- `PencilDrawEffect.writingVariations` üç gerçek kalem kaydını kullanır. `soundVolume` Inspector'da değiştirilebilir; başlangıç 0,22. Perde çizgi boyunca sabittir; ayrı çizgilerde 0,985/1/1,015 gibi küçük farklar vardır. Kalem kalktığında ses düzeyi sıfırdır; bitiş/sıfırlama/devre dışı bırakmada AudioSource durur.
- Baş çizimi 0,9 s; diğer parçalar 0,5 s; parça arası 0,08 s olarak kaldı. Tamamlanmış parçalar hareket ederken yeni çizim ebeveyni takip eder ve tamamlanınca onun bağlantı konumunu ve noktasal hızını devralır. Tek oyunculu sonuç paneli çizimi bekler; skor ve Photon olaylarının zamanlaması değişmedi.

## Kaynak ve lisans

- Patrick Hand / Patrick Wagesreiter: [Google Fonts METADATA](https://raw.githubusercontent.com/google/fonts/main/ofl/patrickhand/METADATA.pb), [TTF](https://raw.githubusercontent.com/google/fonts/main/ofl/patrickhand/PatrickHand-Regular.ttf), [SIL OFL 1.1](https://raw.githubusercontent.com/google/fonts/main/ofl/patrickhand/OFL.txt). Lisans `Assets/Fonts/Paper/OFL.txt` dosyasında korunur. Statik 2048 atlas ASCII ve ÇçĞğİıÖöŞşÜü karakterlerini içerir. Kullanılmayan ₺ işareti kaynak fontta bulunmaz ve atlasa eklenmedi.
- [OpenGameArt Pencil Sounds](https://opengameart.org/content/pencil-sounds), CC0. Kaynak ZIP: https://opengameart.org/sites/default/files/pencil.zip . Yalnızca NachtmahrTV'nin [yazma kaydı](https://freesound.org/s/571800/) kullanılır. Silgi/vurma sesleri kullanılmadı. Paket README'si `Assets/Audio/Pencil/SOURCE-LICENSE.txt`; değiştirilmemiş yazma OGG'si `pencil-write-source.ogg` içinde korunur.
- `NaturalPencilAudio` gerçek kayıttan üç ayrı güçlü yazma penceresini seçer, mono yapar, 160 Hz altını/9 kHz üstünü azaltır, RMS seviyesini eşler ve 20 ms uç yumuşatması uygular. PCM içe aktarma döngü uçlarının sıfır kalmasını sağlar. Varlık üreticisinden yapay gürültü üretimi kaldırıldı; yeniden üretme gerçek kayıtları kullanır. İşlem ayrıntıları `Assets/Audio/Pencil/PROCESSING.md`.
- Kâğıt ve şeffaf darağacı imagegen ile üretildi; özgün çıktılar korunarak projeye kopyalandı. İstemler `IMAGE_PROMPTS.json`. UI çerçeveleri deterministik Unity nine-slice şekilleridir. Önceki altı karakter PNG'sinin piksel verisi korunmuştur.

## Doğrulama ve önizleme

Unity 6000.3.6f1 üzerinde ayrı test projesinde `PencilDrawingValidation.RunBatch` çalıştırılır. Başlıca raporlar bu klasöre kopyalanır:

- `physics-results.txt`: dört kenar, ayak collider'ı, sürükleme hedefi, collider açma/kapama, ekran oranı/güvenli alan, kamera sarsıntısı, kenara yakın çizim, Türkçe atlas ve kayıt uçları.
- `game-results.txt`: iki gerçek oyun sahnesi, 32 tuş, doğru/yanlış renkleri, çizim/sıfırlama, sonuç panelleri ve uzun kelime.
- Mevcut çizim ve fizik regresyon raporları: `drawing-results.txt`, `drawing-play-results.txt`, `ragdoll-physics-results.txt`.
- Görseller: `GAMESCENE.png`, `MultiplayerGameScene.png`, iki sonuç paneli ve `GAMESCENE-long-word.png`.
- `pencil-audio-preview.wav`: üç kayıt varyasyonunun %22 seviyesinde, aralarda kısa sessizlikle dinlenebilir önizlemesi.

Sesin gerçek kayıttan geldiği, kayıtlarda sinyal bulunduğu, uç örneklerin sıfır olduğu, perde sabitliği, kalem kalkması ve sıfırlamada susturma otomatik olarak kontrol edildi. Hoparlör/kulaklık üzerinden işitsel kalite değerlendirmesi bu ortamda yapılmadı; önizleme kullanıcı dinlemesi içindir. Fiziksel Android/iOS cihaz, yön değiştirme dokunma davranışı, çentikli cihaz ve canlı iki cihazlı Photon oturumu ayrıca denenmelidir. Yerel çok oyunculu UI kontrolü ağ olayları göndermeden yapılır.

Yeniden üretme: Tools → Pencil Stickman → Build and validate (F8), ardından Apply to game scenes (F9). Atölye F10 ile açılır. Sahne düzenlemeleri önce kaydedilmelidir.
