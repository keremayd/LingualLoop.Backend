namespace Common.Enums;

/// <summary>
/// Bir Karty kartının kullanıcıya **nasıl sunulacağı**.
///
/// Kart seçimi ile sunum biçimi ayrı kararlardır: hangi kartın geleceğini
/// seçici belirler, o kartın nasıl sorulacağını kullanıcının o kelimeyle
/// geçmişi (<c>KartyPresentationPolicy</c>).
/// </summary>
public enum KartyCardMode
{
    /// <summary>
    /// Kelimeyle **ilk tanışma**. Soru sorulmaz; kelime, artikeli, görseli ve
    /// sesiyle gösterilir.
    /// </summary>
    Introduce = 0,

    /// <summary>Kelime yazılı, dört görsel arasından doğrusu bulunur (anlam).</summary>
    PickImage = 1,

    /// <summary>Görsel ve kelime görünür, artikel sorulur.</summary>
    PickArticle = 2,

    /// <summary>Görselden kelimenin harfleri sırayla dizilir (yazım).</summary>
    ArrangeLetters = 3,

    /// <summary>Kelime yalnız duyulur, dört görsel arasından doğrusu bulunur.</summary>
    Listen = 4,
}
