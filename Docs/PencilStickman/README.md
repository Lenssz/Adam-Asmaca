# Karakalem çubuk adam

Referanstaki yuvarlak, gülümseyen yüz ve elde çizilmiş çizgi hissi kullanıldı. Karakter için şeffaf bir atlas, ardından kafa, gövde, sol/sağ kol ve sol/sağ bacak olmak üzere altı ayrı sprite üretildi. Görseller built-in imagegen ile üretildi; tam istem `generation-prompt.txt` dosyasında. Unity kesme işlemi atlasın RGBA piksellerini değiştirmez.

Yeni model `Assets/Prefabs/PencilRagdoll.prefab`, görseller `Assets/Sprites/PencilStickman/` altında. Eski `RagdollAdam.prefab` ve Antigravity'nin `Stickman` görselleri korunmuştur. Fizik kodu yeni parçaların düzenine uyarlandı. Bu aşama karakter, onu taşıyan çizgiler ve karakterin arkasındaki küçük kağıt alanını kapsar; bütün menülerin kağıt temasına geçişi ayrı aşamadır.

## Deneme

Unity'de **Tools → Pencil Stickman → 3 Open workshop** menüsünü açıp Play'e bas. **Sırayla çiz** altı parçayı açar. **Parça ekle** tek parça ekler, **Sıfırla** yeni bir model oluşturur. Klavye alternatifleri: A, Space ve R. Görünen parçaları fareyle tutup sürükleyebilirsin. Mobil için primary touch girişi de bulunmaktadır; fiziksel Android cihazında doğrulanmamıştır.

Atölye `Assets/Scenes/PencilRagdollPreview.unity` dosyasıdır; kelime seçimi, para veya Photon maç akışını başlatmaz. Build sahne listesine eklenmemiştir. Workshop kamerası portre görünümüne ayarlıdır.

Çizim efektinin gerçek Play Mode karelerinden hazırlanmış önizlemesi: [drawing-preview.gif](drawing-preview.gif).

## Fizik düzeni

- Altı ayrı Rigidbody2D, altı collider ve HingeJoint2D kullanılır. Fizik kökleri birim ölçektedir; çizim boyutu alt nesnedeki SpriteRenderer üzerinde ayarlanır.
- Kafa ip ucuna, gövde kafaya, kollar ve bacaklar gövdeye bağlanır. Parçaların uçları çakışabilir; kendi aralarındaki çarpışmalar kapalıdır.
- Gizli parçaların rigidbody simülasyonu, collider'ı ve eklemi kapalıdır. Görünür hale gelen parça hareket eden ebeveyninin mevcut eklem konumuna yerleştirilir ve hızını devralır.
- Parçalar kalem ucuyla çizilir: kafa 0,9 saniye, gövde ve uzuvlar 0,5 saniye. Kafada önce çember, sonra gözler, gülümseme ve saç çizgileri açılır. Collider veya rigidbody ölçeği değiştirilmez.
- Çizilirken yeni parçanın fizik simülasyonu ve collider'ı kapalıdır; çizim hareket eden ebeveyni takip eder. Tamamlandığında ekleme bağlanır, ebeveynin hareketini devralır ve tutulabilir.
- Kalem yalnızca çizgi çekerken kuru grafit sürtünme sesi çıkarır. Prefabın PencilDrawEffect bileşenindeki **Sound Volume** varsayılan olarak 0,22'dir. Kalem çizgiler arasında yumuşakça kalkar; çizim sonunda veya sıfırlanınca kalem ve ses durur.
- Kol ve bacak açıları dinlenirken okunur bir siluet bırakır. Sürükleme TargetJoint2D yayıyla uygulanır; hedef uzaklığı ve bırakma hızı sınırlıdır. Odak kaybı ve devre dışı bırakma sürüklemeyi sonlandırır.
- Aynı yanlış sayısının tekrar gelmesi tekrar parça eklemez. Aradan sayılar atlandığında eksik parçalar sıraya alınır. Sıfırlama eski modeli hemen etkisizleştirir, animasyon kuyruğunu ve kamera titreşimini temizler.

## Doğrulama

**Tools → Pencil Stickman → 1 Build and validate** atlası keser, prefabı ve üretilen atölye sahnesini yeniler. Atölyede kaydedilmemiş değişiklik varsa yenileme durur. Bu komut üretilen atölye içeriğini baştan oluşturur.

