using System.Globalization;

namespace HesapMakinesi.Views;

// Bilimsel mod: ifade (belirteç listesi) tabanlı çalışır. Parantez ve işlem önceliği desteklenir.
// Öncelik sırası (yüksekten düşüğe): ^ ve exp  →  × ÷ mod  →  + -
public partial class ScientificCalculatorPage : ContentPage
{
    // DURUM  ALANLARI
    private const int MaksRakam = 15;                       // Girilebilecek en fazla rakam sayısı
    private readonly List<string> ifade = new();            // İfadeyi oluşturan belirteçler: "2", "+", "(", ...
    private string girdi = "0";                             // Ekrandaki sayı (ham metin, ondalık ayracı nokta)
    private bool yeniGirdi = true;                          // true ise sıradaki rakam ekranı sıfırlar
    private bool girdiHazir = true;                         // true ise ekrandaki sayı ifadeye eklenmeyi bekleyen bir işlenendir
    private int acikParantez = 0;                           // Henüz kapatılmamış parantez sayısı
    private bool ikinciAktif = false;                       // 2nd tuşu açık mı?
    private bool dereceModu = true;                         // true = DEG (derece), false = RAD (radyan)
    private double hafiza = 0;                              // Hafıza değeri

    // Ayrıştırıcının  kullandığı alanlar
    private List<string> hesapListesi = new();
    private int konum = 0;

    // İfadedeki son belirteç yoksa boş metin
    private string SonBelirtec => ifade.Count > 0 ? ifade[^1] : "";

    public ScientificCalculatorPage()
    {
        InitializeComponent();
        EkraniGuncelle();
    }

    // RAKAM / VİRGÜL / SİLME 

    private void OnRakamClicked(object? sender, EventArgs e)
    {
        if (sender is not Button buton) return;
        string rakam = buton.Text;

        if (yeniGirdi || girdi == "0")
        {
            girdi = rakam;                                        // Yeni sayı başlat
        }
        else
        {
            if (girdi.Count(char.IsDigit) >= MaksRakam) return;   // Rakam sınırı
            girdi += rakam;
        }

        yeniGirdi = false;
        girdiHazir = true;                                        // Artık ifadeye eklenebilecek bir sayı var
        EkraniGuncelle();
    }

    private void OnVirgulClicked(object? sender, EventArgs e)
    {
        if (yeniGirdi)
        {
            girdi = "0.";
            yeniGirdi = false;
        }
        else if (!girdi.Contains('.'))
        {
            girdi += ".";
        }
        girdiHazir = true;
        EkraniGuncelle();
    }

    private void OnGeriClicked(object? sender, EventArgs e)
    {
        if (yeniGirdi) return;                                    // Sonuç / sabit ekrandaysa silme
        girdi = girdi.Length > 1 ? girdi[..^1] : "0";
        if (girdi == "-" || girdi == "") girdi = "0";
        EkraniGuncelle();
    }

    private void OnTemizleClicked(object? sender, EventArgs e) => TumunuTemizle();

    // 2nd VE DEG/RAD DÜĞMELERİ 

    // 2nd: butonların yazısını ve çalıştıracağı fonksiyonu değiştirir
    private void OnIkinciClicked(object? sender, EventArgs e)
    {
        ikinciAktif = !ikinciAktif;

        KareButonu.Text = ikinciAktif ? "x³" : "x²";
        KareButonu.CommandParameter = ikinciAktif ? "kup" : "kare";

        KarekokButonu.Text = ikinciAktif ? "∛x" : "√x";
        KarekokButonu.CommandParameter = ikinciAktif ? "kupkok" : "karekok";

        OnUzeriButonu.Text = ikinciAktif ? "2^x" : "10^x";
        OnUzeriButonu.CommandParameter = ikinciAktif ? "ikiUzeri" : "onUzeri";

        LnButonu.Text = ikinciAktif ? "e^x" : "ln";
        LnButonu.CommandParameter = ikinciAktif ? "eUzeri" : "ln";

        SinButonu.Text = ikinciAktif ? "sin⁻¹" : "sin";
        SinButonu.CommandParameter = ikinciAktif ? "asin" : "sin";

        CosButonu.Text = ikinciAktif ? "cos⁻¹" : "cos";
        CosButonu.CommandParameter = ikinciAktif ? "acos" : "cos";

        TanButonu.Text = ikinciAktif ? "tan⁻¹" : "tan";
        TanButonu.CommandParameter = ikinciAktif ? "atan" : "tan";

        // 2nd açıkken buton yazısı kalın olur (renk/boyut kodla değiştirilmez)
        IkinciButonu.FontAttributes = ikinciAktif ? FontAttributes.Bold : FontAttributes.None;
    }

