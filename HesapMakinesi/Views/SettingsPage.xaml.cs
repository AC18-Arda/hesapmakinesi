using Microsoft.Maui.Storage;

namespace HesapMakinesi.Views;

public partial class SettingsPage : ContentPage
{
    // Sayfa açılırken RadioButton'ları programatik işaretlerken içindeki olayın gereksiz çalışmasını engeleyen yer
    private bool yukleniyor = true;

    public SettingsPage()
    {
        InitializeComponent();

        // Preference ta saklanan son tema seçimini oku yoksa System
        string kayitliTema = Preferences.Default.Get(App.TemaAnahtari, "System");

        // Kayıtlı temaya karşılık gelen RadioButton ı işaretliyor
        switch (kayitliTema)
        {
            case "Light": AcikTemaRadio.IsChecked = true; break;
            case "Dark": KoyuTemaRadio.IsChecked = true; break;
            default: SistemTemaRadio.IsChecked = true; break;
        }

        DurumLabel.Text = $"Geçerli tema: {TemaAdi(kayitliTema)}";
        yukleniyor = false;     // Bizim işlemler burda başlıyo
    }

    // Kullanıcı bir RadioButton seçtiğinde çalışır
    private void OnTemaDegisti(object? sender, CheckedChangedEventArgs e)
    {
        // Sayfa yükleniyorsa ya da bu olay "seçim kaldırıldı" bildirimiyse yok say
        if (yukleniyor || !e.Value) return;

        // Seçilen RadioButton'ın Value değeri "Light", "Dark" veya "System"dir istenen kısım bura işte özellikle kaldığı yerden devam ediyorr
        if (sender is RadioButton secilen && secilen.Value is string tema)
        {
            App.TemaUygula(tema);                           // 1) Temayı ANINDA tüm sayfalara uygula
            Preferences.Default.Set(App.TemaAnahtari, tema);// 2) Kalıcı olarak sakla
            DurumLabel.Text = $"Geçerli tema: {TemaAdi(tema)}";
        }
    }

    // Tema anahtarını kullanıcıya gösterilecek Türkçe isimlere çevirdiğimiz yerr
    private static string TemaAdi(string tema) => tema switch
    {
        "Light" => "Açık",
        "Dark" => "Koyu",
        _ => "Sistem ayarı"
    };
}