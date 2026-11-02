namespace HesapMakinesi;

// Uygulamanın gezinme iskeleti. Menü yapısı tamamen AppShell.xaml içinde tanımlıdır.
public partial class AppShell : Shell
{
    public AppShell()
    {
        // XAML'de tanımlanan menü öğelerini ve sayfaları yükler
        InitializeComponent();
    }
}