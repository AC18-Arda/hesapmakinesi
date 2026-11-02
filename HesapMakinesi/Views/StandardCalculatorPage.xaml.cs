using System.Globalization;

namespace HesapMakinesi.Views;

// Standart mod
public partial class StandardCalculatorPage : ContentPage
{
    // Durum kısmı
    private const int MaksRakam = 15;          // Ekrana girilebilecek en fazla rakam sayısı
    private string girdi = "0";                // Ekrandaki sayının ham hali 
    private double ilkSayi = 0;                // Bekleyen işlemin sol kısmı
    private string bekleyenIslem = "";         // "+", "-", "×", "÷" ya da boş
    private bool yeniGirdi = true;             // true ise sıradaki rakam ekranı sıfırlar ve yeni sayı başlatır
    private bool operandBekleniyor = false;    // İşleme basıldı, sağ operand henüz girilmedi
    private double hafiza = 0;                 // MC, MR, M+, M-, MS için hafıza değeri

    public StandardCalculatorPage()
    {
        InitializeComponent();   // XAML i yükler
        EkraniGuncelle();        // Açılışta "0" gösterir
    }


    // Butonun Text değeri rakamdır
    private void OnRakamClicked(object? sender, EventArgs e)
    {
        if (sender is not Button buton) return;
        string rakam = buton.Text;

        // Yeni sayı başlıyorsa ya da ekranda sadece "0" varsa ekranı rakamla değiştir
        if (yeniGirdi || girdi == "0")
        {
            girdi = rakam;
        }
        else
        {
            // Rakam sayısı sınırını aşmasın max sayı kısmı
            if (girdi.Count(char.IsDigit) >= MaksRakam) return;
            girdi += rakam;
        }

        yeniGirdi = false;
        operandBekleniyor = false;
        EkraniGuncelle();
    }

    // Ondalık ayracı (virgül) butonu
    private void OnVirgulClicked(object? sender, EventArgs e)
    {
        if (yeniGirdi)
        {
            girdi = "0.";            // Yeni sayı "0," ile başlar
            yeniGirdi = false;
        }
        else if (!girdi.Contains('.'))
        {
            girdi += ".";            // Aynı sayıda ikinci bir virgül eklenemez
        }
        operandBekleniyor = false;
        EkraniGuncelle();
    }

    // son karakteri siler
    private void OnGeriClicked(object? sender, EventArgs e)
    {
        if (yeniGirdi) return;       // Sonuç ekrandaysa silinmez
        girdi = girdi.Length > 1 ? girdi[..^1] : "0";
        if (girdi == "-" || girdi == "") girdi = "0";
        EkraniGuncelle();
    }

    // CE: sadece ekrandaki girdiyi temizler, bekleyen işlem korunur
    private void OnCEClicked(object? sender, EventArgs e)
    {
        girdi = "0";
        yeniGirdi = true;
        operandBekleniyor = false;
        EkraniGuncelle();
    }

    // C: her şeyi sıfırlar
    private void OnCClicked(object? sender, EventArgs e) => TumunuTemizle();

    // DÖRT İŞLEM

    // +, -, ×, ÷ butonları 
    private async void OnIslemClicked(object? sender, EventArgs e)
    {
        if (sender is not Button buton || buton.CommandParameter is not string yeniIslem) return;

        await GuvenliCalistirAsync(() =>
        {
            // Kullanıcı arka arkaya iki işlem butonuna bastıysa yalnızca işlemi değiştir
            if (bekleyenIslem != "" && operandBekleniyor)
            {
                bekleyenIslem = yeniIslem;
                IfadeLabel.Text = $"{Virgullu(SayiyiMetneCevir(ilkSayi))} {yeniIslem}";
                return;
            }

            double mevcut = GirdiyiOku();

            // Zincirleme işlem sırayla yapar işte
            if (bekleyenIslem != "")
                mevcut = Hesapla(ilkSayi, mevcut, bekleyenIslem);

            ilkSayi = mevcut;                         // Sonuç yeni sol operand olur
            girdi = SayiyiMetneCevir(mevcut);         // Ekranda ara sonuç gösterilir
            bekleyenIslem = yeniIslem;
            operandBekleniyor = true;
            yeniGirdi = true;
            IfadeLabel.Text = $"{Virgullu(girdi)} {yeniIslem}";
            EkraniGuncelle();
        });
    }

    // = butonu
    private async void OnEsittirClicked(object? sender, EventArgs e)
    {
        await GuvenliCalistirAsync(() =>
        {
            // Bekleyen işlem yoksa yapacağın bişi yok işte 
            if (bekleyenIslem == "")
            {
                IfadeLabel.Text = "";
                yeniGirdi = true;
                return;
            }

            double sag = GirdiyiOku();                              // Sağ operand = ekrandaki sayı
            double sonuc = Hesapla(ilkSayi, sag, bekleyenIslem);    // Sıfıra bölme burada yakalanır

            // Üst satırda tam işlemi gösteren yer
            IfadeLabel.Text = $"{Virgullu(SayiyiMetneCevir(ilkSayi))} {bekleyenIslem} {Virgullu(SayiyiMetneCevir(sag))} =";

            girdi = SayiyiMetneCevir(sonuc);
            bekleyenIslem = "";
            yeniGirdi = true;
            operandBekleniyor = false;
            EkraniGuncelle();
        });
    }

    //TEK SAYIYA UYGULANAN İŞLEMLER

