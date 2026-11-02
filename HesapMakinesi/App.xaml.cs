using Microsoft.Maui.Storage;

namespace HesapMakinesi;

public partial class App : Application
{
    // Tema seçiminin Preferences içinde saklanacağı anahtar (SettingsPage de bunu kullanır)
    public const string TemaAnahtari = "TemaSecimi";

    public App()
    {
        InitializeComponent();

        // Uygulama her açıldığında son seçilen temayı Preferences'tan oku (ilk açılışta "System")
        string kayitliTema = Preferences.Default.Get(TemaAnahtari, "System");

        // Okunan temayı uygulamaya uygula
        TemaUygula(kayitliTema);
    }

    // "Light", "Dark" veya "System" metnini AppTheme değerine çevirip anında uygular
    public static void TemaUygula(string tema)
    {
        // Uygulama nesnesi yoksa (olmaması gerekir) işlem yapma
        if (Current is null) return;

        Current.UserAppTheme = tema switch
        {
            "Light" => AppTheme.Light,       // Açık tema
            "Dark" => AppTheme.Dark,         // Koyu tema
            _ => AppTheme.Unspecified        // Sistem ayarını kullan
        };
    }

    // Uygulama penceresini oluşturur ve içine Shell'i koyar
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell()) { Title = "Hesap Makinesi" };
    }
}