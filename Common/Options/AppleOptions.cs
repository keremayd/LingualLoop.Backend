namespace Common.Options;

/// <summary>
/// Apple ile giriş.
///
/// Kimlik token'ının alıcısı (<c>aud</c>) uygulamanın paket kimliği olmalı:
/// başka bir uygulama için verilmiş Apple token'ı bu sunucuda oturum açmaz.
/// Sır değil, <c>appsettings.json</c>'da durur. Boşsa Apple ile giriş kapalıdır.
///
/// Hesap silinirken Apple'daki izin de geri alınır (App Store 5.1.1(v),
/// Apple TN3194). Bunun için geliştirici hesabındaki "Sign in with Apple"
/// anahtarı gerekir: <see cref="TeamId"/>, <see cref="KeyId"/> ve
/// <see cref="PrivateKeyPath"/>. Üçü <c>appsettings.Local.json</c>'da durur;
/// biri boşsa izin geri alınmaz, hesap yine silinir (loga yazılır).
/// </summary>
public class AppleOptions
{
    public const string SectionName = "Apple";

    /// <summary>iOS uygulamasının paket kimliği (bundle identifier).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Geliştirici hesabının takım kimliği (Membership sayfası).</summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>"Sign in with Apple" anahtarının kimliği (Keys sayfası).</summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Anahtarın <c>.p8</c> dosyasının yolu. Dosya sırdır: depolara girmez.
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>İzni geri almak için gereken her şey yapılandırılmış mı.</summary>
    public bool CanRevoke =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(TeamId) &&
        !string.IsNullOrWhiteSpace(KeyId) &&
        !string.IsNullOrWhiteSpace(PrivateKeyPath);
}
