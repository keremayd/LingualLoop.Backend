using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using Common.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Service.Helpers;

/// <summary>
/// Hesap silinirken kullanıcının Apple'daki iznini geri alır (App Store
/// 5.1.1(v), Apple TN3194): Apple ile girişin bu uygulamaya verdiği yetki
/// biter, uygulama kullanıcının Apple hesabındaki "Apple ile giriş kullanan
/// uygulamalar" listesinden çıkar.
///
/// Token saklamıyoruz. Silme anında uygulama Apple penceresini bir kez daha
/// açar ve tek kullanımlık bir yetki kodu alır (Apple'a göre 5 dakika
/// geçerli). Kod burada yenileme token'ına çevrilir, o token geri alınır.
///
/// İstemci sırrı, geliştirici hesabındaki "Sign in with Apple" anahtarıyla
/// (ES256) imzalanmış kısa ömürlü bir JWT; her silmede yeniden üretilir.
/// </summary>
public sealed class AppleTokenRevoker
{
    private const string Audience = "https://appleid.apple.com";
    private const string TokenUrl = "https://appleid.apple.com/auth/token";
    private const string RevokeUrl = "https://appleid.apple.com/auth/revoke";
    private static readonly TimeSpan SecretLifetime = TimeSpan.FromMinutes(5);
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private readonly AppleOptions _options;

    public AppleTokenRevoker(IOptions<AppleOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>Anahtar yapılandırılmış mı; değilse izin geri alınamaz.</summary>
    public bool IsConfigured => _options.CanRevoke;

    /// <summary>
    /// Yetki kodunu token'a çevirip geri alır. Apple reddederse
    /// <see cref="AppleRevokeException"/>; ağ hatası olduğu gibi yükselir.
    /// </summary>
    public async Task RevokeAsync(string authorizationCode, CancellationToken cancellationToken)
    {
        var secret = CreateClientSecret(DateTime.UtcNow);

        using var tokenResponse = await Http.PostAsync(TokenUrl, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = secret,
                ["code"] = authorizationCode,
                ["grant_type"] = "authorization_code",
            }), cancellationToken);
        var body = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new AppleRevokeException(
                $"kod token'a çevrilemedi: {(int)tokenResponse.StatusCode} {ErrorOf(body)}");
        }

        // Yenileme token'ı kullanıcının iznini temsil ediyor, geri alınınca
        // izin biter. Apple vermediyse erişim token'ı da aynı işi görür.
        string? token;
        string hint;
        using (var json = JsonDocument.Parse(body))
        {
            (token, hint) = json.RootElement.TryGetProperty("refresh_token", out var refresh)
                ? (refresh.GetString(), "refresh_token")
                : (json.RootElement.TryGetProperty("access_token", out var access)
                    ? access.GetString()
                    : null, "access_token");
        }
        if (string.IsNullOrEmpty(token))
            throw new AppleRevokeException("Apple'ın cevabında token yok");

        using var revokeResponse = await Http.PostAsync(RevokeUrl, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = secret,
                ["token"] = token,
                ["token_type_hint"] = hint,
            }), cancellationToken);
        if (!revokeResponse.IsSuccessStatusCode)
        {
            var error = await revokeResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new AppleRevokeException(
                $"izin geri alınamadı: {(int)revokeResponse.StatusCode} {ErrorOf(error)}");
        }
    }

    /// <summary>Yapılandırmadaki anahtarla istemci sırrı.</summary>
    public string CreateClientSecret(DateTime utcNow)
    {
        using var key = ECDsa.Create();
        key.ImportFromPem(File.ReadAllText(_options.PrivateKeyPath));
        return CreateClientSecret(key, _options, utcNow);
    }

    /// <summary>
    /// Apple'ın istediği istemci sırrı: başlıkta ES256 ve anahtar kimliği
    /// (<c>kid</c>); gövdede yalnız <c>iss</c> (takım), <c>iat</c>,
    /// <c>exp</c>, <c>aud</c> ve <c>sub</c> (paket kimliği). Gövde elle
    /// kuruluyor: kitaplığın kendiliğinden eklediği alanlar (<c>nbf</c>,
    /// ikinci <c>iat</c>) Apple'ın tarifinde yok.
    /// </summary>
    public static string CreateClientSecret(ECDsa key, AppleOptions options, DateTime utcNow)
    {
        var credentials = new SigningCredentials(
            new ECDsaSecurityKey(key) { KeyId = options.KeyId },
            SecurityAlgorithms.EcdsaSha256);
        var payload = new JwtPayload
        {
            ["iss"] = options.TeamId,
            ["iat"] = EpochTime.GetIntDate(utcNow),
            ["exp"] = EpochTime.GetIntDate(utcNow.Add(SecretLifetime)),
            ["aud"] = Audience,
            ["sub"] = options.ClientId,
        };
        return new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(new JwtHeader(credentials), payload));
    }

    /// <summary>Apple'ın hata gövdesindeki kısa kod ("invalid_grant" gibi); token içermez.</summary>
    private static string ErrorOf(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.TryGetProperty("error", out var error)
                ? error.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}

/// <summary>Apple yetki kodunu ya da token'ı reddetti.</summary>
public sealed class AppleRevokeException(string message) : Exception(message);
