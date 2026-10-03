# Uygulama ikonu

`Assets/Sprites/PaperTheme/app-icon-pencil.png`: 1254×1254, opak RGB PNG. Oyunun defter kâğıdı ve karakalem temasını taşıyan, elinde kalem tutan gülümseyen çubuk adam. Yazısız, tam kare görsel; dış köşeleri platform kendi maskesiyle biçimlendirir.

Built-in image_gen ile üretildi; tam üretim istemi ve kaynak yolu [app-icon-prompt.json](app-icon-prompt.json) içinde kayıtlıdır. Kaynak kopyası korunmuştur. Unity'de varsayılan uygulama ikonu olarak atanmıştır; ayrı Android adaptive katmanları bu çalışmada hazırlanmadı.

Tools → Pencil Stickman → **7 Apply paper app icon** içe aktarma ve varsayılan ikon atamasını tekrar uygular. Texture2D, mipmap kapalı, sıkıştırmasız, Clamp olarak içe alınır. Diğer platform/sahne ayarları korunur.

Unity 6000.3.6f1 ayrı test projesinde kare kaynak ve PlayerSettings ikon ataması doğrulandı: [app-icon-results.txt](app-icon-results.txt). Gerçek telefon kurulumu ve mağaza paketi oluşturma testi yapılmadı.
