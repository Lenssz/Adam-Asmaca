using System.Collections.Generic;
using UnityEngine;

public class WordDatabase : MonoBehaviour
{
    public List<string> lolWords = new List<string>()
    {
        
        "AATROX", "AHRI", "AKALI", "AKSHAN", "ALISTAR", "AMBESSA", "AMUMU", "ANIVIA", "ANNIE", "APHELIOS", "ASHE", "AURELIONSOL", "AURORA", "AZIR",
        "BARD", "BELVETH", "BLITZCRANK", "BRAND", "BRAUM", "BRIAR",
        "CAITLYN", "CAMILLE", "CASSIOPEIA", "CHOGATH", "CORKI",
        "DARIUS", "DIANA", "DRAVEN", "DRMUNDO",
        "EKKO", "ELISE", "EVELYNN", "EZREAL",
        "FIDDLESTICKS", "FIORA", "FIZZ",
        "GALIO", "GANGPLANK", "GAREN", "GNAR", "GRAGAS", "GRAVES", "GWEN",
        "HECARIM", "HEIMERDINGER", "HWEI",
        "ILLAOI", "IRELIA", "IVERN",
        "JANNA", "JARVANIV", "JAX", "JAYCE", "JHIN", "JINX",
        "KAISA", "KALISTA", "KARMA", "KARTHUS", "KASSADIN", "KATARINA", "KAYLE", "KAYN", "KENNEN", "KHAZIX", "KINDRED", "KLED", "KOGMAW", "KSANTE",
        "LEBLANC", "LEESIN", "LEONA", "LILLIA", "LISSANDRA", "LUCIAN", "LULU", "LUX",
        "MALPHITE", "MALZAHAR", "MAOKAI", "MASTERYI", "MILIO", "MISSFORTUNE", "MORDEKAISER", "MORGANA","MEL",
        "NAAFIRI", "NAMI", "NASUS", "NAUTILUS", "NEEKO", "NIDALEE", "NILAH", "NOCTURNE", "NUNU",
        "OLAF", "ORIANNA", "ORNN",
        "PANTHEON", "POPPY", "PYKE",
        "QIYANA", "QUINN",
        "RAKAN", "RAMMUS", "REKSAI", "RELL", "RENATAGLASC", "RENEKTON", "RENGAR", "RIVEN", "RUMBLE", "RYZE",
        "SAMIRA", "SEJUANI", "SENNA", "SERAPHINE", "SETT", "SHACO", "SHEN", "SHYVANA", "SINGED", "SION", "SIVIR", "SKARNER", "SMOLDER", "SONA", "SORAKA", "SWAIN", "SYLAS", "SYNDRA",
        "TAHMKENCH", "TALIYAH", "TALON", "TARIC", "TEEMO", "THRESH", "TRISTANA", "TRUNDLE", "TRYNDAMERE", "TWISTEDFATE", "TWITCH",
        "UDYR", "URGOT",
        "VARUS", "VAYNE", "VEIGAR", "VELKOZ", "VEX", "VI", "VIEGO", "VIKTOR", "VLADIMIR", "VOLIBEAR",
        "WARWICK", "WUKONG",
        "XAYAH", "XERATH", "XINZHAO",
        "YASUO", "YONE", "YORICK", "YUUMI",
        "ZAC", "ZED", "ZERI", "ZIGGS", "ZILEAN", "ZOE", "ZYRA","ZAHEEN",
        
        
        "RABADON", "ZHONYA", "HEXTECH", "DORAN", "LUDEN", "GUINSOO", "LIANDRY", "SHOJIN", "STERAK",
        
       
        "IXTAL", "PILTOVER", "ZAUN", "TARGON", "ARAM", "PORO", "IONIA", "NOXUS", "DEMACIA", "SHURIMA"
    };