Kontrol, oyunun açık sahnesinden bağımsız Unity 2D preview fizik sahnesinde çalışır. Gizli rigidbody/collider durumları, 1–6 açılışı, tekrarlı ve sınır dışı çağrılar, hareket eden ebeveyne hizalama, ölçekler, bir saniyelik sınırlı kol sürüklemesi ve ardından 12 saniyelik sönümlenme ölçülür. Kolların ve bacakların doğru tarafta, gövdeden ayrık durduğu kontrol edilir. Son ölçüm `physics-results.txt`, birleşmiş görüntü `assembled.png` dosyalarındadır. Ayrıntılı geçici çıktılar `.utmp/pencil-validation/` altında tutulur.

Unity script derlemesi başarılıdır. Bağımsız Roslyn kontrolünde mevcut Photon netstandard sürüm eşleştirme ve KeyboardUI'daki eski FindObjectOfType kullanım uyarıları dışında hata yoktur. Workshop Play modunda altı parçanın görünmesi kontrol edilmiştir. Android build ve iki cihazlı Photon maçı bu çalışmada çalıştırılmamıştır.

**Tools → Pencil Stickman → 4 Validate pencil drawing** altı parçanın %0/%25/%50/%75/%100 görüntülerini, kalem ucunun yol ile örtüşmesini, çizim sırasında kapalı fiziği, hareketli gövdeyi takip etmeyi ve iptal sonrası animasyonsuz açılışı kontrol eder. Tamamlanan çizimin önceki URP sprite görünümünden RGB farkı **0/255**, çizim sırasındaki en büyük eklem aralığı **0** ölçülmüştür. Sonuçlar [drawing-results.txt](drawing-results.txt) dosyasındadır.

Geçici proje kopyasında gerçek Play Mode kontrolleri de çalıştırıldı: kafa ve uzuv çizilirken sıfırlama, sesin başlaması/kesilmesi, hızlı 1–6 kuyruğu, tekrar eden bildirimler, son çizimden önce sarsıntı olmaması, kamera konumunun geri yüklenmesi, tek AudioListener ve sonuç panelinin çizimi beklemesi. Eski bir sonucun sıfırlama sonrasında görünmediği doğrulandı. Sonuçlar [drawing-play-results.txt](drawing-play-results.txt) dosyasındadır. Sesin AudioSource üzerinden çalışması ve WAV sinyali doğrulandı; fiziksel hoparlörden dinleme bu ortamda yapılmadı.

## Çizim varlıkları ve kod

- Mevcut altı karakter sprite'ının RGBA içeriği korunur. Her parçanın `*-order.png` maskesi çizilme zamanını 16 bit olarak R/G kanallarında taşır; doğrusal, sıkıştırmasız ve point filtreli içe aktarılır. `*-strokes.asset` çizgi yollarını, kalem kaldırma aralarını ve süreyi tutar.
- `PencilStroke.shader` maskeyi ilerlemeye göre açar. `PencilDrawEffect`, MaterialPropertyBlock ile ilerlemeyi, kalem ucunu ve 15 ms ses giriş/çıkışını yönetir; çalışma sırasında texture pikselleri yeniden yazılmaz.
- `RagdollController.DrawPart` tamamlanana kadar beklenebilir. `RevealPart(count, false)` anında ve sessiz açar. `HangmanDrawer.WaitForDrawing` çizim kuyruğunu bekler; `DrawingRevision` sıfırlama sonrası eski sonuçların görünmesini önler. Parçalar arasındaki bekleme 0,08 saniyedir. Skor ve Photon olayları çizim süresini beklemez.
- [Kalem görseli](../../Assets/Sprites/PencilStickman/drawing-pencil.png) built-in imagegen ile şeffaf üretildi; tam istem [drawing-pencil-prompt.txt](drawing-pencil-prompt.txt) dosyasındadır. [Kalem sesi](../../Assets/Sprites/PencilStickman/pencil-scratch.wav) deterministik, filtrelenmiş gürültüden yerel olarak üretilmiş 44,1 kHz mono PCM WAV'dır; harici ses kaydı kullanılmadı.

## Oyun sahneleri

**Tools → Pencil Stickman → 2 Apply to game scenes** hem `GAMESCENE` hem `MultiplayerGameScene` içindeki HangmanDrawer referanslarını yeni prefaba bağlar. Çok oyunculu sahnenin eksik ip ucu tamamlanmıştır. Yeni çizimi okunur tutan krem kağıt alanı ve ince askı çizgileri eklenmiştir. Eski askı objesi silinmeden kapatılmıştır. Menü, klavye, kelime ve para sistemi bu aşamanın görsel kapsamı dışında kalır.

Sahne uygulama komutu kaydedilmemiş oyun sahnesini değiştirmez. Aynı komut tekrar çalıştırıldığında ikinci ip ucu veya ikinci kağıt alanı oluşturmaz. Mevcut SP event akışı ve MP doğrudan ResetDrawing/AddPartToQueue çağrıları korunmuştur.