    // DEG ↔ RAD geçişi (trigonometrik fonksiyonlar bu moda göre hesaplanır)
    private void OnAciModuClicked(object? sender, EventArgs e)
    {
        dereceModu = !dereceModu;
        DereceButonu.Text = dereceModu ? "DEG" : "RAD";
    }

    //  İŞLEM (OPERATÖR) BUTONLARI 

    // +, -, ×, ÷, ^ (x^y), mod, exp (×10^) butonları
    private void OnIslemClicked(object? sender, EventArgs e)
    {
        if (sender is not Button buton || buton.CommandParameter is not string islem) return;

        if (girdiHazir)
        {
            // Ekrandaki sayı ifadeye eklenir, ardından işlem eklenir
            GirdiyiIfadeyeEkle();
            ifade.Add(islem);
        }
        else if (SonBelirtec == ")")
        {
            ifade.Add(islem);                        // (2+3) + gibi: kapanış parantezinden sonra işlem
        }
        else if (OperatorMu(SonBelirtec))
        {
            ifade[^1] = islem;                       // Üst üste işlem basıldıysa sonuncusu geçerli olur
        }
        else
        {
            return;                                  // "(" den hemen sonra işlem girilemez
        }

        girdiHazir = false;                          // Şimdi sağ operand bekleniyor
        yeniGirdi = true;
        IfadeyiGuncelle();
    }

    // "(" butonu
    private void OnParantezAcClicked(object? sender, EventArgs e)
    {
        if (girdiHazir && !yeniGirdi)
        {
            // 2( → 2 × ( şeklinde örtük çarpma
            GirdiyiIfadeyeEkle();
            ifade.Add("×");
        }
        else if (SonBelirtec == ")")
        {
            ifade.Add("×");                         
        }

        ifade.Add("(");
        acikParantez++;
        girdi = "0";
        girdiHazir = false;
        yeniGirdi = true;
        IfadeyiGuncelle();
        EkraniGuncelle();
    }

    // ")" butonu
    private void OnParantezKapatClicked(object? sender, EventArgs e)
    {
        if (acikParantez == 0) return;               // Açık parantez yoksa kapatılamaz

        if (girdiHazir)
            GirdiyiIfadeyeEkle();                    // Yazılmış sayıyı ekle
        else if (OperatorMu(SonBelirtec))
            GirdiyiIfadeyeEkle();                    // "(2 +)" durumunda ekrandaki sayı kullanılır
        else if (SonBelirtec == "(")
            return;                                  // Boş parantez "()" geçersiz

        ifade.Add(")");
        acikParantez--;
        girdiHazir = false;
        yeniGirdi = true;
        IfadeyiGuncelle();
    }

    // = butonu kısmı
    private async void OnEsittirClicked(object? sender, EventArgs e)
    {
        await GuvenliCalistirAsync(() =>
        {
            // İfade yoksa ekrandaki sayı zaten sonuçtur
            if (ifade.Count == 0)
            {
                yeniGirdi = true;
                return;
            }

            // İfade eksik bitmişse (örn. "5 +" veya "5 + (") ekrandaki sayı son işlenen olarak eklenir
            if (girdiHazir || OperatorMu(SonBelirtec) || SonBelirtec == "(")
                GirdiyiIfadeyeEkle();

            // Açık kalan parantezleri otomatik kapat
            while (acikParantez > 0)
            {
                ifade.Add(")");
                acikParantez--;
            }

            string ifadeMetni = string.Join(" ", ifade);
            double sonuc = Degerlendir(ifade);       // Hata varsa istisna fırlatır

            girdi = SayiyiMetneCevir(sonuc);
            IfadeLabel.Text = Virgullu(ifadeMetni) + " =";
            ifade.Clear();
            girdiHazir = true;
            yeniGirdi = true;
            EkraniGuncelle();
        });
    }

    // FONKSİYON BUTONLARI 

    // π, e, x², √x, 1/x, |x|, n!, 10^x, log, ln, sin, cos, tan, ± ... (anahtar CommandParameter'dan gelir)
    private async void OnFonksiyonClicked(object? sender, EventArgs e)
    {
        if (sender is not Button buton || buton.CommandParameter is not string anahtar) return;

        await GuvenliCalistirAsync(() =>
        {
            // ± metin üzerinde değiştirilir
            if (anahtar == "isaret")
            {
                if (girdi != "0")
                    girdi = girdi.StartsWith('-') ? girdi[1..] : "-" + girdi;
                girdiHazir = true;
                EkraniGuncelle();
                return;
            }

            double x = GirdiyiOku();
            double sonuc = FonksiyonHesapla(anahtar, x);   // Geçersiz girişte istisna fırlatır

            girdi = SayiyiMetneCevir(sonuc);
            girdiHazir = true;                             // Sonuç artık ifadeye eklenebilir bir işlenendir
            yeniGirdi = true;
            EkraniGuncelle();
        });
    }

