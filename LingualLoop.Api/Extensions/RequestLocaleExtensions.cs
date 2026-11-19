using Service.Helpers;

namespace LingualLoop.Api.Extensions;

public static class RequestLocaleExtensions
{
    /// <summary>
    /// İstemcinin bildirdiği ana dil (<c>Accept-Language</c>); yoksa ya da
    /// desteklenmiyorsa Türkçe. Kural <see cref="LocaleRules"/>'ta.
    /// </summary>
    public static string NativeLanguage(this HttpRequest request) =>
        LocaleRules.FromAcceptLanguage(request.Headers.AcceptLanguage.ToString());
}
