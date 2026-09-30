namespace Common.Enums;

/// <summary>
/// Bir Karty kartının kullanıcıya **nasıl sunulacağı**.
///
/// Kart seçimi ile sunum biçimi ayrı kararlardır: hangi kartın geleceğini
/// puan bandı belirler, o kartın soru mu tanışma mı olacağını kullanıcının o
/// kelimeyle geçmişi belirler. İkisi tek bir bayrağa sıkıştırılmamalı çünkü
/// sunum biçimleri çoğalacak (çoktan seçmeli, yazma…).
/// </summary>
public enum KartyCardMode
{
    /// <summary>
    /// Kelimeyle **ilk tanışma**. Soru sorulmaz; kelime doğru yazımıyla,
    /// artikeliyle ve görseliyle gösterilir.
    ///
    /// Bu adım olmadan Karty öğretmeden soruyordu: yazım sorusu kelimeyi
    /// %50 ihtimalle **bozuk** gösterdiği için ilk temas yanlış formla
    /// oluyordu.
    /// </summary>
    Introduce = 0,

    /// <summary>
    /// Yazım sorusu — kelime ya doğru yazılmış ya iki harfi yer değiştirmiş
    /// hâlde gösterilir, kullanıcı karar verir.
    /// </summary>
    Spelling = 1,
}