    // Seçilen fonksiyonu x değeri için hesaplar
    private double FonksiyonHesapla(string anahtar, double x)
    {
        switch (anahtar)
        {
            case "pi": return Math.PI;
            case "e": return Math.E;
            case "kare": return x * x;
            case "kup": return x * x * x;
            case "ters":
                if (x == 0)
                    throw new DivideByZeroException("Sıfıra bölme hatası: 0 sayısının tersi (1/x) alınamaz.");
                return 1 / x;
            case "mutlak": return Math.Abs(x);
            case "karekok":
                if (x < 0)
                    throw new ArithmeticException("Geçersiz giriş: negatif bir sayının karekökü alınamaz.");
                return Math.Sqrt(x);
            case "kupkok": return Math.Cbrt(x);
            case "faktoriyel": return Faktoriyel(x);
            case "onUzeri": return Math.Pow(10, x);
            case "ikiUzeri": return Math.Pow(2, x);
            case "eUzeri": return Math.Exp(x);
            case "log":
                if (x <= 0)
                    throw new ArithmeticException("Geçersiz giriş: logaritma yalnızca pozitif sayılar için tanımlıdır.");
                return Math.Log10(x);
            case "ln":
                if (x <= 0)
                    throw new ArithmeticException("Geçersiz giriş: doğal logaritma yalnızca pozitif sayılar için tanımlıdır.");
                return Math.Log(x);

            // Trigonometri: sonuçlar 12 basamağa yuvarlanır, böylece sin(180°) = 0 görünür
            case "sin": return Math.Round(Math.Sin(AciyiRadyanaCevir(x)), 12);
            case "cos": return Math.Round(Math.Cos(AciyiRadyanaCevir(x)), 12);
            case "tan":
                {
                    double r = AciyiRadyanaCevir(x);
                    // cos ≈ 0 ise tan tanımsızdır (90°, 270° ...)
                    if (Math.Abs(Math.Cos(r)) < 1e-12)
                        throw new ArithmeticException("Geçersiz giriş: tan fonksiyonu bu açı için tanımsızdır.");
                    return Math.Round(Math.Tan(r), 12);
                }
            case "asin":
                if (x < -1 || x > 1)
                    throw new ArithmeticException("Geçersiz giriş: sin⁻¹ yalnızca -1 ile 1 arasındaki değerler için tanımlıdır.");
                return Math.Round(RadyaniAciyaCevir(Math.Asin(x)), 12);
            case "acos":
                if (x < -1 || x > 1)
                    throw new ArithmeticException("Geçersiz giriş: cos⁻¹ yalnızca -1 ile 1 arasındaki değerler için tanımlıdır.");
                return Math.Round(RadyaniAciyaCevir(Math.Acos(x)), 12);
            case "atan":
                return Math.Round(RadyaniAciyaCevir(Math.Atan(x)), 12);

            default:
                throw new InvalidOperationException("Bilinmeyen fonksiyon: " + anahtar);
        }
    }

    // n! hesabı: yalnızca 0-170 arası tam sayılar
    private static double Faktoriyel(double x)
    {
        if (x < 0 || x != Math.Floor(x))
            throw new ArithmeticException("Geçersiz giriş: faktöriyel yalnızca negatif olmayan tam sayılar için tanımlıdır.");
        if (x > 170)
            throw new ArithmeticException("Sonuç çok büyük: 170'ten büyük sayıların faktöriyeli hesaplanamaz.");

        double sonuc = 1;
        for (int i = 2; i <= (int)x; i++) sonuc *= i;
        return sonuc;
    }

    // Açı modu DEG ise dereceyi radyana çevirir
    private double AciyiRadyanaCevir(double aci) => dereceModu ? aci * Math.PI / 180 : aci;

    // Açı modu DEG ise radyanı dereceye çevirir
    private double RadyaniAciyaCevir(double radyan) => dereceModu ? radyan * 180 / Math.PI : radyan;

    //  HAFIZA İŞLEMLERİ 
    private async void OnHafizaClicked(object? sender, EventArgs e)
    {
        if (sender is not Button buton || buton.CommandParameter is not string komut) return;

        await GuvenliCalistirAsync(() =>
        {
            switch (komut)
            {
                case "MC": hafiza = 0; break;
                case "MR":
                    girdi = SayiyiMetneCevir(hafiza);
                    girdiHazir = true;
                    yeniGirdi = true;
                    break;
                case "M+": hafiza += GirdiyiOku(); yeniGirdi = true; break;
                case "M-": hafiza -= GirdiyiOku(); yeniGirdi = true; break;
                case "MS": hafiza = GirdiyiOku(); yeniGirdi = true; break;
            }
            EkraniGuncelle();
        });
    }

    // İFADE DEĞERLENDİĞİ KISIM