    public List<string> valorantWords = new List<string>()
    {
        // Ajanlar
        "BRIMSTONE", "VIPER", "OMEN", "KILLJOY", "CYPHER", "SOVA", "SAGE", "PHOENIX",
        "JETT", "REYNA", "RAZE", "BREACH", "SKYE", "YORU", "ASTRA", "KAYO",
        "CHAMBER", "NEON", "FADE", "HARBOR", "GEKKO", "DEADLOCK", "ISO", "CLOVE",
        "VYSE", "TEJO", "WAYLAY","VETO","MIKS",
        // Silahlar
        "VANDAL", "PHANTOM", "OPERATOR", "SHERIFF", "SPECTRE", "BULLDOG",
        "GUARDIAN", "MARSHAL", "ODIN", "ARES", "STINGER", "BUCKY", "JUDGE",
        "GHOST", "CLASSIC", "SHORTY", "FRENZY",
        // Haritalar
        "ASCENT", "HAVEN", "SPLIT", "BIND", "ICEBOX", "BREEZE", "FRACTURE",
        "PEARL", "LOTUS", "SUNSET", "ABYSS",
        // Diğer
        "RADIANT", "SPIKE", "VALORANT", "PROTOCOL", "KINGDOM"
    };
    public List<string> csWords = new List<string>()
    {
        // Silahlar
        "AWP", "DEAGLE", "GLOCK", "USP", 
        "BIZON",
        "NOVA",
        "FAMAS", "GALIL", "AUG",
        "NEGEV", 
        // Ekipman
        "FLASHBANG", "GRENADE", "MOLOTOV", "SMOKE", "DECOY",
        "HELMET", "DEFUSER",
        // Haritalar
        "MIRAGE", "INFERNO", "NUKE", "OVERPASS", "VERTIGO",
        "ANCIENT", "ANUBIS", "CACHE", "TRAIN",
        // Diğer
        "BOMBSITE", "OPERATION", "PREMIER", "FACEIT", "WINGMAN"
    };
    // Genel Hayat Kelimeleri - Tam 500 Kelime
    public List<string> generalWords = new List<string>()
    {
        // Ev ve Eşyalar
        "MASA", "SANDALYE", "KOLTUK", "KANEPE", "YATAK", "DOLAP", "HALI", "PERDE", "LAMBA", "AYNA",
        "TELEFON", "BİLGİSAYAR", "KLAVYE", "FARE", "EKRAN", "KABLO", "PRİZ", "KULAKLIK", "HOPARLÖR", "VAZO",
        "ÇERÇEVE", "YASTIK", "BATTANİYE", "YORGAN", "ÇARŞAF", "KAPI", "PENCERE", "DUVAR", "TAVAN", "ZEMİN",
        "ÇATI", "BALKON", "MERDİVEN", "ASANSÖR", "BAHÇE", "MUTFAK", "BANYO", "SALON", "KORİDOR", "GARAJ",
        "DEPO", "FIRÇA", "MACUN", "SÜNGER", "PASPAS", "SÜPÜRGE", "KOVA", "BEZ", "SABUN", "ŞAMPUAN",
        // Mutfak ve Yiyecekler
        "FIRIN", "OCAK", "BUZDOLABI", "TAVA", "TENCERE", "TABAK", "BARDAK", "ÇATAL", "KAŞIK", "BIÇAK",
        "SÜRAHİ", "ÇAYDANLIK", "KASE", "SÜZGEÇ", "KAVANOZ", "TEPSİ", "CEZVE", "ŞİŞE", "FİNCAN", "KUPA",
        "PEÇETE", "ÖRTÜ", "SERVİS", "NİHALE", "RENDE", "HAVAN", "MİKSER", "ROBOT", "TOST", "KAHVALTI",
        "AKŞAM", "ÖĞLE", "YEMEK", "İÇECEK", "SU", "SÜT", "YOĞURT", "PEYNİR", "ZEYTİN", "YUMURTA",
        "BAL", "REÇEL", "PEKMEZ", "TAHİN", "ŞEKER", "TUZ", "UN", "YAĞ", "SİRKE", "ÇAY",
        // Meyve, Sebze ve Bakliyat
        "ELMA", "ARMUT", "MUZ", "ÇİLEK", "KİRAZ", "KARPUZ", "KAVUN", "ÜZÜM", "ERİK", "İNCİR",
        "PORTAKAL", "MANDALİNA", "LİMON", "GREYFURT", "ŞEFTALİ", "KAYISI", "NAR", "AYVA", "KİVİ", "ANANAS",
        "DOMATES", "BİBER", "PATLICAN", "KABAK", "SALATALIK", "SOĞAN", "SARIMSAK", "PATATES", "HAVUÇ", "TURP",
        "ISPANAK", "PIRASA", "LAHANA", "MARUL", "MAYDANOZ", "NANE", "FESLEĞEN", "KEKİK", "KİMYON",
        "NOHUT", "FASULYE", "MERCİMEK", "BAKLA", "BEZELYE", "BARBUNYA", "MISIR", "BUĞDAY", "YULAF", "ARPA",
        // Doğa, Bitki ve Coğrafya
        "GÜL", "LALE", "PAPATYA", "MENEKŞE", "KARANFİL", "ORKİDE", "ZAMBAK", "SÜMBÜL", "NERGİS", "BEGONYA",
        "AĞAÇ", "YAPRAK", "ÇİMEN", "KÖK", "DAL", "TOHUM", "ÇAM", "MEŞE", "SÖĞÜT", "ÇINAR",
        "GÜNEŞ", "AY", "YILDIZ", "BULUT", "YAĞMUR", "KAR", "RÜZGAR", "FIRTINA", "SİS", "DOLU",
        "GÖKYÜZÜ", "TOPRAK", "ATEŞ", "HAVA", "DENİZ", "GÖL", "NEHİR", "ŞELALE", "OKYANUS", "DAĞ",
        "TEPE", "VADİ", "ORMAN", "ÇÖL", "ADA", "SAHİL", "PLAJ", "KUM", "KAYA", "TAŞ",
        // Hayvanlar Alemi
        "KEDİ", "KÖPEK", "KUŞ", "BALIK", "TAVŞAN", "KAPLUMBAĞA", "KURBAĞA", "YILAN", "KERTENKELE", "ASLAN",
        "KAPLAN", "AYI", "KURT", "TİLKİ", "CEYLAN", "GEYİK", "ZEBRA", "ZÜRAFA", "FİL", "MAYMUN",
        "KANGURU", "PENGUEN", "FOK", "YUNUS", "BALİNA", "AHTAPOT", "YENGEÇ", "İSTİDYE", "KARTAL", "ŞAHİN",
        "DOĞAN", "BAYKUŞ", "LEYLEK", "MARTI", "GÜVERCİN", "KARGA", "BÜLBÜL", "PAPAĞAN", "ARI", "SİNEK",
        "SİVRİSİNEK", "KELEBEK", "BÖCEK", "ÖRÜMCEK", "KARINCA", "AKREP", "ÇEKİRGE", "İNEK", "KOYUN", "KEÇİ",
        // Vücut ve Biyoloji
        "BAŞ", "SAÇ", "YÜZ", "GÖZ", "KULAK", "BURUN", "AĞIZ", "DUDAK", "DİŞ", "DİL",
        "BOYUN", "OMUZ", "SIRT", "GÖĞÜS", "KARIN", "BEL", "KOL", "DİRSEK", "BİLEK", "EL",
        "PARMAK", "TIRNAK", "BACAK", "DİZ", "AYAK", "KALP", "BEYİN", "KAN", "DAMAR", "KEMİK",
        "KAS", "MİDE", "CİĞER", "BÖBREK", "BAĞIRSAK", "DERİ", "İSKELET", "KAŞ", "KİRPİK", "ÇENE",
        "YANAK", "ALIN", "TOPUK", "İLİK", "EKLEM", "OMURGA", "HÜCRE", "NEFES", "SOLUK", "NABIZ",
        // Giysiler, Aksesuarlar ve Renkler
        "GÖMLEK", "TİŞÖRT", "KAZAK", "MONT", "CEKET", "PALTO", "HIRKA", "YELEK", "KABAN", "YAĞMURLUK",
        "PANTOLON", "ŞORT", "ETEK", "ELBİSE", "TAKIM", "ÇAMAŞIR", "ÇORAP", "ATLET", "KÜLOT", "SÜTYEN",
        "AYAKKABI", "TERLİK", "ÇİZME", "BOT", "ŞAPKA", "BERE", "ATKI", "ELDİVEN", "KEMER", "KRAVAT",
        "KIRMIZI", "MAVİ", "SARI", "YEŞİL", "MOR", "TURUNCU", "BEYAZ", "SİYAH", "GRİ", "KAHVERENGİ",
        "PEMBE", "LACİVERT", "BORDO", "BEJ", "TURKUAZ", "KUMAŞ", "İPLİK", "DÜĞME", "FERMUAR", "CEP",
        // Taşıtlar ve Mekanlar
        "ARABA", "OTOBÜS", "KAMYON", "MİNİBÜS", "TAKSİ", "TRAKTÖR", "TREN", "METRO", "TRAMVAY", "BİSİKLET",
        "MOTOSİKLET", "UÇAK", "HELİKOPTER", "GEMİ", "VAPUR", "TEKNE", "SANDAL", "YAT", "ROKET", "UYDU",
        "HASTANE", "OKUL", "POSTANE", "BANKA", "MARKET", "BAKKAL", "MANAV", "KASAP", "FIRIN", "LOKANTA",
        "RESTORAN", "OTEL", "SİNEMA", "TİYATRO", "MÜZE", "KÜTÜPHANE", "STADYUM", "PARK", "MEYDAN", "CADDE",
        "SOKAK", "BULVAR", "KÖPRÜ", "TÜNEL", "KAVŞAK", "DURAK", "LİMAN", "OTOGAR", "HAVAALANI", "İSTASYON",
        // Meslekler
        "DOKTOR", "HEMŞİRE", "ÖĞRETMEN", "MÜHENDİS", "MİMAR", "AVUKAT", "HAKİM", "SAVCI", "POLİS", "ASKER",
        "İTFAİYECİ", "PİLOT", "KAPTAN", "ŞOFÖR", "AŞÇI", "GARSON", "BERBER", "TERZİ", "ÇİFTÇİ", "İŞÇİ",
        "PATRON", "MÜDÜR", "MEMUR", "BAKAN", "BAŞKAN", "RESSAM", "YAZAR", "ŞAİR", "MÜZİSYEN", "ŞARKICI",
        "OYUNCU", "YÖNETMEN", "SPORCU", "HAKEM", "GAZETECİ", "SUNUCU", "MUHABİR", "FOTOĞRAFÇI", "HEYKELTRAŞ", "KASİYER",
        "KUYUMCU", "DEMİRCİ", "MARANGOZ", "TESİSATÇI", "ELEKTRİKÇİ", "BOYACI", "TEMİZLİKÇİ", "GÜVENLİK", "SEKRETER", "DANIŞMAN",
        // Kavramlar, Duygular ve Sıfatlar
        "AŞK", "SEVGİ", "SAYGI", "KORKU", "NEFRET", "SEVİNÇ", "ÜZÜNTÜ", "HEYECAN", "ŞAŞKINLIK", "ÖFKE",
        "SİNİR", "CESARET", "UMUT", "HAYAL", "RÜYA", "GERÇEK", "YALAN", "DOĞRU", "YANLIŞ", "GÜZEL",
        "ÇİRKİN", "İYİ", "KÖTÜ", "BÜYÜK", "KÜÇÜK", "UZUN", "KISA", "GENİŞ", "DAR", "KALIN",
        "İNCE", "AĞIR", "HAFİF", "HIZLI", "YAVAŞ", "SICAK", "SOĞUK", "ILIK", "SERİN", "ZENGİN",
        "FAKİR", "GENÇ", "YAŞLI", "YENİ", "ESKİ", "TEMİZ", "PİS", "KOLAY", "ZOR", "GÜÇLÜ"
    };
   
