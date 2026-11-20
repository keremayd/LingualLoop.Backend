using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Service.Helpers;

/// Apple'ın imzaladığı App Store verisini (StoreKit 2 işlemi, App Store sunucu
/// bildirimi; JWS, ES256) doğrular. Sertifika zinciri başlıktaki x5c'den
/// kurulur ve Apple Root CA - G3'e bağlanmalı; kök kodda sabit (macOS sistem
/// kök deposundan alındı, SHA-256 63:34:3A:BF…:91:79). Apple'ın kendi
/// kütüphaneleri .NET için yok; doğrulama aynı adımları izler: zincir, Apple
/// uzantıları (yaprak 1.2.840.113635.100.6.11.1, ara 1.2.840.113635.100.6.2.1),
/// imza.
public static class AppleSignedData
{
    private const string AppleRootCaG3Pem = """
-----BEGIN CERTIFICATE-----
MIICQzCCAcmgAwIBAgIILcX8iNLFS5UwCgYIKoZIzj0EAwMwZzEbMBkGA1UEAwwS
QXBwbGUgUm9vdCBDQSAtIEczMSYwJAYDVQQLDB1BcHBsZSBDZXJ0aWZpY2F0aW9u
IEF1dGhvcml0eTETMBEGA1UECgwKQXBwbGUgSW5jLjELMAkGA1UEBhMCVVMwHhcN
MTQwNDMwMTgxOTA2WhcNMzkwNDMwMTgxOTA2WjBnMRswGQYDVQQDDBJBcHBsZSBS
b290IENBIC0gRzMxJjAkBgNVBAsMHUFwcGxlIENlcnRpZmljYXRpb24gQXV0aG9y
aXR5MRMwEQYDVQQKDApBcHBsZSBJbmMuMQswCQYDVQQGEwJVUzB2MBAGByqGSM49
AgEGBSuBBAAiA2IABJjpLz1AcqTtkyJygRMc3RCV8cWjTnHcFBbZDuWmBSp3ZHtf
TjjTuxxEtX/1H7YyYl3J6YRbTzBPEVoA/VhYDKX1DyxNB0cTddqXl5dvMVztK517
IDvYuVTZXpmkOlEKMaNCMEAwHQYDVR0OBBYEFLuw3qFYM4iapIqZ3r6966/ayySr
MA8GA1UdEwEB/wQFMAMBAf8wDgYDVR0PAQH/BAQDAgEGMAoGCCqGSM49BAMDA2gA
MGUCMQCD6cHEFl4aXTQY2e3v9GwOAEZLuN+yRhHFD/3meoyhpmvOwgPUnPWTxnS4
at+qIxUCMG1mihDK1A3UT82NQz60imOlM27jbdoXt2QfyFMm+YhidDkLF1vLUagM
6BgD56KyKA==
-----END CERTIFICATE-----
""";

    private static readonly X509Certificate2 AppleRoot =
        X509Certificate2.CreateFromPem(AppleRootCaG3Pem);

    private const string LeafOid = "1.2.840.113635.100.6.11.1";
    private const string IntermediateOid = "1.2.840.113635.100.6.2.1";

    /// İmza ve zincir geçerliyse yükü döner, değilse null. [trustedRoot]
    /// yalnız testte verilir (Apple uzantıları o zaman aranmaz).
    public static JsonElement? Verify(string? jws, X509Certificate2? trustedRoot = null)
    {
        if (string.IsNullOrWhiteSpace(jws)) return null;
        var parts = jws.Split('.');
        if (parts.Length != 3) return null;

        try
        {
            using var header = JsonDocument.Parse(Base64UrlDecode(parts[0]));
            if (header.RootElement.GetProperty("alg").GetString() != "ES256") return null;

            var chainCerts = header.RootElement.GetProperty("x5c").EnumerateArray()
                .Select(e => new X509Certificate2(Convert.FromBase64String(e.GetString()!)))
                .ToList();
            if (chainCerts.Count < 2) return null;

            var leaf = chainCerts[0];
            var intermediate = chainCerts[1];
            var root = trustedRoot ?? AppleRoot;

            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(root);
            chain.ChainPolicy.ExtraStore.Add(intermediate);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            if (!chain.Build(leaf)) return null;
            if (!chain.ChainElements[^1].Certificate.RawData.SequenceEqual(root.RawData)) return null;

            if (trustedRoot == null &&
                (leaf.Extensions[LeafOid] == null || intermediate.Extensions[IntermediateOid] == null))
            {
                return null;
            }

            using var key = leaf.GetECDsaPublicKey();
            if (key == null) return null;
            var signedPart = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
            if (!key.VerifyData(signedPart, Base64UrlDecode(parts[2]), HashAlgorithmName.SHA256))
            {
                return null;
            }

            using var payload = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            return payload.RootElement.Clone();
        }
        catch (Exception e) when (e is FormatException or JsonException or CryptographicException
                                       or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    public static string? GetString(this JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// Apple tarihleri Unix milisaniye.
    public static DateTime? GetDate(this JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? DateTimeOffset.FromUnixTimeMilliseconds(value.GetInt64()).UtcDateTime
            : null;

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(s);
    }
}