    // Belirteç listesini işlem önceliğine göre hesaplar
    private double Degerlendir(List<string> belirtecler)
    {
        hesapListesi = belirtecler;
        konum = 0;
        double sonuc = ToplamaCikarma();
        // Tüm belirteçler tüketilmediyse ifade bozuktur (örn. fazladan ")" )
        if (konum != hesapListesi.Count)
            throw new FormatException("İfade geçersiz.");
        return sonuc;
    }

    // Şu anki belirteci döndürür (liste bittiyse boş metin)
    private string Bak() => konum < hesapListesi.Count ? hesapListesi[konum] : "";

    // En düşük öncelik: + ve -
    private double ToplamaCikarma()
    {
        double sol = CarpmaBolme();
        while (Bak() is "+" or "-")
        {
            string op = hesapListesi[konum++];
            double sag = CarpmaBolme();
            sol = op == "+" ? sol + sag : sol - sag;
        }
        return sol;
    }

    // Orta öncelik: ×, ÷, mod
    private double CarpmaBolme()
    {
        double sol = UsAlma();
        while (Bak() is "×" or "÷" or "mod")
        {
            string op = hesapListesi[konum++];
            double sag = UsAlma();

            if (op == "×")
            {
                sol *= sag;
            }
            else
            {
                // ÷ ve mod için sıfıra bölme kontrolü
                if (sag == 0)
                    throw new DivideByZeroException("Sıfıra bölme hatası: bir sayı 0'a bölünemez.");
                sol = op == "÷" ? sol / sag : sol % sag;
            }
        }
        return sol;
    }

    // Yüksek öncelik: ^ ve exp (sağdan sola birleşir: 2^3^2 = 2^(3^2))
    private double UsAlma()
    {
        double taban = Birincil();
        if (Bak() == "^")
        {
            konum++;
            return Math.Pow(taban, UsAlma());
        }
        if (Bak() == "×10^")
        {
            konum++;
            return taban * Math.Pow(10, UsAlma());   // a exp b = a × 10^b
        }
        return taban;
    }

    // En yüksek öncelik: sayı veya parantezli ifade
    private double Birincil()
    {
        string t = Bak();

        if (t == "(")
        {
            konum++;
            double ic = ToplamaCikarma();            // Parantezin içini hesapla
            if (Bak() != ")") throw new FormatException("Parantez eksik.");
            konum++;
            return ic;
        }

        // Sayı değilse (operatör, ")" ya da liste sonu) ifade bozuktur
        if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double sayi))
            throw new FormatException("İfade geçersiz.");

        konum++;
        return sayi;
    }

 

    // Belirteç bir operatör mü?
    private static bool OperatorMu(string t) => t is "+" or "-" or "×" or "÷" or "^" or "mod" or "×10^";

    // Ekrandaki sayıyı ifadeye ekler. Kapanış parantezinden hemen sonra gelirse araya × koyar.
    private void GirdiyiIfadeyeEkle()
    {
        if (SonBelirtec == ")") ifade.Add("×");
        ifade.Add(girdi);
    }

    private double GirdiyiOku() => double.Parse(girdi, CultureInfo.InvariantCulture);

    private static string SayiyiMetneCevir(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d))
            throw new ArithmeticException("Sonuç tanımsız veya hesaplanamayacak kadar büyük.");
        if (d == 0) return "0";
        return d.ToString("G15", CultureInfo.InvariantCulture);
    }

    private static string Virgullu(string metin) => metin.Replace('.', ',');

    private void EkraniGuncelle() => EkranLabel.Text = Virgullu(girdi);

    private void IfadeyiGuncelle() => IfadeLabel.Text = Virgullu(string.Join(" ", ifade));

    private void TumunuTemizle()
    {
        ifade.Clear();
        girdi = "0";
        yeniGirdi = true;
        girdiHazir = true;
        acikParantez = 0;
        IfadeLabel.Text = "";
        EkraniGuncelle();
    }

   
    private async Task GuvenliCalistirAsync(Action islem)
    {
        try
        {
            islem();
        }
        catch (FormatException)              // Bozuk ifade (örn. "5 + × 3" ya da eksik parantez)
        {
            await HataGosterAsync("Geçersiz bir ifade girdiniz. Lütfen işlemi kontrol edin.");
        }
        catch (ArithmeticException ex)       // Sıfıra bölme, negatif log/karekök, faktöriyel sınırı...
        {
            await HataGosterAsync(ex.Message);
        }
        catch (Exception ex)                 // Beklenmeyen hatalar
        {
            await HataGosterAsync($"Beklenmeyen bir hata oluştu: {ex.Message}");
        }
    }

    private async Task HataGosterAsync(string mesaj)
    {
        TumunuTemizle();                                   // Hesap makinesini sıfırla
        await DisplayAlertAsync("Hata", mesaj, "Tamam");   // Kullanıcıya anlaşılır uyarı
    }
}