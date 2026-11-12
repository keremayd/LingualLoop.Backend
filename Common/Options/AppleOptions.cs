namespace Common.Options;

/// <summary>
/// Apple ile giriş.
///
/// Kimlik token'ının alıcısı (<c>aud</c>) uygulamanın paket kimliği olmalı:
/// başka bir uygulama için verilmiş Apple token'ı bu sunucuda oturum açmaz.
/// Sır değil, <c>appsettings.json</c>'da durur. Boşsa Apple ile giriş kapalıdır.
/// </summary>
public class AppleOptions
{
    public const string SectionName = "Apple";

    /// <summary>iOS uygulamasının paket kimliği (bundle identifier).</summary>
    public string ClientId { get; set; } = string.Empty;
}
