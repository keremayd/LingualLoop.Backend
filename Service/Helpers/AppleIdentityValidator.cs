using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using Common.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Service.Helpers;

/// <summary>Apple'ın doğruladığı kimlik.</summary>
/// <param name="Subject">Apple'ın bu uygulamaya özgü, değişmeyen kullanıcı kimliği (<c>sub</c>).</param>
/// <param name="Email">E-posta; kullanıcı gizlediyse Apple'ın aktarma adresi.</param>
public sealed record AppleIdentity(string Subject, string? Email);

/// <summary>
/// Apple kimlik token'ını doğrular: imza Apple'ın yayımladığı anahtarlarla,
/// veren (<c>iss</c>), alıcı (<c>aud</c> = paket kimliği), süre ve nonce.
///
/// Nonce, token'ın başka bir girişten alınıp tekrar kullanılmasını önler:
/// uygulama Apple'a açık değerin SHA-256 özetini gönderir, sunucuya açık
/// değeri yollar; sunucu özeti token'dakiyle karşılaştırır.
///
/// Anahtarlar bellekte tutulur. Token bilinmeyen bir anahtarla imzalıysa
/// (Apple anahtar döndürdüyse) anahtarlar bir kez yeniden indirilir.
/// </summary>
public sealed class AppleIdentityValidator
{
    public const string Issuer = "https://appleid.apple.com";
    private const string KeysUrl = "https://appleid.apple.com/auth/keys";
    private static readonly TimeSpan KeysLifetime = TimeSpan.FromHours(12);
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private readonly AppleOptions _options;
    private readonly SemaphoreSlim _keysLock = new(1, 1);
    private IReadOnlyList<SecurityKey>? _keys;
    private DateTime _keysFetchedAt;

    public AppleIdentityValidator(IOptions<AppleOptions> options)
    {
        _options = options.Value;
    }

    public async Task<AppleIdentity> ValidateAsync(
        string identityToken,
        string rawNonce,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.ClientId))
            throw Invalid("Apple ile giriş yapılandırılmamış");

        try
        {
            var keys = await GetKeysAsync(refresh: false, cancellationToken);
            try
            {
                return Validate(identityToken, rawNonce, keys, _options.ClientId);
            }
            catch (SecurityTokenSignatureKeyNotFoundException)
            {
                keys = await GetKeysAsync(refresh: true, cancellationToken);
                return Validate(identityToken, rawNonce, keys, _options.ClientId);
            }
        }
        catch (Exception e) when (e is SecurityTokenException or ArgumentException)
        {
            throw Invalid(e.Message);
        }
    }

    /// <summary>Anahtarlar elde iken doğrulama; ağa çıkmaz.</summary>
    public static AppleIdentity Validate(
        string identityToken,
        string rawNonce,
        IEnumerable<SecurityKey> keys,
        string clientId)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(identityToken, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = clientId,
            IssuerSigningKeys = keys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        }, out _);

        var nonce = principal.FindFirst("nonce")?.Value;
        if (string.IsNullOrEmpty(rawNonce) || nonce != Sha256Hex(rawNonce))
            throw new SecurityTokenValidationException("nonce uyuşmuyor");

        var subject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(subject))
            throw new SecurityTokenValidationException("kullanıcı kimliği (sub) yok");

        return new AppleIdentity(subject, principal.FindFirst("email")?.Value);
    }

    public static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private async Task<IReadOnlyList<SecurityKey>> GetKeysAsync(
        bool refresh,
        CancellationToken cancellationToken)
    {
        if (!refresh && Fresh()) return _keys!;

        await _keysLock.WaitAsync(cancellationToken);
        try
        {
            if (!refresh && Fresh()) return _keys!;
            var json = await Http.GetStringAsync(KeysUrl, cancellationToken);
            _keys = new JsonWebKeySet(json).GetSigningKeys().ToList();
            _keysFetchedAt = DateTime.UtcNow;
            return _keys;
        }
        finally
        {
            _keysLock.Release();
        }
    }

    private bool Fresh() => _keys != null && DateTime.UtcNow - _keysFetchedAt < KeysLifetime;

    private static LingualLoopException Invalid(string reason) =>
        new(ErrorCode.AppleIdentityInvalid.CreateMessage(reason),
            ErrorCode.AppleIdentityInvalid.GetDescription(reason),
            HttpStatusCode.Unauthorized);
}