    // %, 1/x, x², √x, ± butonları
    private async void OnTekliIslemClicked(object? sender, EventArgs e)
    {
        if (sender is not Button buton || buton.CommandParameter is not string anahtar) return;

        await GuvenliCalistirAsync(() =>
        {
            // ± işareti metin üzerinde değiştirilir; "5." gibi yarım sayılar bozulmaz
            if (anahtar == "isaret")
            {
                if (girdi != "0")
                    girdi = girdi.StartsWith('-') ? girdi[1..] : "-" + girdi;
                operandBekleniyor = false;
                EkraniGuncelle();
                return;
            }

            double x = GirdiyiOku();
            double sonuc;

            switch (anahtar)
            {
                case "yuzde":
                    //  Bekleyen işlem varsa ilk sayının yüzdesi, yoksa 0
                    sonuc = bekleyenIslem != "" ? ilkSayi * x / 100 : 0;
                    break;
                case "ters":
                    if (x == 0)
                        throw new DivideByZeroException("Sıfıra bölme hatası: 0 sayısının tersi (1/x) alınamaz.");
                    sonuc = 1 / x;
                    break;
                case "kare":
                    sonuc = x * x;
                    break;
                case "karekok":
                    if (x < 0)
                        throw new ArithmeticException("Geçersiz giriş: negatif bir sayının karekökü alınamaz.");
                    sonuc = Math.Sqrt(x);
                    break;
                default:
                    return;
            }

            girdi = SayiyiMetneCevir(sonuc);
            yeniGirdi = true;
            operandBekleniyor = false;
            EkraniGuncelle();
        });
    }

    //  HAFIZA İŞLEMLERİ
    private async void OnHafizaClicked(object? sender, EventArgs e)
    {
        if (sender is not Button buton || buton.CommandParameter is not string komut) return;

        await GuvenliCalistirAsync(() =>
        {
            switch (komut)
            {
                case "MC": hafiza = 0; break;                                 // Hafızayı sil
                case "MR":                                                     // Hafızayı ekrana getir
                    girdi = SayiyiMetneCevir(hafiza);
                    yeniGirdi = true;
                    operandBekleniyor = false;
                    break;
                case "M+": hafiza += GirdiyiOku(); yeniGirdi = true; break;   // Hafızaya ekle
                case "M-": hafiza -= GirdiyiOku(); yeniGirdi = true; break;   // Hafızadan çıkar
                case "MS": hafiza = GirdiyiOku(); yeniGirdi = true; break;    // Hafızaya kaydet
            }
            EkraniGuncelle();
        });
    }

    // YARDIMCI METOTLAR

    // İki sayı ile işlemi yapar. Sıfıra bölmede DivideByZeroException fırlatır.
    private static double Hesapla(double a, double b, string islem)
    {
        switch (islem)
        {
            case "+": return a + b;
            case "-": return a - b;
            case "×": return a * b;
            case "÷":
                if (b == 0)
                    throw new DivideByZeroException("Sıfıra bölme hatası: bir sayı 0'a bölünemez.");
                return a / b;
            default:
                throw new InvalidOperationException("Bilinmeyen işlem: " + islem);
        }
    }

    // Ekrandaki ham metni double'a çevirir (InvariantCulture: ondalık ayracı nokta)
    private double GirdiyiOku() => double.Parse(girdi, CultureInfo.InvariantCulture);

    // double → ham metin. 15 anlamlı basamak, 0,1+0,2 gibi sonuçlar "0,3" görünür.
    private static string SayiyiMetneCevir(double d)
    {
        // NaN veya sonsuz sonuç geçersizdir
        if (double.IsNaN(d) || double.IsInfinity(d))
            throw new ArithmeticException("Sonuç tanımsız veya hesaplanamayacak kadar büyük.");
        if (d == 0) return "0";                              // "-0" görünmesini engeller
        return d.ToString("G15", CultureInfo.InvariantCulture);
    }

    // Ham metindeki noktayı Türkçe virgüle çevirir (sadece gösterim için)
    private static string Virgullu(string metin) => metin.Replace('.', ',');

    // Ekran etiketini günceller
    private void EkraniGuncelle() => EkranLabel.Text = Virgullu(girdi);

    // Tüm durumu başlangıca döndürür
    private void TumunuTemizle()
    {
        girdi = "0";
        ilkSayi = 0;
        bekleyenIslem = "";
        yeniGirdi = true;
        operandBekleniyor = false;
        IfadeLabel.Text = "";
        EkraniGuncelle();
    }

    //  HATA YÖNETİMİ KISMI MESELA 0 A BÖLME

    // Verilen işlemi try-catch içinde çalıştırır. Hata olursa uygulama çökmez, kullanıcı uyarılır.
    private async Task GuvenliCalistirAsync(Action islem)
    {
        try
        {
            islem();
        }
        catch (ArithmeticException ex)       // Sıfıra bölme, negatif karekök, taşma vb.
        {
            await HataGosterAsync(ex.Message);
        }
        catch (Exception ex)                 // Beklenmeyen diğer hatalar
        {
            await HataGosterAsync($"Beklenmeyen bir hata oluştu: {ex.Message}");
        }
    }

    // Hesap makinesini sıfırlar ve kullanıcıya uyarı penceresi gösterir
    private async Task HataGosterAsync(string mesaj)
    {
        TumunuTemizle();
      
        await DisplayAlertAsync("Hata", mesaj, "Tamam");
    }
}