    public List<string> countryWords = new List<string>()
    {
        "AFGANİSTAN", "ALMANYA", "AMERİKA", "ANDORRA", "ANGOLA", "ANTİGUA", "ARJANTİN", "ARNAVUTLUK", "AVUSTRALYA", "AVUSTURYA", "AZERBAYCAN",
        "BAHAMALAR", "BAHREYN", "BANGLADEŞ", "BARBADOS", "BELARUS", "BELÇİKA", "BELİZE", "BENİN", "BİRLEŞİKKRALLIK", "BOLİVYA", "BOSNAHERSEK", "BOTSVANA", "BREZİLYA", "BRUNEİ", "BULGARİSTAN", "BURKİNAFASO", "BURUNDİ", "BHUTAN",
        "CİBUTİ", "ÇAD", "ÇEKYA", "ÇİN",
        "DANİMARKA", "DOMİNİK",
        "EKVADOR", "EKVATORGİNESİ", "ELSALVADOR", "ENDONEZYA", "ERİTRE", "ERMENİSTAN", "ESTONYA", "ESVATİNİ", "ETİYOPYA",
        "FAS", "FİJİ", "FİLDİŞİSAHİLİ", "FİLİPİNLER", "FİLİSTİN", "FİNLANDİYA", "FRANSA",
        "GABON", "GAMBİYA", "GANA", "GİNE", "GİNEBİSSAU", "GRENADA", "GUATEMALA", "GUYANA", "GÜNEYAFRİKA", "GÜNEYKORE", "GÜNEYSUDAN", "GÜRCİSTAN",
        "HAİTİ", "HIRVATİSTAN", "HİNDİSTAN", "HOLLANDA", "HONDURAS",
        "IRAK", "İNGİLTERE", "İRAN", "İRLANDA", "İSPANYA", "İSRAİL", "İSVEÇ", "İSVİÇRE", "İTALYA", "İZLANDA",
        "JAMAİKA", "JAPONYA",
        "KAMBOÇYA", "KAMERUN", "KANADA", "KARADAĞ", "KATAR", "KAZAKİSTAN", "KENYA", "KIBRIS", "KIRGIZİSTAN", "KİRİBATİ", "KOLOMBİYA", "KOMORLAR", "KONGO", "KOSOVA", "KOSTARİKA", "KUVEYT", "KUZEYKORE", "MAKEDONYA","KUZEYKIBRIS", "KÜBA",
        "LAOS", "LESOTHO", "LETONYA", "LİBERYA", "LİBYA", "LİECHTENSTEİN", "LİTVANYA", "LÜBNAN", "LÜKSEMBURG",
        "MACARİSTAN", "MADAGASKAR", "MAKEDONYA", "MALAVİ", "MALDİVLER", "MALEZYA", "MALİ", "MALTA", "MARSHALL", "MEKSİKA", "MISIR", "MİKRONEZYA", "MOĞOLİSTAN", "MOLDOVA", "MONAKO", "MORİTANYA", "MORİTİUS", "MOZAMBİK", "MYANMAR",
        "NAMİBYA", "NAURU", "NEPAL", "NİJER", "NİJERYA", "NİKARAGUA", "NORVEÇ",
        "ORTAAFRİKA", "ÖZBEKİSTAN",
        "PAKİSTAN", "PALAU", "PANAMA", "PAPUAYENİGİNE", "PARAGUAY", "PERU", "POLONYA", "PORTEKİZ",
        "ROMANYA", "RUANDA", "RUSYA",
        "SAMOA", "SANMARİNO", "SENEGAL", "SEYŞELLER", "SIRBİSTAN", "SİERRALEONE", "SİNGAPUR", "SLOVAKYA", "SLOVENYA", "SOMALİ", "SRİLANKA", "SUDAN", "SURİNAM", "SURİYE", "SUUDİARABİSTAN",
        "ŞİLİ",
        "TACİKİSTAN", "TANZANYA", "TAYLAND", "TAYVAN", "TOGO", "TONGA", "TRİNİDAD", "TUNUS", "TUVALU", "TÜRKİYE", "TÜRKMENİSTAN",
        "UGANDA", "UKRAYNA", "UMMAN", "URUGUAY", "ÜRDÜN",
        "VANUATU", "VATİKAN", "VENEZUELA", "VİETNAM",
        "YEMEN", "YENİZELANDA", "YEŞİLBURUN", "YUNANİSTAN",
        "ZAMBİYA", "ZİMBABVE"
    };
    
    public List<string> minecraftWords = new List<string>
    {
        // Ana Karakterler ve Efsaneler
        "STEVE", "ALEX", "HEROBRINE", "NOTCH",

        // Pasif ve Nötr Yaratıklar
        "İNEK", "KOYUN", "TAVUK", "AT", "EŞEK","KURT", "KEDİ",
        "PANDA", "KUTUPAYISI", "YARASA", "MÜREKKEPBALIĞI", "KURBAĞA", "AKSOLOTL",
        "ARI", "KÖYLÜ", "DEMİRGOLEM", "KARDANADAM", "YUNUS", "KAPLUMBAĞA", "PAPAĞAN",

        // Düşman Yaratıklar (Mobs)
        "CREEPER", "ZOMBİ", "İSKELET", "ÖRÜMCEK", "ENDERMAN", "GHAST", "BLAZE",
        "SLIME", "MAGMAKÜPÜ", "CADI", "YAĞMACI",
        "WARDEN", "WITHER", "ENDEREJDERHASI", "PİGLİN", "HOGLİN", "BOĞUK",

        // Bloklar ve Madenler
        "ELMAS", "ZÜMRÜT", "ALTIN", "DEMİR", "BAKIR", "KÖMÜR", "KIZILTAŞ", "LAPİS",
        "NETHERİTE", "KUVARS", "AMETİST", "OBSİDYEN", "KATMANKAYASI", "KIRIKTAŞ",
        "TOPRAK", "ÇİMEN", "KUM", "ÇAKIL", "CAM", "SÜNGER",

        // Kızıltaş ve Mekanizmalar
        "PİSTON", "GÖZLEMCİ", "FIRLATICI", "BIRAKICI", "HUNİ", "ŞALTER", "DÜĞME",
        "BASINÇPLAKASI","ŞALTER",

        // Eşyalar ve Aletler
        "ÇALIŞMAMASASI", "OCAK","SANDIK", "ENDERSANDIĞI", "FIÇI",
        "KİTAPLIK", "BÜYÜMASASI", "ÖRS", "İKSİRSTANDI", "KAZAN", "FENER", "MEŞALE",
        "KAMPATEŞİ", "YATAK", "TEKNE", "MADENARABASI", "ELYTRA", "HAVİFİŞEK",
        "KILIÇ", "KAZMA", "BALTA", "KÜREK", "ÇAPA", "YAY", "ARBALET", "OK",
        "ÜÇLÜMIZRAK", "KALKAN", "ZIRH", "OLTA", "MAKAS", "PUSULA", "SAAT", "HARİTA",

        // Yiyecekler ve Tarım
        "ELMA", "ALTINELMA", "HAVUÇ", "PATATES", "EKMEK", "BİFTEK",
        "SOMON", "KİRPİBALIĞI", "KARPUZ", "KABAK", "KURABİYE", "KEK", "PANCAR",
        "KEMİKTOZU", "ŞEKERKAMIŞI", "BUĞDAY",

        // Mekanlar, Biyomlar ve Oyun Terimleri
        "NETHER", "END", "KÖY", "TAPINAK","MINESHAFT",
        "MADEN", "MAĞARA","SPAWNER", "BİYOM", "PORTAL","İKSİR"
    };
}
